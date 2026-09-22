using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// prefab 缓存。按名字存住已经加载过的 prefab，避免同一个资源反复走 Resources / AB 加载。
/// </summary>
public class GameAsset
{
    private Dictionary<string, GameObject> prefabs = new Dictionary<string, GameObject>();

    /// <summary>
    /// 取一个 prefab。loadFromLocal 为 true 时从 Resources 取（本地调试用），
    /// 否则从指定的 AB 包里取。取到的会缓存下来，下次同名直接返回。
    /// </summary>
    public GameObject GetPrefab(bool loadFromLocal, string bundleName, string prefabName)
    {
        if (prefabs.TryGetValue(prefabName, out GameObject prefab))
        {
            return prefab;
        }

        if (loadFromLocal)
        {
            string path = "/Prefabs/" + prefabName;
            prefab = Resources.Load<GameObject>(path);
            if (prefab == null)
            {
                Debug.Log($"Resources文件夹里没有该文件{prefabName}");
                return null;
            }

            prefabs[prefabName] = prefab;
            return prefab;
        }

        prefab = Downloader.Instance.GetAsset<GameObject>(bundleName, prefabName);
        if (prefab == null)
        {
            Debug.Log($"AB包中未找到该文件{prefabName}");
            return null;
        }

        prefabs[prefabName] = prefab;
        return prefab;
    }
}
