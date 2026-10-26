#if USE_UNI_LUA
using LuaAPI = UniLua.Lua;
using RealStatePtr = UniLua.ILuaState;
using LuaCSFunction = UniLua.CSharpFunctionDelegate;
#else
using LuaAPI = XLua.LuaDLL.Lua;
using RealStatePtr = System.IntPtr;
using LuaCSFunction = XLua.LuaDLL.lua_CSFunction;
#endif

using XLua;
using System.Collections.Generic;


namespace XLua.CSObjectWrap
{
    using Utils = XLua.Utils;
    public class SettingsStorageWrap 
    {
        public static void __Register(RealStatePtr L)
        {
			ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
			System.Type type = typeof(SettingsStorage);
			Utils.BeginObjectRegister(type, L, translator, 0, 0, 0, 0);
			
			
			
			
			
			
			Utils.EndObjectRegister(type, L, translator, null, null,
			    null, null, null);

		    Utils.BeginClassRegister(type, L, __CreateInstance, 3, 5, 3);
			Utils.RegisterFunc(L, Utils.CLS_IDX, "SetDisplay", _m_SetDisplay_xlua_st_);
            Utils.RegisterFunc(L, Utils.CLS_IDX, "Save", _m_Save_xlua_st_);
            
			
            
			Utils.RegisterFunc(L, Utils.CLS_GETTER_IDX, "MusicVolume", _g_get_MusicVolume);
            Utils.RegisterFunc(L, Utils.CLS_GETTER_IDX, "SoundVolume", _g_get_SoundVolume);
            Utils.RegisterFunc(L, Utils.CLS_GETTER_IDX, "Brightness", _g_get_Brightness);
            Utils.RegisterFunc(L, Utils.CLS_GETTER_IDX, "WindowWidth", _g_get_WindowWidth);
            Utils.RegisterFunc(L, Utils.CLS_GETTER_IDX, "WindowHeight", _g_get_WindowHeight);
            
			Utils.RegisterFunc(L, Utils.CLS_SETTER_IDX, "MusicVolume", _s_set_MusicVolume);
            Utils.RegisterFunc(L, Utils.CLS_SETTER_IDX, "SoundVolume", _s_set_SoundVolume);
            Utils.RegisterFunc(L, Utils.CLS_SETTER_IDX, "Brightness", _s_set_Brightness);
            
			
			Utils.EndClassRegister(type, L, translator);
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int __CreateInstance(RealStatePtr L)
        {
            return LuaAPI.luaL_error(L, "SettingsStorage does not have a constructor!");
        }
        
		
        
		
        
        
        
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _m_SetDisplay_xlua_st_(RealStatePtr L)
        {
		    try {
            
            
            
                
                {
                    int _width = LuaAPI.xlua_tointeger(L, 1);
                    int _height = LuaAPI.xlua_tointeger(L, 2);
                    bool _fullScreen = LuaAPI.lua_toboolean(L, 3);
                    
                    SettingsStorage.SetDisplay( _width, _height, _fullScreen );
                    
                    
                    
                    return 0;
                }
                
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _m_Save_xlua_st_(RealStatePtr L)
        {
		    try {
            
            
            
                
                {
                    
                        var gen_ret = SettingsStorage.Save(  );
                        LuaAPI.lua_pushboolean(L, gen_ret);
                    
                    
                    
                    return 1;
                }
                
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            
        }
        
        
        
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _g_get_MusicVolume(RealStatePtr L)
        {
		    try {
            
			    LuaAPI.lua_pushnumber(L, SettingsStorage.MusicVolume);
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            return 1;
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _g_get_SoundVolume(RealStatePtr L)
        {
		    try {
            
			    LuaAPI.lua_pushnumber(L, SettingsStorage.SoundVolume);
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            return 1;
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _g_get_Brightness(RealStatePtr L)
        {
		    try {
            
			    LuaAPI.lua_pushnumber(L, SettingsStorage.Brightness);
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            return 1;
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _g_get_WindowWidth(RealStatePtr L)
        {
		    try {
            
			    LuaAPI.xlua_pushinteger(L, SettingsStorage.WindowWidth);
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            return 1;
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _g_get_WindowHeight(RealStatePtr L)
        {
		    try {
            
			    LuaAPI.xlua_pushinteger(L, SettingsStorage.WindowHeight);
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            return 1;
        }
        
        
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _s_set_MusicVolume(RealStatePtr L)
        {
		    try {
                
			    SettingsStorage.MusicVolume = (float)LuaAPI.lua_tonumber(L, 1);
            
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            return 0;
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _s_set_SoundVolume(RealStatePtr L)
        {
		    try {
                
			    SettingsStorage.SoundVolume = (float)LuaAPI.lua_tonumber(L, 1);
            
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            return 0;
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _s_set_Brightness(RealStatePtr L)
        {
		    try {
                
			    SettingsStorage.Brightness = (float)LuaAPI.lua_tonumber(L, 1);
            
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            return 0;
        }
        
		
		
		
		
    }
}
