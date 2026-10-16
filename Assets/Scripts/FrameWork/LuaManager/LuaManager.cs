using System;
using System.IO;
using System.Text;
using UnityEngine;
using XLua;

/// <summary>
/// Lua 环境管理：创建 LuaEnv、接管 require 的文件读取、对外提供执行模块的接口。
/// </summary>
public class LuaManager : Singleton<LuaManager>
{
    // Lua 脚本在 Assets 下的根路径，AB 里登记的完整路径以它开头
    private const string LuaAssetRoot = "Assets/LuaScripts/LuaText/";

    // 磁盘上的根路径（Local 模式用），相对 Application.dataPath
    private const string LuaLocalRoot = "LuaScripts/LuaText";

    private enum LoadingMethod
    {
        Local,
        AssetBundle
    }

    // 存放全部 lua 脚本的 AB 包名
    [SerializeField] private string bundleName = "lua";

    private LuaEnv luaEnv;

    [SerializeField] private LoadingMethod loadMethod = LoadingMethod.AssetBundle;

    protected override void Awake()
    {
        base.Awake();

        luaEnv = new LuaEnv();
        luaEnv.AddLoader(MyLoader);
    }

    // 自定义加载器：Lua 侧执行 require 时回调这里取脚本内容
    private byte[] MyLoader(ref string filePath)
    {
        // require('UI.LoginPanel') -> UI/LoginPanel.lua.txt
        string relative = filePath.Replace(".", "/") + ".lua.txt";

        if (loadMethod == LoadingMethod.Local)
        {
            string path = Path.Combine(Application.dataPath, LuaLocalRoot, relative);
            if (File.Exists(path))
            {
                return File.ReadAllBytes(path);
            }
            Debug.Log($"未在本地找到文件{relative}");
            return null;
        }

        // 传完整路径：AB 里登记的就是这条路径，不会出现同名文件命中歧义
        string assetPath = LuaAssetRoot + relative;

        TextAsset lua = Downloader.Instance.GetAsset<TextAsset>(bundleName, assetPath);
        if (lua != null)
        {
            return Encoding.UTF8.GetBytes(lua.text);
        }

        Debug.Log($"未在{bundleName}包中找到{assetPath}");
        return null;
    }

    /// <summary>
    /// require 一个 Lua 模块，返回它的模块表，失败返回 null。
    /// 面板等需要长期持有模块表的场合用它；只是跑一遍脚本的话用 DoFile 就行。
    /// </summary>
    /// <param name="forceReload">true 时先清 package.loaded 缓存，强制重新执行 chunk</param>
    public LuaTable RequireModule(string moduleName, bool forceReload = false)
    {
        if (string.IsNullOrEmpty(moduleName))
        {
            return null;
        }

        if (luaEnv == null)
        {
            Debug.LogError($"luaEnv 不存在，无法 require {moduleName}");
            return null;
        }

        if (forceReload)
        {
            ClearModule(moduleName);
        }

        try
        {
            object[] results = luaEnv.DoString($"return require('{moduleName}')");
            if (results == null || results.Length == 0)
            {
                return null;
            }

            return results[0] as LuaTable;
        }
        catch (Exception e)
        {
            Debug.LogError($"require('{moduleName}') 执行失败：{e.Message}");
            return null;
        }
    }

    /// <summary>
    /// 执行一个 Lua 模块，不保留模块表。
    /// forceReload 用于重复进场景：require 命中 package.loaded 会直接返回，
    /// 不清缓存的话第二次进来就什么都没执行。
    /// </summary>
    public void DoFile(string fileName, bool forceReload = false)
    {
        LuaTable module = RequireModule(fileName, forceReload);
        module?.Dispose();
    }

    /// <summary>清掉某个 Lua 模块的缓存，需要在它所在的 AB 包卸载之前调用。</summary>
    public void ClearModule(string fileName)
    {
        if (luaEnv == null)
        {
            return;
        }

        luaEnv.DoString($"package.loaded['{fileName}'] = nil");
    }

    /// <summary>取一个全局函数，一般是 Lua 侧注册的 Main / Game_Start 这类入口。</summary>
    public LuaFunction GetLuaFunction(string funcName)
    {
        return luaEnv.Global.Get<LuaFunction>(funcName);
    }

    /// <summary>把 C# 对象注入成 Lua 全局变量，Lua 里直接用这个名字访问。</summary>
    public void Inject<T>(string name, T obj)
    {
        luaEnv.Global.Set(name, obj);
    }

    public LuaTable NewLuaTable()
    {
        return luaEnv.NewTable();
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();   // 清掉 Singleton 里的静态引用
        luaEnv?.Dispose();
        luaEnv = null;
        // lua 是常驻包，加载和释放统一由 CommonBundleLoader 负责，这里不做 UnloadAB
    }
}
