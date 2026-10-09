using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

/// <summary>
/// 热更新下载与资源加载核心，单例，挂在常驻功能场景上。
///
/// 先下载后加载：
///   拉远程清单 → 和本地缓存对比 → 下载差异包 → 读主包里的依赖表 → isReady = true
///   之后 CommonBundleLoader 加载常驻包、SceneBundleLoader 加载场景包，都走 LoadBundle 记账
///   常驻包加载完由 CommonBundleLoader 调 NotifyBundlesLoaded，业务这时才能取资源
///
/// 三个信号不要混用：
///   downloadOnComplete  下载完成，包已经落到磁盘（不代表已加载）
///   downloadOnFailed    只在下载阶段失败时触发，运行期加载失败不走它
///   onBundlesLoaded     常驻包已加载，可以安全 DoFile / GetAsset
///
/// 下载失败统一走 FailAndNotify：记下原因、把进度 UI 换成错误提示、亮出重试按钮。
/// 等着下载完成的 Loader 失败时不退出循环，留在原地等重试成功，
/// 这样重试之后整条链路会自己继续，不需要谁去重新驱动。
/// </summary>
public class Downloader : Singleton<Downloader>
{
    [Header("更新服务器地址（末尾带/）")]
    [SerializeField] private string path = @"http://localhost/热更新贪吃蛇/AssetBundles/PC/";

    [Header("网络请求失败自动重试次数")]
    [SerializeField] private int maxRetry = 3;

    private string localBundlesPath;
    private string localManifestPath;
    private string masterBundleName = "PC";

    private AssetBundleManifest unityManifest;   // 依赖关系表，从主包里读
    private AssetBundle masterBundle;            // 主包本体，只用来读依赖表，不进引用计数
    private bool isDownloading;                  // 防止重复触发下载

    /// <summary>下载阶段是否完成（包文件都在磁盘上了），Loader 等这个再开始加载</summary>
    public bool isReady;

    /// <summary>常驻包是否加载完成，业务等这个再 DoFile / GetAsset</summary>
    public bool isBundlesLoaded;

    public Dictionary<string, AssetBundle> assetBundles = new Dictionary<string, AssetBundle>();
    public Dictionary<string, int> refCounts = new Dictionary<string, int>();

    [Header("更新 UI")]
    [SerializeField] private Transform UI;
    [SerializeField] private Slider slider;
    [SerializeField] private Text text;
    [SerializeField] private Button retryButton;   // 失败时才显示的重试按钮，onClick 在代码里绑

    [Header("单次网络请求超时（秒），0 表示不限制")]
    [SerializeField] private int timeout = 10;

    public Action downloadOnComplete;
    public Action<string> downloadOnFailed;
    public Action onBundlesLoaded;

    /// <summary>下载阶段是否失败，调 StartDownload 重试时会自动清零</summary>
    public bool hasFailed { get; private set; }

    /// <summary>最后一次失败的原因，给 UI 和日志用</summary>
    public string lastError { get; private set; }

    protected override void Awake()
    {
        base.Awake();

        localBundlesPath = Application.persistentDataPath + "/bundles";
        localManifestPath = Application.persistentDataPath + "/manifest.json";
        if (!Directory.Exists(localBundlesPath))
        {
            Directory.CreateDirectory(localBundlesPath);
        }

        if (retryButton != null)
        {
            retryButton.onClick.AddListener(StartDownload);   // 代码绑定，省得在 Inspector 上拖
        }

        StartDownload();   // 里面会隐藏重试按钮，不用再单独 SetActive(false)
    }

    private void Start()
    {
        // 把自己交给 Lua，供 Lua 侧取资源（Downloader:GetSprite）。
        // 放 Start 不放 Awake：LuaManager 也是单例，Awake 的执行顺序不保证。
        if (LuaManager.Instance != null)
        {
            LuaManager.Instance.Inject("Downloader", this);
        }
    }

    /// <summary>
    /// 启动下载。正在下载或已经下完时重复调用无效，失败后业务可以再调它重试。
    /// 重试是重新走一遍流程，已经下好的包会在对比阶段命中缓存，只补缺的那几个。
    /// </summary>
    public void StartDownload()
    {
        if (isDownloading || isReady)
        {
            return;
        }

        hasFailed = false;
        lastError = null;
        SetProgressUIVisible(true);   // 重试期间进度要看得见
        SetRetryVisible(false);       // 但重试按钮先收起来，避免连点

        isDownloading = true;
        StartCoroutine(RunDownload());
    }

