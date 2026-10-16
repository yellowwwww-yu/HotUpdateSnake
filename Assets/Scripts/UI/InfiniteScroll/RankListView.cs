using System.Collections.Generic;
using UnityEngine;
using XLua;

[LuaCallCSharp]
[RequireComponent(typeof(InfiniteScroll))]
public class RankListView : MonoBehaviour
{
    private InfiniteScroll scroll;
    private string[][] datas = new string[0][];
    private readonly Dictionary<RectTransform, UnityEngine.UI.Text[]> labels =
        new Dictionary<RectTransform, UnityEngine.UI.Text[]>();

    // Lua 仍传三列字符串，但数据读取从滚动核心移到排行榜展示组件。
    public void SetDatas(LuaTable table)
    {
        int length = table == null ? 0 : table.Length;
        string[][] next = new string[length][];
        for (int i = 0; i < length; i++)
        {
            next[i] = new string[] { string.Empty, string.Empty, string.Empty };
            LuaTable row = table.Get<int, LuaTable>(i + 1);
            if (row == null) continue;
            try
            {
                for (int j = 0; j < 3; j++)
                    next[i][j] = row.Get<int, string>(j + 1) ?? string.Empty;
            }
            finally { row.Dispose(); }
        }

        datas = next;
        if (scroll == null) scroll = GetComponent<InfiniteScroll>();
        scroll.SetRows(datas.Length, ShowRow);
    }

    private void ShowRow(RectTransform row, int dataIndex)
    {
        if (!labels.TryGetValue(row, out UnityEngine.UI.Text[] texts))
        {
            if (row.childCount < 3)
                throw new System.InvalidOperationException("排行榜行需要三列文本");
            texts = new UnityEngine.UI.Text[3];
            for (int i = 0; i < 3; i++)
            {
                texts[i] = row.GetChild(i).GetComponent<UnityEngine.UI.Text>();
                if (texts[i] == null)
                    throw new System.InvalidOperationException("排行榜的前三个子节点需有 Text");
            }
            labels.Add(row, texts);
        }

        for (int i = 0; i < 3; i++)
            texts[i].text = dataIndex < 0 ? string.Empty : datas[dataIndex][i];
    }
}
