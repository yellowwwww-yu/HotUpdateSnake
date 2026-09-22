using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// 场景级 AB 包的生命周期管理。挂在游戏场景里的常驻物体上（比如 SceneRoot），
/// 本场景特有的包在 Inspector 上声明，公用包不用填，归 CommonBundleLoader 管。
///
/// 场景启动后等下载就绪，再批量加载（依赖会自动带上）；
/// 场景卸载时 OnDestroy 归还引用，计数归零的包自动释放。
/// </summary>
public class SceneBundleLoader : MonoBehaviour
{
    [Header("本场景特有的AB包名（公用包不用填，归 CommonBundleLoader 管）")]
    [SerializeField] private string[] bundles;

    [Header("释放时是否连已实例化的资源对象一起销毁")]
    [SerializeField] private bool unloadAllLoadedObjects = true;

    [Header("本场景包全部加载完成后触发")]
    public Action onReady;

    /// <summary>本场景的包是否已全部加载完成</summary>
    public bool IsReady { get; private set; }

    private bool hasLoaded;   // 防止"没加载完就退出场景"时错误归还引用

    IEnumerator Start()
    {
        // Downloader 为 null 说明常驻功能场景没加载，这里会一直等下去，
        // 先把原因打出来，免得变成一个没有任何报错的静默卡住
        if (Downloader.Instance == null)
        {
            Debug.LogError("Downloader 不存在（常驻功能场景未加载？）：SceneBundleLoader 将持续等待，本场景包不会加载");
        }

        // 要等下载完成，包文件才在磁盘上；还要等常驻包加载完，lua 包才可读。
        // 失败时不退出循环，停在这里等重试成功即可。failLogged 防止每帧刷日志。
        bool failLogged = false;
        while (Downloader.Instance == null || !Downloader.Instance.isReady || !Downloader.Instance.isBundlesLoaded)
        {
            if (!failLogged && Downloader.Instance != null && Downloader.Instance.hasFailed)
            {
                failLogged = true;
                Debug.LogError($"热更下载失败，场景包暂停加载，等待重试：{Downloader.Instance.lastError}");
            }

            yield return null;
        }

        foreach (string bundleName in bundles)
        {
            if (Downloader.Instance.LoadBundle(bundleName) == null)
            {
                Debug.Log($"场景包 {bundleName} 加载失败，本场景功能可能不完整");
            }
        }

        hasLoaded = true;
        IsReady = true;
        onReady?.Invoke();
        Debug.Log($"场景资源就绪，共加载 {bundles.Length} 个包");
    }

    private void OnDestroy()
    {
        IsReady = false;

        // 还没加载成功就退出场景（或直接退出游戏）时不归还，避免引用计数错乱
        if (!hasLoaded || Downloader.Instance == null)
        {
            return;
        }

        foreach (string bundleName in bundles)
        {
            Downloader.Instance.UnloadAB(bundleName, unloadAllLoadedObjects);
        }
    }
}
