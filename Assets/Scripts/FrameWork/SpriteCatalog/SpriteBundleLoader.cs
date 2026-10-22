using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpriteBundleLoader : Singleton<SpriteBundleLoader>
{
    private const string SettingsBundle = "sprite_bundlesettings";
    private const string SettingsAsset = "SpriteBundleSettings";

    private readonly Dictionary<string, SpriteCatalog> groups =
        new Dictionary<string, SpriteCatalog>();

    // 每次成功取得包引用都记一笔，同一个包可以出现多次。
    private readonly List<string> heldBundles = new List<string>();

    [SerializeField]private SpriteBundleSettings settings;
    private Downloader downloader;

    private IEnumerator Start()
    {
        if (Instance != this) yield break;

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

        settings = LoadAsset<SpriteBundleSettings>(
            SettingsBundle, SettingsAsset);
        return settings != null;
    }

    private SpriteCatalog FindGroup(string id)
    {
        if (!Init())
        {
            Debug.LogWarning("精灵目录尚未初始化，请确认热更已完成且设置可加载。", this);
            return null;
        }

        if (groups.TryGetValue(id, out SpriteCatalog catalog) && catalog != null)
            return catalog;

        if (settings.groups == null) return null;

        foreach (SpriteBundleSettings.Group group in settings.groups)
        {
            if (group == null || group.id != id) continue;

            catalog = LoadAsset<SpriteCatalog>(group.bundle, group.catalog);
            if (catalog == null) return null;

            // 只缓存成功结果，失败后再次查询仍可重试。
            groups[id] = catalog;
            return catalog;
        }

        Debug.LogError($"没有配置精灵资源组：{id}", this);
        return null;
    }

    public Sprite GetSprite(string catalogId, string spriteId)
    {
        if (string.IsNullOrWhiteSpace(catalogId) ||
            string.IsNullOrWhiteSpace(spriteId))
        {
            Debug.LogError("Catalog ID 和 Sprite ID 不能为空。", this);
            return null;
        }

        SpriteCatalog catalog = FindGroup(catalogId);
        if (catalog == null) return null;

        if (catalog.TryGet(spriteId, out Sprite sprite))
            return sprite;

        Debug.LogError($"资源组 {catalogId} 中没有有效精灵：{spriteId}", this);
        return null;
    }

    // 包与资产都成功才保留持有记录；读资产失败则归还本次包引用。
    private T LoadAsset<T>(string bundleName, string assetName)
        where T : UnityEngine.Object
    {
        if (string.IsNullOrWhiteSpace(bundleName) ||
            string.IsNullOrWhiteSpace(assetName))
        {
            Debug.LogError("精灵配置中的包名或资源地址为空。", this);
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
