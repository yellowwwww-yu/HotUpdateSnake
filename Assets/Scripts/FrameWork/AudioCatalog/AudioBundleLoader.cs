using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using XLua;

[LuaCallCSharp]
public class AudioBundleLoader : Singleton<AudioBundleLoader>
{
    private const string SettingsBundle = "audio_bundlesettings";
    private const string SettingsAsset = "AudioBundleSettings";

    private readonly Dictionary<string, AudioCatalog> groups =
        new Dictionary<string, AudioCatalog>(StringComparer.Ordinal);

    // 每次成功取得包引用都记一笔，同一个包可以出现多次。
    private readonly List<string> heldBundles = new List<string>();

    // 配置从 AB 自动加载，不在 Inspector 直接引用目录或配置资产。
    private AudioBundleSettings settings;
    private Downloader downloader;

    private IEnumerator Start()
    {
        if (Instance != this) yield break;

        // 和 Audio 播放器分开注入，避免混淆资源获取与实际播放。
        if (LuaManager.Instance != null)
            LuaManager.Instance.Inject("AudioResources", this);

        // 兼容下载前创建、下载后创建和 Downloader 稍后出现。
        while (Downloader.Instance == null || !Downloader.Instance.isReady)
            yield return null;

        Init();
    }

    private bool Init()
    {
        if (Instance != this) return false;

        if (downloader == null)
        {
            // 旧 Downloader 销毁时会释放其包，旧缓存不再可用。
            groups.Clear();
            heldBundles.Clear();
            settings = null;
            downloader = Downloader.Instance;
        }

        if (downloader == null || !downloader.isReady)
            return false;

        // 已初始化则直接复用，不重复增加配置包引用。
        if (settings != null) return true;

        settings = LoadAsset<AudioBundleSettings>(
            SettingsBundle, SettingsAsset);
        return settings != null;
    }

    private AudioCatalog FindGroup(string id)
    {
        if (!Init())
        {
            Debug.LogWarning("音频目录尚未初始化，请确认热更已完成且设置可加载。", this);
            return null;
        }

        if (groups.TryGetValue(id, out AudioCatalog catalog) && catalog != null)
            return catalog;

        AudioBundleSettings.Group match = null;
        if (settings.groups != null)
        {
            foreach (AudioBundleSettings.Group group in settings.groups)
            {
                if (group == null || group.id != id) continue;

                if (match != null)
                {
                    Debug.LogError($"音频资源组 ID 重复：{id}", this);
                    return null;
                }
                match = group;
            }
        }

        if (match == null)
        {
            Debug.LogError($"没有配置音频资源组：{id}", this);
            return null;
        }

        catalog = LoadAsset<AudioCatalog>(match.bundle, match.catalog);
        if (catalog == null) return null;

        // 只缓存成功结果，失败后再次查询仍可重试。
        groups[id] = catalog;
        return catalog;
    }

    /// <summary>按资源组与稳定 Key 获取音频，不负责播放或加载音频采样数据。</summary>
    public AudioClip GetClip(string catalogId, string clipId)
    {
        if (string.IsNullOrWhiteSpace(catalogId) ||
            string.IsNullOrWhiteSpace(clipId))
        {
            Debug.LogError("Catalog ID 和 Clip ID 不能为空。", this);
            return null;
        }

        AudioCatalog catalog = FindGroup(catalogId);
        if (catalog == null) return null;

        if (catalog.TryGet(clipId, out AudioClip clip))
            return clip;

        Debug.LogError($"资源组 {catalogId} 中没有有效音频：{clipId}", this);
        return null;
    }

    // 包与资产都成功才保留持有记录；读资产失败则归还本次包引用。
    private T LoadAsset<T>(string bundleName, string assetName)
        where T : UnityEngine.Object
    {
        if (string.IsNullOrWhiteSpace(bundleName) ||
            string.IsNullOrWhiteSpace(assetName))
        {
            Debug.LogError("音频配置中的包名或资源地址为空。", this);
            return null;
        }

        T asset = null;
        bool acquired = false;

        try
        {
            if (downloader.LoadBundle(bundleName) == null)
                return null;

            acquired = true;
            asset = downloader.GetAsset<T>(bundleName, assetName);
            if (asset == null)
            {
                Debug.LogError($"找不到资源：{bundleName} / {assetName}", this);
                return null;
            }

            heldBundles.Add(bundleName);
            return asset;
        }
        catch (Exception exception)
        {
            Debug.LogException(exception, this);
            return null;
        }
        finally
        {
            if (acquired && asset == null)
                downloader.UnloadAB(bundleName, false);
        }
    }

    protected override void OnDestroy()
    {
        try
        {
            if (downloader != null)
            {
                // 与每次成功的 LoadBundle 配对，配置包最后归还。
                for (int i = heldBundles.Count - 1; i >= 0; i--)
                {
                    try
                    {
                        downloader.UnloadAB(heldBundles[i], false);
                    }
                    catch (Exception exception)
                    {
                        // 一个包归还异常时，继续清理其他持有记录。
                        Debug.LogException(exception, this);
                    }
                }
            }
        }
        finally
        {
            groups.Clear();
            heldBundles.Clear();
            settings = null;
            downloader = null;
            base.OnDestroy();
        }
    }
}
