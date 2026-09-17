using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

/// <summary>
/// 热更新下载与资源加载核心（单例，挂在常驻功能场景）。
///
/// 三个阶段 + 两类信号：
///   下载阶段：远程清单 → 对比本地缓存 → 差异下载(自动重试) → 主包依赖表就绪
///             完成后置 isReady = true 并广播 downloadOnComplete（语义：包文件已全在磁盘）
///   加载阶段：CommonBundleLoader 加载常驻包、SceneBundleLoader 加载场景包，都走 LoadBundle 记账
///   就绪信号：常驻包加载完成后 CommonBundleLoader 调 NotifyBundlesLoaded()
///             业务等 onBundlesLoaded / isBundlesLoaded 再 DoFile、GetAsset —— 时序从此显式
///
/// 事件语义（已收紧，别混用）：
///   downloadOnComplete : 下载完成（磁盘就绪，不代表已加载）
///   downloadOnFailed   : 仅下载阶段失败（运行期加载失败不再走这个事件）
///   onBundlesLoaded    : 常驻包已加载，可以安全取资源
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

    private AssetBundleManifest unityManifest;   // 依赖关系表（读自主包）
    private AssetBundle masterBundle;            // 主包本体：只供读依赖表，不入引用计数，退出时才释放
    private bool isDownloading;                  // 防重复触发下载

    /// <summary>下载阶段是否完成（包文件已全部在磁盘），Loader 等这个再开始加载。</summary>
    public bool isReady;

    /// <summary>常驻包是否已加载完成（业务等这个再 DoFile / GetAsset）。</summary>
    public bool isBundlesLoaded;

    public Dictionary<string, AssetBundle> assetBundles = new Dictionary<string, AssetBundle>();
    public Dictionary<string, int> refCounts = new Dictionary<string, int>();

    [SerializeField] private Slider slider;
    [SerializeField] private Text text;

    public Action downloadOnComplete;
    public Action<string> downloadOnFailed;
    public Action onBundlesLoaded;

    protected override void Awake()
    {
        base.Awake();

        localBundlesPath = Application.persistentDataPath + "/bundles";
        localManifestPath = Application.persistentDataPath + "/manifest.json";
        if (!Directory.Exists(localBundlesPath))
        {
            Directory.CreateDirectory(localBundlesPath);
        }

        StartDownload();
    }

    /// <summary>启动下载；下载中或已完成时重复调用无效。失败后业务可再调它重试。</summary>
    public void StartDownload()
    {
        if (isDownloading || isReady)
        {
            return;
        }

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
            isDownloading = false;   // 无论成败都解锁，允许重试
        }
    }

    IEnumerator DownloadAll()
    {
        // ---- ① 拉远程清单（自动重试） ----
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

        // ---- ② 对比本地缓存，算出真正要下的差异包 ----
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
            yield return new WaitForSeconds(0.5f);   // 停留一下再进入加载阶段
        }

        // ---- ③ 逐个差异下载（整体进度实时更新：已完成包 + 当前包进度） ----
        int total = needDownload.Count;
        int downloaded = 0;
        foreach (BundleInfo info in needDownload)
        {
            byte[] data = null;
            yield return DownloadData(path + info.name,
                r => data = r,
                p =>
                {
                    // 整体进度 = (已完成的包数 + 当前包进度) / 总需下载包数
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

        // ---- ④ 主包必须就位，读依赖表（LoadBundle 递归全靠它） ----
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

        // ---- ⑤ 收尾：本地清单"滞后提交"（避免半截文件被误当做好文件），再广播下载完成 ----
        SaveManifestToLocal(remoteManifest);

        SetProgress(1f, "更新完成");
        yield return new WaitForSeconds(0.5f);   // 让"更新完成"字样停留一下再隐藏

        isReady = true;
        downloadOnComplete?.Invoke();
        HideProgressUI();
    }

    /// <summary>
    /// 带重试的统一下载。只要成功一次：onResult(字节)。全部失败：onResult(null)。
    /// onProgress 实时回调 0~1，不需要就传 null（manifest 小，不显示进度）。
    /// </summary>
    IEnumerator DownloadData(string url, Action<byte[]> onResult, Action<float> onProgress)
    {
        for (int i = 1; i <= maxRetry; i++)
        {
            using (UnityWebRequest www = UnityWebRequest.Get(url))
            {
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
                yield return new WaitForSeconds(1f);   // 停 1 秒再试，别疯狂重连
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
    /// 加载一个包（含依赖）并把计数 +1。加载失败返回 null，由调用方处理。
    /// 运行期失败只打日志，不再触发 downloadOnFailed（那是下载阶段的事件）。
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
            // 写盘失败这里只报错不中断；下次启动时清单对比会发现文件缺失/不符，自动重下（自愈）
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

    /// <summary>
    /// 统一刷新进度 UI：整体进度(0~1) + 状态文案。
    /// UI 未拖引用时自动跳过，不影响逻辑。
    /// </summary>
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
    /// 下载阶段失败统一出口：刷新 UI 文案 + 触发 downloadOnFailed 事件。
    /// 不隐藏进度 UI，把错误留在屏幕上，玩家可据此决定是否重试。
    /// </summary>
    private void FailAndNotify(string message)
    {
        SetProgress(slider != null ? slider.value : 0f, message);
        downloadOnFailed?.Invoke(message);
    }

    private void HideProgressUI()
    {
        if (slider != null) { slider.gameObject.SetActive(false); }
        if (text != null) { text.gameObject.SetActive(false); }
    }

    /// <summary>
    /// 从"已加载"的包里取资源：只借用、不计数。
    /// 包未加载时返回 null 并提示。正常时序下（等 onBundlesLoaded）常驻包必已就绪，不会再走到提示。
    /// </summary>
    public T GetAsset<T>(string bundleName, string fileName) where T : UnityEngine.Object
    {
        if (!assetBundles.TryGetValue(bundleName, out AssetBundle ab))
        {
            Debug.LogWarning($"{bundleName} 尚未加载，GetAsset 只借用已加载的包：\n" +
                             "① 若属常驻包：确认已填进 CommonBundleLoader，并等 onBundlesLoaded 再取用；\n" +
                             "② 若属场景包：确认该场景 SceneBundleLoader 加载完成（IsReady）后再取用。");
            return null;
        }

        return ab.LoadAsset<T>(fileName);
    }

    /// <summary>
    /// 常驻包全部加载完成后由 CommonBundleLoader 调用（幂等）。
    /// 触发后 onBundlesLoaded 广播，业务脚本可以安全 DoFile / GetAsset 了。
    /// </summary>
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
        base.OnDestroy();   // 清 Singleton.instance，防悬挂引用

        if (masterBundle != null)
        {
            masterBundle.Unload(true);   // 主包不入字典，退出时单独释放
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
    }
}
