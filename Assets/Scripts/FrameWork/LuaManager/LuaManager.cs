using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using XLua;

public class LuaManager : Singleton<LuaManager>
{
    private enum LoadingMethod
    {
        Local,
        AssetBundle
    }

    [SerializeField] private string bundleName = "lua";

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
        string fileName = filePath.Replace(".","/") + ".lua.txt";

        if(loadMethod == LoadingMethod.Local)
        {
            string path = Path.Combine(Application.dataPath, "LuaScripts/LuaText", fileName);
            if (File.Exists(path))
            {
                return File.ReadAllBytes(path);
            }
            Debug.Log($"未在本地找到文件{fileName}");
            return null;
        }

        TextAsset lua = Downloader.Instance.GetAsset<TextAsset>(bundleName, fileName);
        if(lua != null)
        {
            return Encoding.UTF8.GetBytes(lua.text);
        }
        Debug.Log($"未在{bundleName}包中找到{fileName}");
        return null;
    }

    public void DoFile(string fileName)
    {
        luaEnv.DoString($"require('{fileName}')");
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
