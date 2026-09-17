using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Security.Cryptography;
using System.Text;

public static class HotUpdateManifestTool
{
    [MenuItem("热更新/生成更新清单")]
    public static void GenerateManifest()
    {
        string dir = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "AssetBundles", "PC"));

        if (!Directory.Exists(dir))
        {
            Debug.Log("请先打包AB包到对应路径，"+dir);
            return;
        }

        int version = 1;
        string manifestPath = Path.Combine(dir,"manifest.json");
        if (File.Exists(manifestPath))
        {
            HotUpdateManifest old = JsonUtility.FromJson<HotUpdateManifest>(File.ReadAllText(manifestPath));
            version = old.version + 1;
        }

        HotUpdateManifest manifest = new HotUpdateManifest
        {
            version = version,
            bundles = new List<BundleInfo>()
        };

        foreach(string filePath in Directory.GetFiles(dir))
        {
            string fileName = Path.GetFileName(filePath);

            if(fileName == "manifest.json" || fileName.EndsWith(".manifest"))
            {
                continue;
            }

            BundleInfo bundle = new BundleInfo
            {
                name = fileName,
                hash = GetMD5(filePath)
            };
            manifest.bundles.Add(bundle);
        }

        File.WriteAllText(manifestPath, JsonUtility.ToJson(manifest));

        Debug.Log($"已生成热更新对比文件，verson:{version}，共{manifest.bundles.Count}个包");
        Debug.Log("别忘记上传文件到服务器");
    }

    private static string GetMD5(string filePath)
    {
        using (MD5 md5 = MD5.Create())
        using (FileStream stream = File.OpenRead(filePath))
        {
            byte[] hash = md5.ComputeHash(stream);
            StringBuilder sb = new StringBuilder();
            foreach (byte b in hash)
            {
                sb.Append(b.ToString("x2"));
            }
            return sb.ToString();
        }
    }
}
