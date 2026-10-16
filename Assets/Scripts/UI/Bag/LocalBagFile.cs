using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;
using XLua;

[Serializable]
[LuaCallCSharp]
public class LocalBagItemData
{
    public int id;
    public int count;
}

[Serializable]
[LuaCallCSharp]
public class LocalBagData
{
    public int version;
    public string account;
    public List<LocalBagItemData> items;
}

[LuaCallCSharp]
public class LocalBagLoadResult
{
    public bool success;
    public LocalBagData data;
    public string error;
}

[LuaCallCSharp]
public static class LocalBagFile
{
    private const int Version = 1;
    private static readonly string SaveDirectory =
        Path.Combine(Application.persistentDataPath, "bags");
    private static readonly Encoding Utf8 = new UTF8Encoding(false, true);

    public static LocalBagData Create(string account)
    {
        return new LocalBagData
        {
            version = Version,
            account = account,
            items = new List<LocalBagItemData>()
        };
    }

    public static string GetFilePath(string account)
    {
        if (string.IsNullOrEmpty(account))
            throw new ArgumentException("账号不能为空");

        // 不直接用账号作文件名，避免账号中的路径字符影响存档位置。
        using (SHA256 sha = SHA256.Create())
        {
            byte[] hash = sha.ComputeHash(Utf8.GetBytes(account));
            StringBuilder key = new StringBuilder(hash.Length * 2);
            foreach (byte value in hash)
                key.Append(value.ToString("x2"));

            return Path.Combine(SaveDirectory, key + ".json");
        }
    }

    public static LocalBagLoadResult Load(string account)
    {
        try
        {
            string json = File.ReadAllText(GetFilePath(account), Utf8);
            LocalBagData data = JsonUtility.FromJson<LocalBagData>(json);
            Validate(data, account);
            return new LocalBagLoadResult { success = true, data = data };
        }
        catch (FileNotFoundException)
        {
            // success=true、data=null 专门表示首次没有存档。
            return new LocalBagLoadResult { success = true };
        }
        catch (DirectoryNotFoundException)
        {
            return new LocalBagLoadResult { success = true };
        }
        catch (Exception ex)
        {
            return new LocalBagLoadResult
            {
                success = false,
                error = "背包读取失败：" + ex.Message
            };
        }
    }

    // 返回null表示保存成功；返回字符串表示失败原因。
    public static string Save(LocalBagData data)
    {
        try
        {
            Validate(data, data == null ? null : data.account);
            string filePath = GetFilePath(data.account);
            Directory.CreateDirectory(SaveDirectory);

            string tempPath = filePath + ".tmp";
            File.WriteAllText(tempPath, JsonUtility.ToJson(data, true), Utf8);
            if (File.Exists(filePath))
                File.Replace(tempPath, filePath, null);
            else
                File.Move(tempPath, filePath);

            return null;
        }
        catch (Exception ex)
        {
            return "背包保存失败：" + ex.Message;
        }
    }

    private static void Validate(LocalBagData data, string account)
    {
        if (data == null)
            throw new InvalidDataException("存档内容为空");
        if (data.version != Version)
            throw new InvalidDataException("不支持的背包存档版本");
        if (string.IsNullOrEmpty(data.account) || data.account != account)
            throw new InvalidDataException("存档账号不匹配");
        if (data.items == null)
            throw new InvalidDataException("存档缺少物品列表");

        HashSet<int> ids = new HashSet<int>();
        foreach (LocalBagItemData item in data.items)
        {
            if (item == null || item.id <= 0 || item.count <= 0)
                throw new InvalidDataException("存档存在无效物品或数量");
            if (!ids.Add(item.id))
                throw new InvalidDataException("存档存在重复物品ID");
        }
    }
}
