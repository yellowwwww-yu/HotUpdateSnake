using System;
using UnityEngine;
using UnityEngine.EventSystems;

// 只负责竖向滚动、边界和行槽复用；行内容由 C# 回调绘制。
public class InfiniteScroll : MonoBehaviour, IDragHandler, IScrollHandler
{
    [SerializeField] private float spacing = 0;
    [SerializeField] private float rowHeight = 45;
    [SerializeField] private int visibleCount = 8;
    [SerializeField] private RectTransform content;
    [SerializeField] private Canvas canvas;
    [SerializeField] private RectTransform[] rows;
    [SerializeField] private float scrollDeltaScale = 20;
    [SerializeField] private bool loop = true;

    private int dataRowCount;
    private int firstIndex;
    private float offset;
    private bool initialized;
    private Action<RectTransform, int> renderRow;
    private Action beforeMove;

    private void Awake()
    {
        Initialize();
    }

    private void Initialize()
    {
        if (initialized) return;
        canvas = GetComponentInParent<Canvas>();
        if (content == null || canvas == null)
            throw new InvalidOperationException("InfiniteScroll 需要 Content 和父 Canvas");
        if (rowHeight <= 0 || spacing < 0 || visibleCount <= 0)
            throw new InvalidOperationException("行高/可视行数需大于0，间距不能为负");

        rows = new RectTransform[content.childCount];
        for (int i = 0; i < rows.Length; i++)
            rows[i] = (RectTransform)content.GetChild(i);
        initialized = true;
    }

    // rowCount 是数据行数。dataIndex 从0开始，-1代表空行槽。
    public void SetRows(int rowCount, Action<RectTransform, int> renderer,
        Action onBeforeMove = null, bool keepPosition = false)
    {
        if (rowCount < 0) throw new ArgumentOutOfRangeException(nameof(rowCount));
        if (renderer == null) throw new ArgumentNullException(nameof(renderer));
        Initialize();

        float cell = rowHeight + spacing;
        float position = keepPosition ? firstIndex * cell + offset : 0;
        renderRow = renderer;
        beforeMove = onBeforeMove;
        beforeMove?.Invoke();
        dataRowCount = rowCount;
        position = LimitPosition(position);
        firstIndex = (int)(position / cell);
        offset = position - firstIndex * cell;
        content.anchoredPosition = new Vector2(content.anchoredPosition.x, offset);
        RefreshRows();
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (dataRowCount == 0) return;
        Move(eventData.delta.y / canvas.scaleFactor);
    }

    public void OnScroll(PointerEventData eventData)
    {
        Move(-eventData.scrollDelta.y * scrollDeltaScale);
    }

    private void Move(float distance)
    {
        if (distance == 0 || dataRowCount == 0) return;
        float cell = rowHeight + spacing;
        float previous = firstIndex * cell + offset;
        float total = LimitPosition(previous + distance);

        if (total == previous) return;
        beforeMove?.Invoke();
        firstIndex = (int)(total / cell);
        offset = total - firstIndex * cell;
        content.anchoredPosition = new Vector2(content.anchoredPosition.x, offset);
        RefreshRows();
    }

    private float LimitPosition(float position)
    {
        float cell = rowHeight + spacing;
        if (loop && dataRowCount > 0)
        {
            float height = dataRowCount * cell;
            position %= height;
            if (position < 0) position += height;
            return position;
        }

        float maxOffset = Mathf.Max(0, (dataRowCount - visibleCount) * cell);
        return Mathf.Clamp(position, 0, maxOffset);
    }

    private void RefreshRows()
    {
        for (int i = 0; i < rows.Length; i++)
        {
            int index = firstIndex + i;
            if (loop && dataRowCount > 0) index %= dataRowCount;
            renderRow(rows[i], index < dataRowCount ? index : -1);
        }
    }
}
