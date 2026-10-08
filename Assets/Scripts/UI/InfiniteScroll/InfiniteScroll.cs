using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using XLua;

[LuaCallCSharp]
public class InfiniteScroll : MonoBehaviour, IDragHandler, IScrollHandler
{
    [SerializeField]private float spacing = 0;
    [SerializeField]private float rowHeight = 45;
    [SerializeField]private int visibleCount = 8;
    [SerializeField]private RectTransform content;
    [SerializeField]private Canvas canvas;
    [SerializeField]private RectTransform[] rows;
    [SerializeField]private float scrollDeltaScale = 20;

    private float offset = 0;
    private int firstIndex = 0;

    [SerializeField] private bool loop = true;

    public string[][] datas = new string[][]
    {
        new string[] { "1", "张三", "3000" },
        new string[] { "2", "李四", "4000" },
        new string[] { "3", "王五", "5000" }
    };

    private void Awake()
    {

        canvas = GetComponentInParent<Canvas>();

        if (content == null)
        {
            Debug.LogError($"{name} 没有指定 Content，列表不会显示");
            return;
        }

        rows = new RectTransform[content.childCount];
        for(int i = 0; i<content.childCount; i++)
        {
            RectTransform row = content.GetChild(i) as RectTransform;
            rows[i] = row;
        }

        ShowData();
    }

    /// <summary>
    /// 由 Lua 调用，把榜单一次性读成纯 C# 数组，之后滚动不再跨语言访问 Lua 表。
    /// 每行三列，顺序要和 ShowData 里取子节点的顺序一致：名次、名字、分数。
    /// </summary>
    public void SetDatas(LuaTable table)
    {
        if (table == null || table.Length == 0)
        {
            // 长度为 0 时 ShowData 里的取模会除零，所以空表直接忽略、保持上一次的内容
            return;
        }

        int rowCount = table.Length;
        datas = new string[rowCount][];

        for (int i = 0; i < rowCount; i++)
        {
            // Lua 的表是 1 基，跨到 C# 要自己 +1
            LuaTable row = table.Get<int, LuaTable>(i + 1);
            datas[i] = new string[3];

            if (row == null)
            {
                continue;
            }

            for (int j = 0; j < 3; j++)
            {
                // 取不到就补空串，ShowData 里就不用再判越界
                datas[i][j] = row.Get<int, string>(j + 1) ?? string.Empty;
            }

            row.Dispose();
        }

        // 换了数据要回到顶部，否则会停在上一次滚到的位置
        firstIndex = 0;
        offset = 0;

        if (content != null)
        {
            content.anchoredPosition = Vector2.zero;
        }

        ShowData();
    }

    public void OnDrag(PointerEventData eventData)
    {
        float distance = eventData.delta.y/canvas.scaleFactor;
        Move(distance);
    }

    public void OnScroll(PointerEventData eventData)
    {
        float distance = -eventData.scrollDelta.y * scrollDeltaScale;
        Move(distance);
    }

    private void Move(float distance)
    {
        if(distance == 0 || datas.Length == 0)
        {
            return;
        }

        float cell = rowHeight + spacing;

        // 滚过的整行也算进总距离里，行号只从总距离反推一次。
        // 不能"先加行号再夹 offset"：越界那一步行号已经加过了，
        // 只夹 offset 的话行号还会一直往前跑，表尾就会绕回开头接着显示。
        float total = firstIndex * cell + offset + distance;

        if(loop)
        {
            // 循环模式：总距离在数据总高里打转
            float contentHeight = datas.Length * cell;

            total %= contentHeight;

            if(total < 0)
            {
                total += contentHeight;
            }
        }
        else
        {
            // 关循环：能滚的距离 = 数据总高 - 可视区高，拖到两头就停
            float maxOffset = (datas.Length - visibleCount) * cell;

            if(maxOffset < 0)
            {
                maxOffset = 0;
            }

            if(total < 0)
            {
                total = 0;
            }
            else if(total > maxOffset)
            {
                total = maxOffset;
            }
        }

        // 总距离 = 整行数 * 行高 + 不足一行的偏移
        firstIndex = (int)(total / cell);
        offset = total - firstIndex * cell;

        content.anchoredPosition = new Vector2(0, offset);
        ShowData();
    }

    private void ShowData()
    {
        for(int i = 0; i<rows.Length; i++)
        {
            int dataIndex = firstIndex + i;
            string[] data = null;

            if(dataIndex < datas.Length)
            {
                data = datas[dataIndex];
            }
            else if(loop && datas.Length > 0)
            {
                // 循环模式行槽永远填满，取完了绕回表头接着取
                data = datas[dataIndex % datas.Length];
            }

            // 关循环时数据不够填满行槽，多出来的槽 data 是 null，留空不绕回第一条
            rows[i].GetChild(0).GetComponent<Text>().text = data == null ? string.Empty : data[0];
            rows[i].GetChild(1).GetComponent<Text>().text = data == null ? string.Empty : data[1];
            rows[i].GetChild(2).GetComponent<Text>().text = data == null ? string.Empty : data[2];
        }
    }
}
