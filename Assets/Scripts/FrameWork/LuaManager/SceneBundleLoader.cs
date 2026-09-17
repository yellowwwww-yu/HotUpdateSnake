using System.Collections;
using UnityEngine;
using UnityEngine.Events;

// 场景级 AB 包生命周期管理：
// 挂在"游戏场景"的常驻物体上（如 SceneRoot），Inspector 声明本场景特有的包。
// 场景启动 → 等下载就绪 → 批量加载（依赖自动连带）
// 场景卸载（UnloadSceneAsync）→ OnDestroy → 批量归还引用，计数归零自动释放
public class SceneBundleLoader : MonoBehaviour
{
    [Header("本场景特有的AB包名（公用包不用填，归 CommonBundleLoader 管）")]
    [SerializeField] private string[] bundles;

    [Header("释放时是否连已实例化的资源对象一起销毁")]
    [SerializeField] private bool unloadAllLoadedObjects = true;

    [Header("本场景包全部加载完成后触发（场景初始化逻辑拖到这里，或代码轮询 IsReady）")]
    [SerializeField] private UnityEvent onReady;

    /// <summary>本场景包是否已全部加载完成（供代码侧判断）</summary>
    public bool IsReady { get; private set; }

    private bool hasLoaded;   // 防止"还没加载完就退出场景"时错误归还计数

    IEnumerator Start()
    {
        // 必须等下载阶段完成，包文件才在本地磁盘上
        while (!Downloader.Instance.isReady)
        {
            yield return null;
        }

        foreach(string bundleName in bundles)
        {
            if(Downloader.Instance.LoadBundle(bundleName) == null)
            {
                Debug.Log($"场景包 {bundleName} 加载失败，本场景功能可能不完整");
            }
        }

        hasLoaded = true;
        IsReady = true;
        onReady?.Invoke();   // ← 广播：本场景资源就绪
        Debug.Log($"场景资源就绪，共加载 {bundles.Length} 个包");
    }

    private void OnDestroy()
    {
        IsReady = false;

        // 还没加载成功就退出场景（或游戏退出）时不归还，避免计数错乱
        if(!hasLoaded || Downloader.Instance == null)
        {
            return;
        }

        foreach(string bundleName in bundles)
        {
            Downloader.Instance.UnloadAB(bundleName, unloadAllLoadedObjects);
        }
    }
}
