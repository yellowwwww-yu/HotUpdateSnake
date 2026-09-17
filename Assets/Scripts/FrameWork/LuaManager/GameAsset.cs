using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class GameAsset
{
    private Dictionary<string, GameObject> prefabs = new Dictionary<string, GameObject>();

    public GameObject GetPrefab(bool loadFromLocal, string bundleName, string prefabName)
    {
        if(prefabs.TryGetValue(prefabName, out GameObject prefab))
        {
            return prefab;
        }

        if (loadFromLocal)
        {
            string path = "/Prefabs/" + prefabName;
            prefab = Resources.Load<GameObject>(path);
            if(prefab == null)
            {
                Debug.Log($"Resources文件夹里没有该文件{prefabName}");
                return null;
            }
            prefabs[prefabName] = prefab;
            return prefab;
        }

        prefab = Downloader.Instance.GetAsset<GameObject>(bundleName, prefabName);
        if(prefab == null)
        {
            Debug.Log($"AB包中未找到该文件{prefabName}");
            return null;
        }
        prefabs[prefabName] = prefab;
        return prefab;
    }
}
