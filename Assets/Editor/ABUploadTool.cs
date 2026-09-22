#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 把打包好的 AssetBundle 文件夹整个拷到本地服务器目录。
/// 路径写死，菜单点一下就拷，不做窗口。
/// </summary>
public static class ABUploadTool
{
    // 源：相对项目根
    private const string SourceDir = @"AssetBundles/PC";

    // 目标：本地服务器上的物理路径（NetBox2 站点根 E:\WebServer）
    private const string TargetDir = @"E:\WebServer\热更新贪吃蛇\AssetBundles\PC";

    [MenuItem("热更新/上传AB到服务器")]
    public static void Upload()
    {
        string src = Path.GetFullPath(Path.Combine(Application.dataPath, "..", SourceDir));

        if (!Directory.Exists(src))
        {
            Debug.LogError("源文件夹不存在：" + src);
            return;
        }

        Directory.CreateDirectory(TargetDir);

        string[] files = Directory.GetFiles(src, "*", SearchOption.AllDirectories);
        foreach (string file in files)
        {
            string rel = file.Substring(src.Length).TrimStart('\\', '/');
            string dst = Path.Combine(TargetDir, rel);

            Directory.CreateDirectory(Path.GetDirectoryName(dst));
            File.Copy(file, dst, true);
        }

        Debug.Log($"上传完成：{files.Length} 个文件 → {TargetDir}");
    }
}
#endif
