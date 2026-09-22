using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using XLua;

namespace Framework.UI
{
    /// <summary>
    /// 挂在面板根节点和列表格子 prefab 上，把 Inspector 里配好的引用交给 Lua 使用。
    /// 本身不含任何业务逻辑，按钮点了要做什么由 Lua 侧决定。
    /// </summary>
    [LuaCallCSharp]
    public class UIBind : MonoBehaviour
    {
        /// <summary>一条引用记录：key 是给 Lua 用的名字，target 是要拖的节点或组件</summary>
        [Serializable]
        public struct Binding
        {
            public string key;
            public UnityEngine.Object target;
        }

        [Header("引用绑定，key 一般直接填节点名")]
        [SerializeField]
        private Binding[] bindings;

        private Dictionary<string, UnityEngine.Object> lookup = new Dictionary<string, UnityEngine.Object>();

        /// <summary>面板返回自己的 Lua 模块表（按钮回调往它上面转发）；列表格子没有模块，返回 null</summary>
        protected virtual LuaTable Owner => null;

        private void Awake()
        {
            InitLookup();
        }

        // 建查找表不需要 Lua 参与，所以放在 Awake。
        // 格子从池里复用时 Awake 不会重跑，这张表建一次就够了。
        private void InitLookup()
        {
            if (bindings == null)
            {
                return;
            }

            foreach (Binding binding in bindings)
            {
                if (string.IsNullOrEmpty(binding.key) || binding.target == null)
                {
                    continue;
                }

                lookup[binding.key] = binding.target;
            }
        }

        /// <summary>
        /// 取引用。返回的是 UnityEngine.Object，xLua 会按运行时类型推给 Lua，
        /// 所以 Lua 侧可以直接当原本的类型用：self.view:Get("Txt_Name").text = "abc"
        /// </summary>
        public UnityEngine.Object Get(string key)
        {
            if (!lookup.TryGetValue(key, out UnityEngine.Object o))
            {
                Debug.Log($"不存在key{key}");
                return null;
            }

            return o;
        }

        /// <summary>
        /// 给按钮绑点击回调。method 是 Lua 模块里的方法名，显式传，不做 Btn_ 前缀推导。
        /// </summary>
        public void BindClick(string key, string method)
        {
            if (!lookup.TryGetValue(key, out UnityEngine.Object o))
            {
                Debug.Log($"不存在key{key}");
                return;
            }

            if (string.IsNullOrEmpty(method))
            {
                Debug.Log("方法名不能为空");
                return;
            }

            if (o is not Button button)
            {
                Debug.Log($"{key}不是button");
                return;
            }

            // 格子会被池复用，不先清监听器会越叠越多，表现是点一次触发多次
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => CallModule(method));
        }

        /// <summary>给 ScrollListView 用：把格子里的按钮统一接到一个出口，回调会带上 key</summary>
        internal void BindAllButton(Action<UIBind, string> handler)
        {
            foreach (Binding binding in bindings)
            {
                if (binding.target is not Button button)
                {
                    continue;
                }

                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => handler(this, binding.key));
            }
        }

        /// <summary>调用 Lua 模块上的方法，方法不存在时返回 false</summary>
        protected bool CallModule(string method, params object[] args)
        {
            if (string.IsNullOrEmpty(method) || Owner == null)
            {
                return false;
            }

            LuaFunction fn = Owner.Get<LuaFunction>(method);
            if (fn == null)
            {
                return false;
            }

            try
            {
                // Lua 的 obj:method(...) 等价于 method(obj, ...)，self 要自己补上
                int count = args.Length;
                object[] full = new object[count + 1];
                full[0] = Owner;
                for (int i = 0; i < args.Length; i++)
                {
                    full[i + 1] = args[i];
                }

                fn.Call(full);
                return true;
            }
            finally
            {
                fn.Dispose();
            }
        }

        protected virtual void OnDestroy()
        {
            lookup.Clear();
        }
    }
}
