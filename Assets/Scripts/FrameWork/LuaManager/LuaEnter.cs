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
        Debug.Log(message+",AB包下载失败,无法加载lua文件");
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
        Downloader.Instance.onBundlesLoaded -= StartLua;
        Downloader.Instance.downloadOnFailed -= OnDownloadFailed;
    }
}
