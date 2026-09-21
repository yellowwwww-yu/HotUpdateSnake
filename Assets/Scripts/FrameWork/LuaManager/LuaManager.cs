using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using XLua;

public class LuaManager : Singleton<LuaManager>
{
    /// <summary>Lua 脚本在 Assets 下的根目录 —— AB 内登记的全路径以此开头</summary>
    private const string LuaAssetRoot = "Assets/LuaScripts/LuaText/";

    /// <summary>磁盘上的根目录（Local 模式用），相对 Application.dataPath</summary>
    private const string LuaLocalRoot = "LuaScripts/LuaText";

    private enum LoadingMethod
    {
        Local,
        AssetBundle
    }

    /// <summary>Lua 脚本的分包路由：相对路径命中 prefix 的脚本去 bundle 包里取</summary>
    [Serializable]
    public class BundleRule
    {
        public string prefix;   // 如 UI
        public string bundle;   // 如 ui_lua
    }

    [SerializeField] private string bundleName = "lua";

    [Header("分包路由：命中 prefix 的脚本去对应包取，未命中走上面的 bundleName")]
    [SerializeField] private BundleRule[] bundleRules;

    private LuaEnv luaEnv;
    [SerializeField]private LoadingMethod loadMethod = LoadingMethod.AssetBundle;

    protected override void Awake()
    {
        base.Awake();
        
        luaEnv = new LuaEnv();
        luaEnv.AddLoader(MyLoader);
    }

    private byte[] MyLoader(ref string filePath)
    {
        // require('UI.LoginPanel') -> UI/LoginPanel.lua.txt
        string relative = filePath.Replace(".","/") + ".lua.txt";

        if(loadMethod == LoadingMethod.Local)
        {
            string path = Path.Combine(Application.dataPath, LuaLocalRoot, relative);
            if (File.Exists(path))
            {
                return File.ReadAllBytes(path);
            }
            Debug.Log($"未在本地找到文件{relative}");
            return null;
        }

        // 传全路径：AB 里登记的就是这条序列化路径，精确命中，
        // 不存在"不同目录下有同名文件时短名命中歧义"的问题。
        string assetPath = LuaAssetRoot + relative;
        string bundle = ResolveBundle(relative);

        TextAsset lua = Downloader.Instance.GetAsset<TextAsset>(bundle, assetPath);
        if(lua != null)
        {
            return Encoding.UTF8.GetBytes(lua.text);
        }
        Debug.Log($"未在{bundle}包中找到{assetPath}");
        return null;
    }

    /// <summary>
    /// 按前缀决定去哪个 AB 包取脚本。没配规则、或没命中任何规则时走常驻的 bundleName。
    /// 例：规则 {prefix = "UI", bundle = "ui_lua"}，则 require('UI.LoginPanel') 去 ui_lua 包找。
    /// </summary>
    private string ResolveBundle(string relative)
    {
        if (bundleRules != null)
        {
            foreach (BundleRule rule in bundleRules)
            {
                if (relative.StartsWith(rule.prefix + "/"))
                {
                    return rule.bundle;
                }
            }
        }

        return bundleName;
    }

    /// <summary>
    /// 执行一个 Lua 模块。
    /// forceReload = true 时先清 package.loaded 缓存再 require —— require 命中缓存会直接返回，
    /// 不重新执行 chunk，重进场景时表现就是"写了 DoFile 却什么都没发生"。
    /// </summary>
    public void DoFile(string fileName, bool forceReload = false)
    {
        if (forceReload)
        {
            ClearModule(fileName);
        }

        luaEnv.DoString($"require('{fileName}')");
    }

    /// <summary>清掉某个 Lua 模块的缓存。必须在它所在 AB 包卸载之前调用。</summary>
    public void ClearModule(string fileName)
    {
        luaEnv.DoString($"package.loaded['{fileName}'] = nil");
    }

    public LuaFunction GetLuaFunction(string funcName)
    {
        return luaEnv.Global.Get<LuaFunction>(funcName);
    }

    public void Inject<T>(string name, T obj)
    {
        luaEnv.Global.Set(name, obj);
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();   // 清 Singleton.instance
        luaEnv?.Dispose();
        luaEnv = null;
        // lua 属常驻包：加载与释放统一由 CommonBundleLoader 负责，这里不再越权 UnloadAB
    }
}
