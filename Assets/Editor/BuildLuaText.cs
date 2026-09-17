#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;

public class BuildLuaText
{
    [MenuItem("热更新/复制Lua文件为Txt")]
    public static void CopyLuaFilesAsTxt()
    {
        // 硬编码源/目标文件夹（相对项目根的路径）
        string sourceFolder = Path.Combine(Application.dataPath, "LuaScripts/Lua");
        string targetFolder = Path.Combine(Application.dataPath, "LuaScripts/LuaText");

        if (!Directory.Exists(sourceFolder))
        {
            Debug.LogWarning("源文件夹不存在：" + sourceFolder);
            return;
        }

        // 确保目标文件夹存在
        if (!Directory.Exists(targetFolder))
        {
            Directory.CreateDirectory(targetFolder);
        }

        // 递归获取所有 .lua 文件
        string[] luaFiles = Directory.GetFiles(sourceFolder, "*.lua", SearchOption.AllDirectories);

        int copiedCount = 0;
        foreach (string filePath in luaFiles)
        {
            // 直接用文件名拼接：X.lua -> X.lua.txt（铺平，不保留子目录）
            string targetFilePath = Path.Combine(targetFolder, Path.GetFileName(filePath)) + ".txt";

            // 覆盖复制到目标文件夹
            File.Copy(filePath, targetFilePath, true);
            copiedCount++;
        }

        // 刷新 AssetDatabase，使 Unity 识别新文件（目标文件夹在项目内）
        AssetDatabase.Refresh();

        Debug.Log($"已复制 {copiedCount} 个 .lua 文件到 {targetFolder}，并添加了 .txt 后缀（已铺平）。");
    }
}
#endif