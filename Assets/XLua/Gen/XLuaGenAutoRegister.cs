#if USE_UNI_LUA
using LuaAPI = UniLua.Lua;
using RealStatePtr = UniLua.ILuaState;
using LuaCSFunction = UniLua.CSharpFunctionDelegate;
#else
using LuaAPI = XLua.LuaDLL.Lua;
using RealStatePtr = System.IntPtr;
using LuaCSFunction = XLua.LuaDLL.lua_CSFunction;
#endif

using System;
using System.Collections.Generic;
using System.Reflection;


namespace XLua.CSObjectWrap
{
    public class XLua_Gen_Initer_Register__
	{
        
        
        static void wrapInit0(LuaEnv luaenv, ObjectTranslator translator)
        {
        
            translator.DelayWrapLoader(typeof(AudioBundleLoader), AudioBundleLoaderWrap.__Register);
        
        
            translator.DelayWrapLoader(typeof(LocalBagItemData), LocalBagItemDataWrap.__Register);
        
        
            translator.DelayWrapLoader(typeof(LocalBagData), LocalBagDataWrap.__Register);
        
        
            translator.DelayWrapLoader(typeof(LocalBagLoadResult), LocalBagLoadResultWrap.__Register);
        
        
            translator.DelayWrapLoader(typeof(LocalBagFile), LocalBagFileWrap.__Register);
        
        
            translator.DelayWrapLoader(typeof(BagListView), BagListViewWrap.__Register);
        
        
            translator.DelayWrapLoader(typeof(RankListView), RankListViewWrap.__Register);
        
        
            translator.DelayWrapLoader(typeof(LocalPlayerData), LocalPlayerDataWrap.__Register);
        
        
            translator.DelayWrapLoader(typeof(LocalAccountData), LocalAccountDataWrap.__Register);
        
        
            translator.DelayWrapLoader(typeof(LocalAccountFile), LocalAccountFileWrap.__Register);
        
        
            translator.DelayWrapLoader(typeof(ScreenBrightness), ScreenBrightnessWrap.__Register);
        
        
            translator.DelayWrapLoader(typeof(SettingsStorage), SettingsStorageWrap.__Register);
        
        
            translator.DelayWrapLoader(typeof(Tutorial.BaseClass), TutorialBaseClassWrap.__Register);
        
        
            translator.DelayWrapLoader(typeof(Tutorial.TestEnum), TutorialTestEnumWrap.__Register);
        
        
            translator.DelayWrapLoader(typeof(Tutorial.DerivedClass), TutorialDerivedClassWrap.__Register);
        
        
            translator.DelayWrapLoader(typeof(Tutorial.ICalc), TutorialICalcWrap.__Register);
        
        
            translator.DelayWrapLoader(typeof(Tutorial.DerivedClassExtensions), TutorialDerivedClassExtensionsWrap.__Register);
        
        
            translator.DelayWrapLoader(typeof(Framework.SceneLoader), FrameworkSceneLoaderWrap.__Register);
        
        
            translator.DelayWrapLoader(typeof(Framework.UI.UIBind), FrameworkUIUIBindWrap.__Register);
        
        
            translator.DelayWrapLoader(typeof(Framework.UI.UIManager), FrameworkUIUIManagerWrap.__Register);
        
        
            translator.DelayWrapLoader(typeof(Framework.UI.PopupView), FrameworkUIPopupViewWrap.__Register);
        
        
            translator.DelayWrapLoader(typeof(Framework.Pool.PoolManager), FrameworkPoolPoolManagerWrap.__Register);
        
        
            translator.DelayWrapLoader(typeof(Framework.Audio.AudioManager), FrameworkAudioAudioManagerWrap.__Register);
        
        
            translator.DelayWrapLoader(typeof(Tutorial.DerivedClass.TestEnumInner), TutorialDerivedClassTestEnumInnerWrap.__Register);
        
        
        
        }
        
        static void Init(LuaEnv luaenv, ObjectTranslator translator)
        {
            
            wrapInit0(luaenv, translator);
            
            
            translator.AddInterfaceBridgeCreator(typeof(Tutorial.CSCallLua.ItfD), TutorialCSCallLuaItfDBridge.__Create);
            
        }
        
	    static XLua_Gen_Initer_Register__()
        {
		    XLua.LuaEnv.AddIniter(Init);
		}
		
		
	}
	
}
namespace XLua
{
	public partial class ObjectTranslator
	{
		static XLua.CSObjectWrap.XLua_Gen_Initer_Register__ s_gen_reg_dumb_obj = new XLua.CSObjectWrap.XLua_Gen_Initer_Register__();
		static XLua.CSObjectWrap.XLua_Gen_Initer_Register__ gen_reg_dumb_obj {get{return s_gen_reg_dumb_obj;}}
	}
	
	internal partial class InternalGlobals
    {
	    
	    static InternalGlobals()
		{
		    extensionMethodMap = new Dictionary<Type, IEnumerable<MethodInfo>>()
			{
			    
			};
			
			genTryArrayGetPtr = StaticLuaCallbacks.__tryArrayGet;
            genTryArraySetPtr = StaticLuaCallbacks.__tryArraySet;
		}
	}
}
