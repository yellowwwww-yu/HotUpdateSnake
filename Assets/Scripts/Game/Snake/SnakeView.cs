using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using XLua;

/// <summary>
/// 一条蛇的表现层：把 Lua 算好的「头位置 + 朝向 + 间距 + 节点数」变成实际的一串节点。
///
/// 分工：
///   Lua 管规则与状态 —— 转向、前进、长度，每帧把结果推进 SetState
///   C#  管轨迹与渲染 —— 按累积路程记轨迹、逐节点跟随，纯搬运，不认识任何游戏规则
///
/// 节点跟随不放 Lua：节点数 × 每帧跨语言调用是热点，而且这是纯数学，留在 C# 更快更稳。
/// </summary>
public class SnakeView : MonoBehaviour
{
    private float totalDis = 0;
    private Vector2 headPos = Vector2.zero;
    private float headAngle = 0;              // 弧度，0 = +X，与 Lua 侧一致

    private Vector2 lastPos = Vector2.zero;
    private int trailStart = 0;
    private float needDis = 0;
    private int nodeCount = 1;                // 节点总数（含蛇头），只由 Lua 传
    private int activeNodeCount = 1;
    private float spacing = 0.22f;

    private bool hasState;                    // 收到第一帧状态前不记轨迹
    private bool warnedMissingPrefab;

    private GameObject headPrefab;
    private GameObject bodyPrefab;
    private Transform headTransform;

    private class TrailPoint
    {
        public float dis;
        public Vector2 pos;
    }

    List<TrailPoint> trails = new List<TrailPoint>(256);
    List<Transform> nodes = new List<Transform>();   // 只装身体，蛇头是单独一个物体

    private LuaFunction luaStart;
    private LuaFunction luaUpdate;
    private LuaFunction luaInput;

    private bool luaStarted;

    [SerializeField] private SceneBundleLoader sceneBundleLoader;

    // ==================== 给 Lua 调用的接口 ====================

    /// <summary>
    /// 由 Lua 每帧调用（也是唯一的入口）。
    /// 参数顺序 (x, y, spacing, nodeCount, angle) 必须和 Lua 侧一致。
    /// 这里只缓存，真正落地在 LateUpdate。
    /// </summary>
    public void SetState(float x, float y, float spacing, int nodeCount, float angle)
    {
        headPos = new Vector2(x, y);
        headAngle = angle;

        this.spacing = Mathf.Max(0.01f, spacing);        // spacing 为 0 / 负数会把里程算崩
        this.nodeCount = Mathf.Max(1, nodeCount);
        activeNodeCount = this.nodeCount;

        needDis = (this.nodeCount + 1) * this.spacing + this.spacing * 2;

        if (!hasState)
        {
            // 第一帧：轨迹从当前位置起算，否则会从世界原点拉出一条假尾巴
            hasState = true;
            totalDis = 0;
            lastPos = headPos;
            trails.Clear();
            trailStart = 0;
            trails.Add(new TrailPoint { dis = 0, pos = headPos });
        }

        // nodes 只装身体，所以数量和节点总数差 1
        if (activeNodeCount - 1 != nodes.Count)
        {
            CreateNodes(activeNodeCount - 1);
        }
    }

    // ==================== 启动 ====================

    private void Awake()
    {
        // 只在没拖引用时才去找，别无条件覆盖 Inspector 上的配置
        if (sceneBundleLoader == null)
        {
            GameObject go = GameObject.Find("SceneBundleLoader");
            if (go != null)
            {
                sceneBundleLoader = go.GetComponent<SceneBundleLoader>();
            }
        }

        if (sceneBundleLoader == null)
        {
            Debug.LogError("SnakeView 找不到 SceneBundleLoader：Inspector 未拖引用，场景里也没有同名物体");
        }
    }

    private void Start()
    {
        if (sceneBundleLoader == null)
        {
            return;
        }

        sceneBundleLoader.onReady += LuaStart;

        // SceneBundleLoader.Start 在依赖已就绪时会跳过 yield、同帧同步 Invoke。
        // Unity 不保证两个物体的 Start 顺序，排在后面就永远收不到回调 —— 自己补一次。
        if (sceneBundleLoader.IsReady)
        {
            LuaStart();
        }
    }

