using System;
using System.Collections.Generic;
using UnityEngine;
using XLua;

namespace Framework.UI
{
    /// <summary>UI 层级，决定渲染顺序和遮挡关系</summary>
    public enum UILayer
    {
        Background = 0,   // 主界面：登录、大厅
        Normal = 1,       // 普通面板：背包、排行榜
        Popup = 2,        // 弹窗：确认框、奖励
        Tips = 3,         // 飘字、Toast
    }

    /// <summary>
    /// 面板管理：从哪加载、放在哪层、开着还是缓存着。
    /// 只负责面板本身，不关心什么时候该开哪个面板，那是流程状态机的事。
    /// </summary>
    [LuaCallCSharp]
    public class UIManager : Singleton<UIManager>
    {
        [Serializable]
        public class PanelEntry
        {
            public string name;        // 逻辑名，Lua 侧用它开面板："Bag"
            public string bundle;      // 所在 AB 包："ui_bag"
            public string asset;       // 包内 prefab 名："UIPanel_Bag.prefab"
            public UILayer layer;
            public bool cache = true;  // 关闭后是否留在缓存里
        }

        [Header("四个层级容器，顺序必须和 UILayer 一致")]
        [SerializeField] private Transform[] layers;

        [Header("面板配置表")]
        [SerializeField] private PanelEntry[] panels;

        [Header("最多缓存几个面板，超出后淘汰最早缓存的")]
        [SerializeField] private int cacheCapacity = 5;

        private readonly Dictionary<string, PanelEntry> entries = new Dictionary<string, PanelEntry>();
        private readonly Dictionary<string, UIPanel> opened = new Dictionary<string, UIPanel>();
        private readonly Dictionary<string, UIPanel> cached = new Dictionary<string, UIPanel>();

        // 记录进入缓存的先后顺序。这里只淘汰"最早缓存的"，
        // 不需要精确的 LRU，用队列就够。
        private readonly Queue<string> cacheOrder = new Queue<string>();

        protected override void Awake()
        {
            base.Awake();

            entries.Clear();
            if (panels != null)
            {
                foreach (PanelEntry e in panels)
                {
                    if (e != null && !string.IsNullOrEmpty(e.name))
                    {
                        entries[e.name] = e;
                    }
                }
            }
        }

        // ==================== 给 Lua 用的接口 ====================

        public void Open(string name) => Open(name, null);

        public void Open(string name, LuaTable data)
        {
            if (!entries.TryGetValue(name, out PanelEntry entry))
            {
                Debug.LogError($"[UI] 没有配名为 {name} 的面板");
                return;
            }

            // 已经开着：只刷新，不重建
            if (opened.TryGetValue(name, out UIPanel open) && open != null)
            {
                open.gameObject.SetActive(true);
                open.transform.SetAsLastSibling();
                open.CallRefresh(data);
                return;
            }

            // 缓存里有就拿回来用，没有才创建
            UIPanel panel;
            if (cached.TryGetValue(name, out panel) && panel != null)
            {
                cached.Remove(name);
            }
            else
            {
                panel = Create(entry);
                if (panel == null)
                {
                    return;
                }
            }

            panel.gameObject.SetActive(true);
            panel.transform.SetAsLastSibling();   // 同层里最后打开的在最上面
            opened[name] = panel;
            panel.CallOpen(data);
        }

        public void Close(string name)
        {
            if (!opened.TryGetValue(name, out UIPanel panel) || panel == null)
            {
                opened.Remove(name);
                return;
            }

            opened.Remove(name);

            // 先让 Lua 收尾，再隐藏
            panel.CallClose();
            panel.gameObject.SetActive(false);

            if (entries.TryGetValue(name, out PanelEntry entry) && entry.cache)
            {
                AddToCache(name, panel);
            }
            else
            {
                Destroy(panel.gameObject);
                ReleaseBundle(entry);
            }
        }

        public void CloseAll()
        {
            List<string> names = new List<string>(opened.Keys);
            foreach (string n in names)
            {
                Close(n);
            }
        }

        public bool IsOpen(string name) => opened.ContainsKey(name);

        // ==================== 内部 ====================

        private UIPanel Create(PanelEntry entry)
        {
            if (Downloader.Instance == null)
            {
                Debug.LogError("[UI] Downloader 不存在（常驻场景没加载？）");
                return null;
            }

            // LoadBundle 让包引用 +1，GetAsset 只借不计数。
            // 记账规则是：面板实例活着，这个包就被引用着，关闭时对称减 1。
            if (Downloader.Instance.LoadBundle(entry.bundle) == null)
            {
                Debug.LogError($"[UI] 面板 {entry.name} 的包 {entry.bundle} 加载失败");
                return null;
            }

            GameObject prefab = Downloader.Instance.GetAsset<GameObject>(entry.bundle, entry.asset);
            if (prefab == null)
            {
                Debug.LogError($"[UI] 在 {entry.bundle} 里找不到 {entry.asset}");
                ReleaseBundle(entry);   // 回滚刚才那次 +1
                return null;
            }

            Transform parent = (layers != null && (int)entry.layer < layers.Length)
                ? layers[(int)entry.layer]
                : transform;

            GameObject instance = Instantiate(prefab, parent, false);

            UIPanel panel = instance.GetComponent<UIPanel>();
            if (panel == null)
            {
                Debug.LogError($"[UI] {entry.asset} 的根节点上没有挂 UIPanel");
                Destroy(instance);
                ReleaseBundle(entry);
                return null;
            }

            LuaTable module = LuaManager.Instance != null
                ? LuaManager.Instance.RequireModule(panel.Module)
                : null;

            if (module == null)
            {
                Debug.LogError($"[UI] 加载 Lua 模块 {panel.Module} 失败（模块名拼错了？包没打进去？）");
                Destroy(instance);
                ReleaseBundle(entry);
                return null;
            }

            panel.AttachModule(module);
            return panel;
        }

        private void AddToCache(string name, UIPanel panel)
        {
            cached[name] = panel;
            cacheOrder.Enqueue(name);

            // 用 while 不用 if：容量被调小过的话，一次可能淘汰好几个
            while (cacheOrder.Count > cacheCapacity)
            {
                string oldest = cacheOrder.Dequeue();

                if (!cached.TryGetValue(oldest, out UIPanel old))
                {
                    continue;   // 这个名字已经被主动移出缓存了
                }

                cached.Remove(oldest);
                Debug.Log($"[UI] 缓存超过 {cacheCapacity} 个，销毁最早缓存的 {oldest}");
                Destroy(old.gameObject);

                if (entries.TryGetValue(oldest, out PanelEntry e))
                {
                    ReleaseBundle(e);
                }
            }
        }

        private void ReleaseBundle(PanelEntry entry)
        {
            if (entry != null && Downloader.Instance != null)
            {
                // 传 false：UI 包不能连带销毁已加载的资源，
                // 否则该包里的图集、字体会一起没掉，别的面板会花屏。
                Downloader.Instance.UnloadAB(entry.bundle, false);
            }
        }
    }
}
