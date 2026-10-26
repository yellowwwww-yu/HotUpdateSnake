using System;
using System.Collections.Generic;
using Unity.VisualScripting;
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
        // 全部 UIBind 共用点击音缓存；加载器或 Downloader 更换后重新取用。
        private static AudioClip clickClip;
        private static AudioBundleLoader clickResources;
        private static Downloader clickDownloader;

        private Dictionary<string, GameObject> lookup = new Dictionary<string, GameObject>();

        /// <summary>面板返回自己的 Lua 模块表（按钮回调往它上面转发）；列表格子没有模块，返回 null</summary>
        protected virtual LuaTable Owner => null;

        private void Awake()
        {
            InitLookup(transform);
        }

        // 建查找表不需要 Lua 参与，所以放在 Awake。
        // 格子从池里复用时 Awake 不会重跑，这张表建一次就够了。
        private void InitLookup(Transform transform)
        {
            for(int i = 0; i<transform.childCount; i++)
            {
                GameObject child = transform.GetChild(i).gameObject;
                if (lookup.ContainsKey(child.name))
                {
                    Debug.Log($"发现同名对象{child.name}，已跳过");
                    continue;
                }
                lookup[child.name] = child;

                InitLookup(child.transform);
            }
        }

        /// <summary>
        /// 取引用。返回的是 GameObject，xLua 会按运行时类型推给 Lua，
        /// 所以 Lua 侧可以直接当原本的类型用：self.view:Get("Txt_Name"):GetComponent("Text").text = "abc"
        /// </summary>
        public GameObject Get(string key)
        {
            if (!lookup.TryGetValue(key, out GameObject o))
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
            if (!lookup.TryGetValue(key, out GameObject o))
            {
                Debug.Log($"不存在key{key}");
                return;
            }

            if (string.IsNullOrEmpty(method))
            {
                Debug.Log("方法名不能为空");
                return;
            }

            Button button = o.GetComponent<Button>();

            if (button == null)
            {
                Debug.Log($"{key}不是button");
                return;
            }

            PrepareClickSound();

            // 格子会被池复用，不先清监听器会越叠越多，表现是点一次触发多次
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() =>
            {
                PlayClickSound();
                CallModule(method);
            });
        }

        /// <summary>给 ScrollListView 用：把格子里的按钮统一接到一个出口，回调会带上 key</summary>
        internal void BindAllButton(Action<UIBind, string> handler)
        {
            PrepareClickSound();
            foreach (KeyValuePair<string,GameObject> pair in lookup)
            {
                Button button = pair.Value.GetComponent<Button>();
                if (button == null)
                {
                    continue;
                }

                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() =>
                {
                    PlayClickSound();
                    handler(this, pair.Key);
                });
            }
        }

        // 通用界面反馈：不判断按钮业务结果，也不替代原有点击回调。
        private static void PrepareClickSound()
        {
            var resources = AudioBundleLoader.Instance;
            var downloader = Downloader.Instance;
            if (resources == null || downloader == null || !downloader.isReady)
            {
                clickResources = null;
                clickDownloader = null;
                clickClip = null;
                return;
            }

            if (clickResources != resources || clickDownloader != downloader)
            {
                clickResources = resources;
                clickDownloader = downloader;
                clickClip = null;
            }

            // 成功后直接复用引用；失败不缓存，后续点击仍可重试。
            if (clickClip == null)
                clickClip = resources.GetClip("common", "ui_button_click");

        }

        // 常规情况下只用预取缓存；资源管理器重建后也能在这里恢复。
        private static void PlayClickSound()
        {
            var player = Framework.Audio.AudioManager.Instance;
            if (player == null || !player.isActiveAndEnabled) return;

            PrepareClickSound();
            if (clickClip != null && clickClip.loadState == AudioDataLoadState.Loaded)
                player.PlaySound(clickClip);
        }

        public void BindSliderChanged(string key, string method)
        {
            GameObject node = Get(key);
            if (node == null || string.IsNullOrEmpty(method)) return;
        
            Slider slider = node.GetComponent<Slider>();
            if (slider == null) return;
        
            slider.onValueChanged.RemoveAllListeners();
            slider.onValueChanged.AddListener(value => CallModule(method, value));
        }
        
        public void UnBindSliderChanged(string key)
        {
            GameObject node = Get(key);
            if (node == null) return;
        
            Slider slider = node.GetComponent<Slider>();
            if (slider != null) slider.onValueChanged.RemoveAllListeners();
        }
        

        public void BindToggleChanged(string key, string method)
        {
            GameObject node = Get(key);
            if (node == null || string.IsNullOrEmpty(method)) return;
            Toggle toggle = node.GetComponent<Toggle>();
            if (toggle == null) return;
            toggle.onValueChanged.RemoveAllListeners();
            toggle.onValueChanged.AddListener(value => CallModule(method, value));
        }
        
        public void UnBindToggleChanged(string key)
        {
            GameObject node = Get(key);
            if (node == null) return;
            Toggle toggle = node.GetComponent<Toggle>();
            if (toggle != null) toggle.onValueChanged.RemoveAllListeners();
        }
        
        public void BindDropdownChanged(string key, string method)
        {
            GameObject node = Get(key);
            if (node == null || string.IsNullOrEmpty(method)) return;
            Dropdown dropdown = node.GetComponent<Dropdown>();
            if (dropdown == null) return;
            dropdown.onValueChanged.RemoveAllListeners();
            dropdown.onValueChanged.AddListener(value => CallModule(method, value));
        }
        
        public void UnBindDropdownChanged(string key)
        {
            GameObject node = Get(key);
            if (node == null) return;
            Dropdown dropdown = node.GetComponent<Dropdown>();
            if (dropdown != null) dropdown.onValueChanged.RemoveAllListeners();
        }
        

        public void BindInputChanged(string key, string method)
        {
            GameObject inputObj = Get(key);
            if(inputObj == null)
            {
                Debug.Log($"未找到{key}");
                return;
            }
            InputField input = inputObj.GetComponent<InputField>();
            if(input == null)
            {
                Debug.Log($"{key}上没有InputField组件");
                return;
            }

            input.onValueChanged.RemoveAllListeners();
            input.onValueChanged.AddListener(value => CallModule(method, value));
        }

        public void UnBindInputChanged(string key)
        {
            GameObject inputObj = Get(key);
            if(inputObj == null)
            {
                Debug.Log($"未找到{key}");
                return;
            }
            InputField input = inputObj.GetComponent<InputField>();
            if(input == null)
            {
                Debug.Log($"{key}上没有InputField组件");
                return;
            }

            input.onValueChanged.RemoveAllListeners();
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
