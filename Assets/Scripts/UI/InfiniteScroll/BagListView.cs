using System;
using System.Collections.Generic;
using UnityEngine;
using XLua;

[CSharpCallLua]
public delegate void BagBeforeMoveCallBack();

[LuaCallCSharp]
[RequireComponent(typeof(InfiniteScroll))]
public class BagListView : MonoBehaviour
{
    private const string ItemSpriteGroup = "items";

    private InfiniteScroll scrool;
    private List<BagItem> datas = new List<BagItem>();
    private int column = 6;
    private int Length = 0;

    private GameObject CellPrefab;
    private Stack<GameObject> pool = new Stack<GameObject>();
    private Dictionary<int, BagCell> cells = new Dictionary<int, BagCell>();
    private BagBeforeMoveCallBack beforeMove;

    [SerializeField]private RectTransform PoolContainer;

    [Serializable]
    public class BagItem
    {
        public int id;
        public string name;
        public int count;
        public string desc;
        public string iconKey;
    }

    public void BindBeforeMove(BagBeforeMoveCallBack callback)
    {
        beforeMove = callback;
    }

    public void UnBindBeforeMove()
    {
        beforeMove = null;
    }

    public void SetDatas(LuaTable table, bool keepPosition = false)
    {
        datas.Clear();
        Length = table != null ? table.Length : 0;
        for (int i = 0; i < Length; i++)
        {
            LuaTable row = table.Get<int, LuaTable>(i + 1);
            try
            {
                BagItem item = new BagItem
                {
                    id = row.Get<string, int>("id"),
                    name = row.Get<string, string>("name"),
                    count = row.Get<string, int>("count"),
                    desc = row.Get<string, string>("desc"),
                    iconKey = row.Get<string, string>("iconKey")
                };
                datas.Add(item);
            }
            finally
            {
                row?.Dispose();
            }
        }

        if (scrool == null)
        {
            scrool = GetComponent<InfiniteScroll>();
        }
        int rowCount = Mathf.CeilToInt((float)Length / column);
        scrool.SetRows(rowCount, ShowRow, OnBeforeMove, keepPosition);
    }

    // 只改数量，不重建格子，也不改变滚动位置。
    public void UpdateCount(int id, int count)
    {
        foreach (BagItem item in datas)
        {
            if (item.id == id)
            {
                item.count = count;
                break;
            }
        }

        if (cells.TryGetValue(id, out BagCell cell) && cell != null)
        {
            cell.SetTxt_Num(count);
        }
    }

    private void OnBeforeMove()
    {
        cells.Clear();
        beforeMove?.Invoke();
    }

    private void ShowRow(RectTransform row, int index)
    {
        for (int i = row.childCount - 1; i >= 0; i--)
        {
            ReleaseCell(row.GetChild(i).gameObject);
        }
        if (index < 0) return;

        int startIndex = index * column;
        int remain = Length - startIndex;
        if (remain <= 0) return;

        int cellCount = Mathf.Min(column, remain);
        for (int i = 0; i < cellCount; i++)
        {
            GameObject newCell = GetCell(row);
            BagCell bagCell = newCell.GetComponent<BagCell>();
            BagItem info = datas[startIndex + i];
            InitCell(bagCell, info);
            cells[info.id] = bagCell;
        }
    }

    private void InitCell(BagCell bagCell, BagItem info)
    {
        bagCell.SetId(info.id);
        bagCell.SetTxt_Num(info.count);
        bagCell.SetName(info.name);
        bagCell.SetDesc(info.desc);

        Sprite icon = null;
        SpriteBundleLoader loader = SpriteBundleLoader.Instance;
        if (loader != null && !string.IsNullOrWhiteSpace(info.iconKey))
            icon = loader.GetSprite(ItemSpriteGroup, info.iconKey);

        // 复用格子时始终覆盖图片，读取失败也不能保留上一物品的图标。
        bagCell.SetImg_Item(icon);
    }

    private GameObject GetCell(RectTransform row)
    {
        GameObject go;
        if (pool.Count > 0)
        {
            go = pool.Pop();
            go.transform.SetParent(row, false);
            go.SetActive(true);
        }
        else
        {
            if (CellPrefab == null)
            {
                CellPrefab = Downloader.Instance.GetAsset<GameObject>("ui_prefab", "BagCell");
            }
            go = Instantiate(CellPrefab, row, false);
        }
        return go;
    }

    private void ReleaseCell(GameObject go)
    {
        if (go == null) return;
        go.SetActive(false);
        go.transform.SetParent(PoolContainer, false);
        pool.Push(go);
    }

    private void OnDisable()
    {
        UnBindBeforeMove();
        cells.Clear();
    }
}
