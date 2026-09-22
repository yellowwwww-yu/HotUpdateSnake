using UnityEngine;
using XLua;

namespace Framework.UI
{
    /// <summary>
    /// 面板根节点组件，在 UIBind 的基础上多一份 Lua 模块表。
    /// module 填 Lua 模块名（如 UI.LoginPanel），UIManager 打开面板时会 require 它并注入过来。
    /// </summary>
    public class UIPanel : UIBind
    {
        [Header("对应的 Lua 模块名，如 UI.LoginPanel")]
        [SerializeField]
        private string module;

        public string Module => module;

        private LuaTable lua;

        protected override LuaTable Owner => lua;

        /// <summary>把 Lua 模块表挂上来，同时把自身注入成模块的 view，Lua 里用 self.view 取引用</summary>
        public void AttachModule(LuaTable moduleTable)
        {
            lua = moduleTable;

            if (lua != null)
            {
                lua.Set("view", this);
            }
        }

        internal void CallOpen(LuaTable data)
        {
            if (!CallModule("OnOpen", data))
            {
                Debug.LogWarning($"[UI]{module}没有写OnOpen方法");
            }
        }

        internal void CallRefresh(LuaTable data)
        {
            if (!CallModule("OnRefresh", data))
            {
                Debug.LogWarning($"[UI]{module}没有写OnRefresh方法");
            }
        }

        internal void CallClose()
        {
            if (!CallModule("OnClose"))
            {
                Debug.LogWarning($"[UI]{module}没有写OnClose方法");
            }

            // 退订由框架统一触发，Lua 侧不用自己记得调
            if (!CallModule("ReleaseEvents"))
            {
                Debug.LogWarning($"[UI]{module}没有写ReleaseEvents方法");
            }
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            // LuaManager 可能已经先销毁了（退出播放时 Unity 不保证 OnDestroy 顺序），
            // 那时 luaEnv 已释放，再 Dispose 会报 lua env had disposed
            if (LuaManager.Instance != null)
            {
                lua?.Dispose();
            }

            lua = null;
        }
    }
}
