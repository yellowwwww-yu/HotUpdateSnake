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
        // 必须等下载阶段完成，包文件才在本地磁盘上
        while (!Downloader.Instance.isReady)
        {
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
