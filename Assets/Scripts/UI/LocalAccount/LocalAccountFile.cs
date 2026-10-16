using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using XLua;

[Serializable]
[LuaCallCSharp]
public class LocalPlayerData
{
    public string account;
    public string password;
    public string nick;
    public int level;
    public int avatar;
}


[Serializable]
[LuaCallCSharp]
public class LocalAccountData
{
    public int version;
    public List<LocalPlayerData> players;
}

[LuaCallCSharp]
public static class LocalAccountFile
{
    public static string FilePath => Path.Combine(Application.persistentDataPath, "local_accounts.json");
    public static LocalAccountData Load()
    {
        Debug.Log(FilePath);
        if (!File.Exists(FilePath))
        {
            return new LocalAccountData
            {
                version = 1,
                players = new List<LocalPlayerData>()
            };
        }

        string json = File.ReadAllText(FilePath, Encoding.UTF8);
        LocalAccountData data = JsonUtility.FromJson<LocalAccountData>(json);
        return data;
    }

    public static void Save(LocalAccountData data)
    {
        Directory.CreateDirectory(Application.persistentDataPath);

        // 先写临时文件，完整写入后再替换，避免直接截断已有账号文件。
        string tempPath = FilePath + ".tmp";
        File.WriteAllText(tempPath, JsonUtility.ToJson(data, true),
            new UTF8Encoding(false));

        if (File.Exists(FilePath))
        {
            File.Replace(tempPath, FilePath, null);
        }
        else
        {
            File.Move(tempPath, FilePath);
        }
    }
}
