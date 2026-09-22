using UnityEngine;
using XLua;

/// <summary>
/// Lua 总入口。等常驻包加载完成后执行 Main.lua，并驱动它的 Game_Start / Game_Update。
/// </summary>
public class LuaEnter : MonoBehaviour
{
    private LuaFunction startFunction;
    private LuaFunction updateFunction;

    private void Start()
    {
        Downloader.Instance.onBundlesLoaded += StartLua;
        Downloader.Instance.downloadOnFailed += OnDownloadFailed;
    }

    private void StartLua()
    {
        LuaManager.Instance.DoFile("Main");
        startFunction = LuaManager.Instance.GetLuaFunction("Game_Start");
        updateFunction = LuaManager.Instance.GetLuaFunction("Game_Update");

        if (startFunction == null)
        {
            Debug.LogError("Main.lua 里没找到 Game_Start，Lua 侧没有启动");
            return;
        }

        startFunction.Call();
    }

    // 这条是"Lua 入口没起来"的根因，用 LogError 免得被 Console 里的其他日志淹掉
    private void OnDownloadFailed(string message)
    {
        Debug.LogError($"[热更] AB 包下载失败，Lua 无法启动：{message}");
    }

    private void Update()
    {
        if (updateFunction != null)
        {
            updateFunction.Call();
        }
    }

    private void OnDestroy()
    {
        // LuaEnter 在 StartScene、Downloader 在 PersistentScene，两者的销毁顺序没人保证。
        // Downloader 的 OnDestroy 会先把 Instance 置空，这时候再解绑就是空引用。
        if (Downloader.Instance == null)
        {
            return;
        }

        Downloader.Instance.onBundlesLoaded -= StartLua;
        Downloader.Instance.downloadOnFailed -= OnDownloadFailed;
    }
}