    private void LuaStart()
    {
        if (luaStarted)
        {
            return;
        }
        luaStarted = true;

        if (LuaManager.Instance == null)
        {
            Debug.LogError("LuaManager 不存在，SnakeView 无法启动");
            return;
        }

        LuaManager.Instance.Inject("SnakeView", this);

        // forceReload：require 有 package.loaded 缓存，不清的话第二次进场景 chunk 不会重新执行
        LuaManager.Instance.DoFile("Game.SnakeDemo", true);

        // 必须先于 Call()：Lua 建蛇时会调 CreateNodes，那时 headPrefab / bodyPrefab 得已经拿到
        Init();

        luaStart = LuaManager.Instance.GetLuaFunction("Snake_Start");
        luaUpdate = LuaManager.Instance.GetLuaFunction("Snake_Update");
        luaInput = LuaManager.Instance.GetLuaFunction("Snake_Input");

        if (luaStart == null)
        {
            Debug.LogError("SnakeDemo.lua 里没找到 Snake_Start");
            return;
        }

        luaStart.Call();
    }

    private void Init()
    {
        headPrefab = Downloader.Instance.GetAsset<GameObject>("prefab", "headPrefab");
        bodyPrefab = Downloader.Instance.GetAsset<GameObject>("prefab", "bodyPrefab");

        if (headPrefab == null || bodyPrefab == null)
        {
            Debug.LogError("prefab 包里的 headPrefab / bodyPrefab 取不到：确认本场景的 SceneBundleLoader 已加载 prefab 包");
            return;
        }

        GameObject head = GameObject.Instantiate(headPrefab, transform, false);
        head.name = "Head";
        headTransform = head.transform;
        headTransform.position = headPos;
    }

    private void Update()
    {
        if (luaUpdate == null)
        {
            return;
        }

        FeedInput();
        luaUpdate.Call(Time.deltaTime);
    }

    /// <summary>演示用输入：鼠标指向就是目标朝向。接真玩法时换成摇杆或网络帧。</summary>
    private void FeedInput()
    {
        if (luaInput == null || Camera.main == null)
        {
            return;
        }

        Vector3 world = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector2 dir = new Vector2(world.x, world.y) - headPos;

        if (dir.sqrMagnitude < 0.0001f)
        {
            return;
        }

        luaInput.Call(Mathf.Atan2(dir.y, dir.x));   // 弧度，和 Lua 侧一致
    }

    // ==================== 表现层内部 ====================

    private void LateUpdate()
    {
        if (!hasState)
        {
            return;
        }

        // ① 按实际走过的距离推进轨迹，与帧率、速度无关
        float moved = Vector2.Distance(lastPos, headPos);
        if (moved > 0)
        {
            totalDis += moved;
            lastPos = headPos;
            trails.Add(new TrailPoint { dis = totalDis, pos = headPos });
            TrimTrail();
        }

        // ② 蛇头不在 nodes 里，得单独摆
        if (headTransform != null)
        {
            headTransform.position = headPos;
            headTransform.rotation = Quaternion.Euler(0, 0, headAngle * Mathf.Rad2Deg);
        }

        // ③ 摆身体：必须在头的位置算完之后，否则身体恒慢一帧
        SetNodesPos();
    }

    /// <summary>
    /// 裁剪轨迹，只保留"节点数 + 1 倍间距再加一点余量"这么长的一段，更早的点不再使用。
    /// 推进起点只是一次整数加法，攒够 256 个才真正回收内存，避免每帧搬数组。
    /// </summary>
    private void TrimTrail()
    {
        while (trailStart < trails.Count - 3 && trails[trailStart + 1].dis < totalDis - needDis)
        {
            trailStart++;
        }

        if (trailStart > 256)
        {
            trails.RemoveRange(0, trailStart);
            trailStart = 0;
        }
    }