    IEnumerator RunDownload()
    {
        try
        {
            yield return DownloadAll();
        }
        finally
        {
            isDownloading = false;   // 不管成功失败都解锁，允许重试
        }
    }

    IEnumerator DownloadAll()
    {
        // 1. 拉远程清单（内部带重试）
        SetProgress(0f, "正在检查更新…");
        byte[] manifestBytes = null;
        yield return DownloadData(path + "manifest.json", r => manifestBytes = r, null);

        if (manifestBytes == null)
        {
            FailAndNotify($"获取远程清单失败（已重试{maxRetry}次），请检查网络后点重试");
            yield break;
        }

        HotUpdateManifest remoteManifest = null;
        try
        {
            remoteManifest = JsonUtility.FromJson<HotUpdateManifest>(Encoding.UTF8.GetString(manifestBytes));
        }
        catch (Exception e)
        {
            Debug.LogError("远程清单解析异常：" + e.Message);
        }

        if (remoteManifest == null || remoteManifest.bundles == null || remoteManifest.bundles.Count == 0)
        {
            FailAndNotify("远程清单内容为空或格式错误");
            yield break;
        }

        // 2. 和本地缓存对比，算出真正要下的包
        HotUpdateManifest localManifest = GetLocalManifest();

        List<BundleInfo> needDownload = new List<BundleInfo>();
        foreach (BundleInfo info in remoteManifest.bundles)
        {
            bool cacheHit = GetLocalHash(localManifest, info.name) == info.hash
                            && File.Exists(localBundlesPath + "/" + info.name);
            if (cacheHit)
            {
                Debug.Log($"缓存命中，{info.name} 无需下载");
                continue;
            }

            needDownload.Add(info);
        }
        Debug.Log($"远程共 {remoteManifest.bundles.Count} 个包，本次需下载 {needDownload.Count} 个");

        if (needDownload.Count == 0)
        {
            SetProgress(1f, "已是最新版本");
            yield return new WaitForSeconds(0.5f);   // 停一下再进加载阶段
        }

        // 3. 逐个下载差异包，进度按"已完成的包 + 当前包进度"实时更新
        int total = needDownload.Count;
        int downloaded = 0;
        foreach (BundleInfo info in needDownload)
        {
            byte[] data = null;
            yield return DownloadData(path + info.name,
                r => data = r,
                p =>
                {
                    float overall = total > 0 ? (downloaded + p) / total : 1f;
                    SetProgress(overall, $"正在下载 {info.name}  ({downloaded + 1}/{total})  {Mathf.Floor(p * 100)}%");
                });

            if (data == null)
            {
                FailAndNotify($"包 {info.name} 下载失败（已重试{maxRetry}次），本次更新中断，可点重试继续");
                yield break;
            }

            SaveBundleToLocal(data, info.name);
            downloaded++;
            Debug.Log($"{info.name} 下载并保存成功");
        }

        // 4. 主包就位后才能读依赖表，后面 LoadBundle 的递归全靠它
        string masterPath = localBundlesPath + "/" + masterBundleName;
        if (!File.Exists(masterPath))
        {
            FailAndNotify($"主包 {masterBundleName} 不在本地，请确认服务器清单包含主包");
            yield break;
        }

        masterBundle = AssetBundle.LoadFromFile(masterPath);
        if (masterBundle == null)
        {
            FailAndNotify("主包损坏，无法读取依赖表");
            yield break;
        }

        unityManifest = masterBundle.LoadAsset<AssetBundleManifest>("AssetBundleManifest");
        if (unityManifest == null)
        {
            FailAndNotify("主包内依赖表损坏");
            yield break;
        }

        // 5. 最后才写本地清单，避免半截文件被当成好文件，然后广播下载完成
        SaveManifestToLocal(remoteManifest);

        SetProgress(1f, "更新完成");
        yield return new WaitForSeconds(0.5f);   // 让"更新完成"多停一会儿

        isReady = true;
        downloadOnComplete?.Invoke();
        HideProgressUI();
    }

