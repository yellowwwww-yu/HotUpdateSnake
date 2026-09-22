using System.Collections;
using UnityEngine;

/// <summary>
/// 常驻包管理，负责全局公用的 AB 包，比如 lua 包、公共 UI 图集、字体。
/// 挂在常驻功能场景里（和 Downloader 同一个物体就行），启动时加载一次，直到退出游戏才释放。
/// </summary>
public class CommonBundleLoader : MonoBehaviour
{
    [Header("全局公用的AB包名，如 lua / commonui")]
    [SerializeField] private string[] commonBundles;

    IEnumerator Start()
    {
        // Downloader 为 null 说明常驻功能场景没加载，先把原因打出来，别静默卡住
        if (Downloader.Instance == null)
        {
            Debug.LogError("Downloader 不存在（常驻功能场景未加载？）：常驻包将持续等待加载");
        }

        // 等下载完成。失败时这里不退出循环：热更没成功游戏本来就不该继续，
        // 停在这里等玩家点重试，isReady 变 true 之后循环会自己通过。
        // 如果这里 yield break，重试成功后就没有人来加载常驻包了。
        // failLogged 只是防止失败期间每帧刷日志。
        bool failLogged = false;
        while (Downloader.Instance == null || !Downloader.Instance.isReady)
        {
            if (!failLogged && Downloader.Instance != null && Downloader.Instance.hasFailed)
            {
                failLogged = true;
                Debug.LogError($"热更下载失败，常驻包暂停加载，等待重试：{Downloader.Instance.lastError}");
            }

            yield return null;
        }

        foreach (string bundleName in commonBundles)
        {
            if (Downloader.Instance.LoadBundle(bundleName) == null)
            {
                Debug.Log($"常驻包 {bundleName} 加载失败");
            }
        }

        Debug.Log($"常驻资源就绪，共加载 {commonBundles.Length} 个包");
        Downloader.Instance.NotifyBundlesLoaded();   // 通知业务：常驻包加载完了，可以取资源
    }

    private void OnDestroy()
    {
        // 物体在常驻场景里，走到这里基本就是退出游戏了
        if (Downloader.Instance == null)
        {
            return;
        }

        foreach (string bundleName in commonBundles)
        {
            Downloader.Instance.UnloadAB(bundleName, true);
            Debug.Log($"{bundleName}已被释放");
        }
    }
}
