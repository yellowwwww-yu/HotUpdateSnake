using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using XLua;

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
        startFunction.Call();
    }

    private void OnDownloadFailed(string message)
    {
        // 升到 LogError：这条是"整个 Lua 入口没起来"的根因，最容易被 Console 噪音淹掉
        Debug.LogError($"[热更] AB 包下载失败，Lua 无法启动：{message}");
    }

    private void Update()
    {
        if(updateFunction != null)
        {
            updateFunction.Call();
        }
    }

    private void OnDestroy()
    {
        // Downloader 也是 Singleton，它的 OnDestroy 会先把 Instance 置空；
        // 而 LuaEnter 在 StartScene、Downloader 在 PersistentScene，退出播放/切场景时的
        // 销毁顺序没有人保证，直接解绑会 NRE。
        if (Downloader.Instance == null)
        {
            return;
        }

        Downloader.Instance.onBundlesLoaded -= StartLua;
        Downloader.Instance.downloadOnFailed -= OnDownloadFailed;
    }
}