    /// <summary>
    /// 带重试的下载。成功时回调 onResult(字节)，全部失败回调 onResult(null)。
    /// onProgress 实时回调 0~1，不需要进度就传 null（清单很小，不用显示进度）。
    /// </summary>
    IEnumerator DownloadData(string url, Action<byte[]> onResult, Action<float> onProgress)
    {
        for (int i = 1; i <= maxRetry; i++)
        {
            using (UnityWebRequest www = UnityWebRequest.Get(url))
            {
                if (timeout > 0)
                {
                    www.timeout = timeout;   // 服务器连上了却不响应时，别在这一帧死等
                }

                www.SendWebRequest();

                while (!www.isDone)
                {
                    onProgress?.Invoke(www.downloadProgress);
                    yield return null;
                }

                if (www.result == UnityWebRequest.Result.Success)
                {
                    Debug.Log($"下载成功：{url}");
                    onResult(www.downloadHandler.data);
                    yield break;
                }

                Debug.LogWarning($"第 {i}/{maxRetry} 次下载失败：{url}，原因：{www.error}");
            }

            if (i < maxRetry)
            {
                yield return new WaitForSeconds(1f);   // 隔一秒再试，别连续重连
            }
        }

        Debug.LogError($"重试 {maxRetry} 次后仍然失败：{url}");
        onResult(null);
    }