    private void SetNodesPos()
    {
        if (trails.Count < 2 || nodes.Count == 0)
        {
            return;
        }

        int bodyCount = Mathf.Min(activeNodeCount - 1, nodes.Count);

        // 节点按顺序依次往后，目标里程单调递减，所以游标只在循环外初始化一次，
        // 整体是 O(轨迹点数 + 节点数)
        int p = trails.Count - 1;

        for (int i = 0; i < bodyCount; i++)
        {
            // 第 i 个身体节点（蛇头是 0 号）站在离头 (i+1) 个间距的里程点上
            float target = totalDis - (i + 1) * spacing;

            // 头还没走出这么远（开局阶段）：压在轨迹最早的保留点上，蛇是"从起点长出来"的
            if (target <= trails[trailStart].dis)
            {
                nodes[i].position = trails[trailStart].pos;
                continue;
            }

            while (p > trailStart && trails[p].dis > target)
            {
                p--;
            }

            // 兜底：保证 p+1 合法（spacing 异常时可能一次都没回退）
            if (p > trails.Count - 2)
            {
                p = trails.Count - 2;
            }

            float disDiff = trails[p + 1].dis - trails[p].dis;
            if (disDiff < 0.0001f)
            {
                // 两点几乎重合，直接取前者，防除零 / NaN
                nodes[i].position = trails[p].pos;
            }
            else
            {
                float t = Mathf.Clamp01((target - trails[p].dis) / disDiff);
                nodes[i].position = Vector2.Lerp(trails[p].pos, trails[p + 1].pos, t);
            }
        }
    }

    /// <summary>bodyCount 是身体节点数（不含蛇头）。</summary>
    private void CreateNodes(int bodyCount)
    {
        bodyCount = Mathf.Max(0, bodyCount);

        if (bodyCount > nodes.Count)
        {
            if (bodyPrefab == null)
            {
                Debug.LogError("bodyPrefab 为空，无法创建蛇身节点（Init 未执行或 prefab 包未加载）");
                return;
            }

            int needCount = bodyCount - nodes.Count;
            for (int i = 0; i < needCount; i++)
            {
                GameObject body = GameObject.Instantiate(bodyPrefab, transform, false);

                // 列表为空时没有"上一节"可参考，就用蛇头当前位置起步
                body.transform.position = nodes.Count > 0 ? nodes[nodes.Count - 1].position : headPos;

                SpriteRenderer sprite = body.GetComponentInChildren<SpriteRenderer>();
                if (sprite != null)
                {
                    // 用真实下标，越靠后的身体越底层；这样蛇头穿过身体时不会忽隐忽现
                    sprite.sortingOrder = -(nodes.Count + 1);
                }

                nodes.Add(body.transform);
            }
        }

        for (int i = 0; i < nodes.Count; i++)
        {
            bool active = i < bodyCount;
            if (nodes[i].gameObject.activeSelf != active)
            {
                nodes[i].gameObject.SetActive(active);
            }
        }
    }

    private void OnDestroy()
    {
        if (sceneBundleLoader != null)
        {
            sceneBundleLoader.onReady -= LuaStart;
        }

        // LuaManager 可能已经先一步销毁（退出播放 / 切场景 / 关应用时，Unity 不保证
        // MonoBehaviour 之间的 OnDestroy 顺序）。它销毁时 luaEnv 已被 Dispose、rawL 归零，
        // 这时再碰 LuaFunction 就会抛 InvalidOperationException: this lua env had disposed!。
        // env 销毁时已把它持有的全部 Lua 引用一并释放，所以这里跳过是安全的，不是泄漏。
        if (LuaManager.Instance != null)
        {
            luaStart?.Dispose();
            luaUpdate?.Dispose();
            luaInput?.Dispose();

            // 清掉 Lua 模块缓存，下次进本场景 DoFile 才会重新执行 chunk
            LuaManager.Instance.ClearModule("Game.SnakeDemo");
        }

        luaStart = luaUpdate = luaInput = null;
    }
}
