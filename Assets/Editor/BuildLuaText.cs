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

        // 先清掉上一轮的产物：源文件被删除或改名后，旧 .lua.txt 会变成包里的"幽灵脚本"，
        // 代码里明明已经没有它了，require 却还能拿到旧逻辑。
        // 只删文件、不删目录 —— 子目录的 .meta 里存着 AB 名，把目录删掉就得回 Inspector 重设一次。
        int cleanedCount = 0;
        foreach (string staleFile in Directory.GetFiles(targetFolder, "*.lua.txt", SearchOption.AllDirectories))
        {
            File.Delete(staleFile);
            cleanedCount++;
        }

        // 递归获取所有 .lua 文件
        string[] luaFiles = Directory.GetFiles(sourceFolder, "*.lua", SearchOption.AllDirectories);

        int copiedCount = 0;
        foreach (string filePath in luaFiles)
        {
            // 保留源目录的相对层级：UI/Login.lua -> LuaText/UI/Login.lua.txt
            // 结构镜像之后 require 路径 / 报错行号 / 磁盘文件三者能直接对上，
            // 并且"给文件夹设 AB 名"就成了分包手段（目录即包）。
            string relativePath = filePath.Substring(sourceFolder.Length + 1);
            string targetFilePath = Path.Combine(targetFolder, relativePath) + ".txt";

            // 子目录要现建
            Directory.CreateDirectory(Path.GetDirectoryName(targetFilePath));

            // 覆盖复制到目标文件
            File.Copy(filePath, targetFilePath, true);
            copiedCount++;
        }

        // 刷新 AssetDatabase，使 Unity 识别新文件（目标文件夹在项目内）
        AssetDatabase.Refresh();

        Debug.Log($"已复制 {copiedCount} 个 .lua 到 {targetFolder}（保留子目录结构），清理旧产物 {cleanedCount} 个。");
    }
}
#endif