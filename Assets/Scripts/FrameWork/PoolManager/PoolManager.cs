using System.Collections.Generic;
using UnityEngine;
using XLua;

namespace Framework.Pool
{
    /// <summary>
    /// 通用 GameObject 对象池，用完不销毁，收起来等下次复用。
    /// 目前只有 GameObject 一种池化对象，所以不抽泛型也不分层。
    ///
    /// 注意：只池化常驻包里的资源。要池化的 prefab 记得加进 CommonBundleLoader，
    /// 这样就不会出现"池里的对象比 AB 包活得久"的问题。
    /// </summary>
    [LuaCallCSharp]
    public class PoolManager : Singleton<PoolManager>
    {
        [Header("每个 prefab 最多攒多少个空闲实例，超出的在归还时直接销毁")]
        [SerializeField] private int maxIdlePerPrefab = 32;

        [Header("空闲实例的容器（建议指向 UIRoot 下一个隐藏节点）")]
        [SerializeField] private Transform idleRoot;

        // prefab 的 InstanceID -> 空闲实例栈。
        // 用 InstanceID 而不是名字做 key，不同目录下的同名 prefab 会撞车。
        private readonly Dictionary<int, Stack<GameObject>> idle =
            new Dictionary<int, Stack<GameObject>>();

        // 借出去的实例 -> 它来自哪个 prefab，Recycle 时靠它反查归属
        private readonly Dictionary<GameObject, int> borrowed =
            new Dictionary<GameObject, int>();

        // RecycleAll 用的临时表，避免每次都分配
        private readonly List<GameObject> temp = new List<GameObject>(32);

        private int createdCount;
        private int reuseCount;

        /// <summary>累计新建了几个实例（只涨不跌，用来看池有没有真的在复用）</summary>
        public int CreatedCount => createdCount;

        /// <summary>累计复用了几次</summary>
        public int ReusedCount => reuseCount;

        private Transform IdleRoot => idleRoot != null ? idleRoot : transform;

        // ==================== 给 Lua 调用的接口 ====================

        /// <summary>借一个实例，有空闲的就复用，没有才新建</summary>
        public GameObject Get(GameObject prefab, Transform parent = null)
        {
            if (prefab == null)
            {
                Debug.LogError("[Pool] prefab 为空（AB 已卸载？引用变 MissingReference？）");
                return null;
            }

            int key = prefab.GetInstanceID();
            if (!idle.TryGetValue(key, out Stack<GameObject> stack))
            {
                stack = new Stack<GameObject>();
                idle[key] = stack;
            }

            GameObject go = null;

            // 栈里可能躺着"伪 null"：对象被外部 Destroy 过（Lua 直接 Destroy，
            // 或者场景卸载带走了）。这时 C# 引用不为 null，但 go == null 是 true。
            // 必须在这里剔掉，否则它会被当成正常实例返回，
            // 等到之后访问它的属性才抛 MissingReferenceException。
            while (stack.Count > 0)
            {
                GameObject candidate = stack.Pop();
                if (candidate != null)
                {
                    go = candidate;
                    break;
                }
            }

            if (go == null)
            {
                go = Instantiate(prefab, parent != null ? parent : IdleRoot, false);
                createdCount++;
            }
            else
            {
                reuseCount++;

                if (parent != null)
                {
                    go.transform.SetParent(parent, false);
                }
            }

            go.SetActive(true);
            borrowed[go] = key;
            return go;
        }

        /// <summary>归还一个实例。Lua 侧只传对象，不用说明它属于哪个桶。</summary>
        public void Recycle(GameObject go)
        {
            // 已经是伪 null，没什么可还的，也不算错误
            if (go == null)
            {
                return;
            }

            if (!borrowed.TryGetValue(go, out int key))
            {
                // 不是本池借出的，或者已经还过一次了。
                // 这里不销毁它，销毁会把别人的东西弄丢，只报警让调用方自己发现问题。
                Debug.LogWarning($"[Pool] {go.name} 不在借出记录里（重复归还？），本次忽略");
                return;
            }

            borrowed.Remove(go);
            go.SetActive(false);

            // key 一定在 idle 里：桶在 Get 时建，Clear 只清空栈不删桶
            Stack<GameObject> stack = idle[key];

            if (stack.Count >= maxIdlePerPrefab)
            {
                Destroy(go);   // 攒够了就销毁，不让空闲实例无限涨
                return;
            }

            go.transform.SetParent(IdleRoot, false);
            stack.Push(go);
        }

        /// <summary>把某个 prefab 借出去还没还的都收回来（列表清空时用）</summary>
        public int RecycleAll(GameObject prefab)
        {
            if (prefab == null)
            {
                return 0;
            }

            int key = prefab.GetInstanceID();

            // 先快照再还，Recycle 会改 borrowed，不能边遍历边改
            temp.Clear();
            foreach (KeyValuePair<GameObject, int> kv in borrowed)
            {
                if (kv.Value == key)
                {
                    temp.Add(kv.Key);
                }
            }

            int count = temp.Count;
            for (int i = 0; i < count; i++)
            {
                Recycle(temp[i]);
            }

            return count;
        }

        /// <summary>销毁某个 prefab 的全部空闲实例，它的 AB 包要卸载前调用</summary>
        public void Clear(GameObject prefab)
        {
            if (prefab == null)
            {
                return;
            }

            if (!idle.TryGetValue(prefab.GetInstanceID(), out Stack<GameObject> stack))
            {
                return;
            }

            while (stack.Count > 0)
            {
                GameObject go = stack.Pop();
                if (go != null)
                {
                    Destroy(go);
                }
            }

            // 只清空栈不删桶，借出中的那些还回来时还能找到自己的桶
        }

        /// <summary>打印统计。列表滚动时的"新建次数"就是复用有没有生效的证据。</summary>
        public void DumpStats()
        {
            int idleTotal = 0;
            foreach (Stack<GameObject> stack in idle.Values)
            {
                idleTotal += stack.Count;
            }

            Debug.Log($"[Pool] 新建 {createdCount} 个，复用 {reuseCount} 次，空闲 {idleTotal} 个，桶 {idle.Count} 个");
        }

        private void Start()
        {
            // 注入成 Lua 全局变量 Pool。
            // 时序上没问题：LuaManager 在 Awake 里建 luaEnv，所有 Awake 都早于所有 Start。
            if (LuaManager.Instance != null)
            {
                LuaManager.Instance.Inject("Pool", this);
            }
        }
    }
}
