using System;
using UnityEngine;
using XLua;

/// <summary>本机设置存储；不需要挂物体，也不创建新的Lua全局。</summary>
[LuaCallCSharp]
public static class SettingsStorage
{
    private const string Prefix = "Settings.v1.";

    public static float MusicVolume
    {
        get => ReadValue("Music", 1f);
        set => PlayerPrefs.SetFloat(Prefix + "Music", Normalize(value));
    }

    public static float SoundVolume
    {
        get => ReadValue("Sound", 1f);
        set => PlayerPrefs.SetFloat(Prefix + "Sound", Normalize(value));
    }

    public static float Brightness
    {
        get => ReadValue("Brightness", 1f);
        set => PlayerPrefs.SetFloat(Prefix + "Brightness", Normalize(value));
    }

    public static int WindowWidth => ReadSize("Width", 1280, Screen.currentResolution.width);
    public static int WindowHeight => ReadSize("Height", 720, Screen.currentResolution.height);

    public static void SetDisplay(int width, int height, bool fullScreen)
    {
        PlayerPrefs.SetInt(Prefix + "Width", width);
        PlayerPrefs.SetInt(Prefix + "Height", height);
        PlayerPrefs.SetInt(Prefix + "FullScreen", fullScreen ? 1 : 0);
    }

    public static bool Save()
    {
        try
        {
            PlayerPrefs.Save();
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError("[Settings] 保存失败：" + e.Message);
            return false;
        }
    }

    // 首个场景加载后恢复显示，不依赖玩家是否打开设置面板。
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void RestoreDisplay()
    {
        if (Application.platform != RuntimePlatform.WindowsPlayer ||
            !PlayerPrefs.HasKey(Prefix + "FullScreen")) return;

        bool fullScreen = PlayerPrefs.GetInt(Prefix + "FullScreen", 0) == 1;
        int width = fullScreen ? Screen.currentResolution.width : WindowWidth;
        int height = fullScreen ? Screen.currentResolution.height : WindowHeight;
        Screen.SetResolution(width, height,
            fullScreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed);
    }

    private static float ReadValue(string key, float fallback)
    {
        return Normalize(PlayerPrefs.GetFloat(Prefix + key, fallback));
    }

    private static float Normalize(float value)
    {
        return float.IsNaN(value) || float.IsInfinity(value) ? 1f : Mathf.Clamp01(value);
    }

    private static int ReadSize(string key, int fallback, int maximum)
    {
        int value = PlayerPrefs.GetInt(Prefix + key, fallback);
        return Mathf.Clamp(value > 0 ? value : fallback, 1, Mathf.Max(1, maximum));
    }
}
