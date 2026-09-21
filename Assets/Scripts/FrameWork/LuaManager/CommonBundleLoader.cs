using System.Collections;
using UnityEngine;

// 常驻包管理：全局公用的 AB 包（lua 包、公共 UI 图集、字体等）
// 挂在"常驻功能场景"里（和 Downloader 同一个物体即可），
// 启动时加载一次，直到游戏退出才释放——共用包不会再随场景反复加载卸载。
public class CommonBundleLoader : MonoBehaviour
{
    [Header("全局公用的AB包名，如 lua / commonui")]
    [SerializeField] private string[] commonBundles;

    IEnumerator Start()
    {
        // Downloader 为 null 说明常驻功能场景没加载：先把原因喊出来，别静默挂死
        if (Downloader.Instance == null)
        {
            Debug.LogError("Downloader 不存在（常驻功能场景未加载？）：常驻包将持续等待加载");
        }

        // 等下载阶段成功。失败时【刻意不退出循环】：
        // 热更没成功游戏就不该继续，而停在这里等，玩家点重试、isReady 变 true 之后
        // 本循环会自然通过——如果这里 yield break，重试成功后就没人加载常驻包了。
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

        foreach(string bundleName in commonBundles)
        {
            if(Downloader.Instance.LoadBundle(bundleName) == null)
            {
                Debug.Log($"常驻包 {bundleName} 加载失败");
            }
        }

        Debug.Log($"常驻资源就绪，共加载 {commonBundles.Length} 个包");
        Downloader.Instance.NotifyBundlesLoaded();   // ← 关键：广播"常驻包已加载完，业务可开始"
    }

    private void OnDestroy()
    {
        // 只有游戏退出才会走到这里（物体在常驻场景里）
        if(Downloader.Instance == null)
        {
            return;
        }

        foreach(string bundleName in commonBundles)
        {
            Downloader.Instance.UnloadAB(bundleName, true);
            Debug.Log($"{bundleName}已被释放");
        }
    }
}