    private HotUpdateManifest GetLocalManifest()
    {
        if (!File.Exists(localManifestPath))
        {
            Debug.Log("未找到本地Manifest文件");
            return null;
        }

        try
        {
            return JsonUtility.FromJson<HotUpdateManifest>(File.ReadAllText(localManifestPath));
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// 加载一个包（含依赖）并把引用计数加一，失败返回 null 由调用方处理。
    /// 运行期加载失败只打日志，不触发 downloadOnFailed，那是下载阶段的事件。
    /// </summary>
    public AssetBundle LoadBundle(string bundleName)
    {
        if (unityManifest == null)
        {
            Debug.LogError($"{bundleName} 加载失败：依赖表未就绪（下载未完成或主包缺失）");
            return null;
        }

        if (assetBundles.TryGetValue(bundleName, out AssetBundle bundle))
        {
            refCounts[bundleName] += 1;
            Debug.Log($"{bundleName} 已被加载，引用+1，当前引用数：{refCounts[bundleName]}");
            return bundle;
        }

        string[] dependencies = unityManifest.GetAllDependencies(bundleName);
        foreach (string dependence in dependencies)
        {
            if (LoadBundle(dependence) == null)
            {
                Debug.LogError($"获取依赖 {dependence} 失败");
                return null;
            }
        }

        string bundlePath = localBundlesPath + "/" + bundleName;
        if (!File.Exists(bundlePath))
        {
            Debug.LogError($"本地文件丢失：{bundleName}");
            return null;
        }

        AssetBundle ab = AssetBundle.LoadFromFile(bundlePath);
        if (ab == null)
        {
            Debug.LogError($"{bundleName} 本地文件损坏");
            return null;
        }

        assetBundles[bundleName] = ab;
        refCounts[bundleName] = 1;
        Debug.Log($"{bundleName} 加载完成，引用数为1");
        return ab;
    }

    private void SaveBundleToLocal(byte[] data, string fileName)
    {
        try
        {
            File.WriteAllBytes(localBundlesPath + "/" + fileName, data);
        }
        catch (Exception e)
        {
            // 这里只报错不中断。下次启动对比清单时会发现文件缺失，重新下载
            Debug.LogError("保存到本地失败:" + e.Message);
        }
    }

    private void SaveManifestToLocal(HotUpdateManifest manifest)
    {
        try
        {
            File.WriteAllText(localManifestPath, JsonUtility.ToJson(manifest));
        }
        catch (Exception e)
        {
            Debug.LogError("保存Manifest到本地时出错：" + e.Message);
        }
    }

    private string GetLocalHash(HotUpdateManifest localManifest, string bundleName)
    {
        if (localManifest == null)
        {
            return null;
        }

        foreach (BundleInfo info in localManifest.bundles)
        {
            if (info.name == bundleName)
            {
                return info.hash;
            }
        }

        return null;
    }

    /// <summary>刷新进度 UI：整体进度(0~1) + 状态文案。没拖引用会自动跳过，不影响逻辑。</summary>
    private void SetProgress(float overall, string status)
    {
        if (slider != null)
        {
            slider.value = overall;
        }
        if (text != null)
        {
            text.text = status;
        }
    }

    /// <summary>
    /// 下载失败的统一出口：记录原因、刷新 UI 文案、亮出重试按钮、触发 downloadOnFailed。
    /// 这里不隐藏进度 UI，错误要留在屏幕上，玩家才能判断要不要重试。
    /// </summary>
    private void FailAndNotify(string message)
    {
        hasFailed = true;
        lastError = message;

        SetProgress(slider != null ? slider.value : 0f, message);
        SetProgressUIVisible(true);   // 错误原因要留在屏幕上，别被上一次成功的隐藏盖掉
        SetRetryVisible(true);

        Debug.LogError("[热更] " + message);
        downloadOnFailed?.Invoke(message);
    }

    private void SetProgressUIVisible(bool visible)
    {
        if(UI!=null){UI.gameObject.SetActive(visible);}
    }

    // 重试按钮单独控制，不并进 SetProgressUIVisible，
    // 否则"开始下载时显示进度"会顺手把失败时才该出现的重试按钮也点亮
    private void SetRetryVisible(bool visible)
    {
        if (retryButton != null) { retryButton.gameObject.SetActive(visible); }
    }

    private void HideProgressUI()
    {
        SetProgressUIVisible(false);
        SetRetryVisible(false);
    }

    /// <summary>
    /// 从已加载的包里取资源，只借用不计数。包没加载时返回 null 并提示。
    /// 正常时序下（等 onBundlesLoaded 再取）常驻包一定已经就绪，不会走到提示分支。
    /// </summary>
    public T GetAsset<T>(string bundleName, string fileName) where T : UnityEngine.Object
    {
        if (!assetBundles.TryGetValue(bundleName, out AssetBundle ab))
        {
            Debug.LogWarning($"{bundleName} 尚未加载，GetAsset 只能从已加载的包里取：\n" +
                             "常驻包请确认已填进 CommonBundleLoader，并等 onBundlesLoaded 后再取用；\n" +
                             "场景包请确认本场景的 SceneBundleLoader 已加载完成（IsReady）。");
            return null;
        }

        return ab.LoadAsset<T>(fileName);
    }

    /// <summary>
    /// 给 Lua 用的精灵取用口。
    /// Lua 调泛型方法要经过 xlua.get_generic_method，绕；这里开个非泛型直口。
    /// 只从已加载的包里借，不加载、不计数 —— 与 GetAsset 行为一致。
    /// </summary>
    public UnityEngine.Sprite GetSprite(string bundleName, string fileName)
    {
        return GetAsset<UnityEngine.Sprite>(bundleName, fileName);
    }

    /// <summary>常驻包全部加载完成后由 CommonBundleLoader 调用，重复调用无效</summary>
    public void NotifyBundlesLoaded()
    {
        if (isBundlesLoaded)
        {
            return;
        }

        isBundlesLoaded = true;
        Debug.Log("常驻资源全部就绪，业务可以开始了");
        onBundlesLoaded?.Invoke();
    }

    public void UnloadAB(string bundleName, bool unloadAllLoadeObjects)
    {
        if (!refCounts.TryGetValue(bundleName, out int count))
        {
            Debug.Log("无法释放不存在的AssetBundle");
            return;
        }

        count--;
        if (count > 0)
        {
            refCounts[bundleName] = count;
            Debug.Log($"{bundleName} 该包仍有引用，暂不释放,当前引用数{count}");
            return;
        }

        refCounts.Remove(bundleName);

        if (assetBundles.TryGetValue(bundleName, out AssetBundle ab))
        {
            ab.Unload(unloadAllLoadeObjects);
            assetBundles.Remove(bundleName);
            Debug.Log($"{bundleName} 目标包已被释放");
        }

        foreach (string dependence in unityManifest.GetAllDependencies(bundleName))
        {
            UnloadAB(dependence, unloadAllLoadeObjects);
        }
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();   // 清掉 Singleton 里的静态引用

        if (masterBundle != null)
        {
            masterBundle.Unload(true);   // 主包不在字典里，单独释放
            masterBundle = null;
            unityManifest = null;
        }

        foreach (AssetBundle ab in assetBundles.Values)
        {
            ab.Unload(true);
        }
        assetBundles.Clear();
        refCounts.Clear();

        isReady = false;
        isBundlesLoaded = false;
        isDownloading = false;
        hasFailed = false;
        lastError = null;
    }
}
