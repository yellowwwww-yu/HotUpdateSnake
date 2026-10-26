using UnityEngine;
using XLua;

/// <summary>常驻遮罩控制，关闭设置面板后仍保持当前亮度。</summary>
[LuaCallCSharp]
public class ScreenBrightness : MonoBehaviour
{
    [SerializeField] private UnityEngine.UI.Image mask;
    private float value = 1f;

    public float Value => value;

    private void Start()
    {
        if (mask == null || LuaManager.Instance == null)
        {
            Debug.LogError("[Brightness] 请配置常驻遮罩并保证LuaManager存在", this);
            return;
        }

        mask.raycastTarget = false;
        SetValue(SettingsStorage.Brightness);
        LuaManager.Instance.Inject("Brightness", this);
    }

    public void SetValue(float newValue)
    {
        value = float.IsNaN(newValue) || float.IsInfinity(newValue)
            ? 1f : Mathf.Clamp01(newValue);
        mask.color = new Color(0f, 0f, 0f, (1f - value) * 0.65f);
    }

    private void OnApplicationPause(bool paused)
    {
        if (paused) SettingsStorage.Save();
    }

    private void OnApplicationQuit()
    {
        SettingsStorage.Save();
    }
}
