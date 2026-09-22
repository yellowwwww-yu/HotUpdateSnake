using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Security.Cryptography;
using System.Text;

/// <summary>
/// 生成热更新清单：扫一遍打包目录，把每个包的 MD5 写进 manifest.json。
/// 每次打包完跑一次，然后连同 AB 包一起上传到服务器。
/// </summary>
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

        // 版本号在上一版基础上加一，没有旧清单就从 1 开始
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

    /// <summary>算文件内容的 MD5，客户端靠它判断本地缓存的包是不是最新的</summary>
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
