using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "SpriteCatalog",
    menuName = "Game/Sprite Catalog")]
public sealed class SpriteCatalog : ScriptableObject
{
    [Serializable]
    public sealed class Entry
    {
        public string key;
        public Sprite sprite;
    }

    [SerializeField] private List<Entry> entries = new List<Entry>();

    [NonSerialized] private Dictionary<string, Sprite> lookup;

    public bool TryGet(string key, out Sprite sprite)
    {
        sprite = null;
        if (string.IsNullOrWhiteSpace(key)) return false;

        if (lookup == null) BuildLookup();

        return lookup.TryGetValue(key, out sprite) && sprite != null;
    }

    private void BuildLookup()
    {
        // 配置有误时保留空索引，避免使用只构建了一半的目录。
        lookup = new Dictionary<string, Sprite>(StringComparer.Ordinal);
        var candidate = new Dictionary<string, Sprite>(StringComparer.Ordinal);

        for (int i = 0; i < entries.Count; i++)
        {
            Entry entry = entries[i];
            if (entry == null ||
                string.IsNullOrWhiteSpace(entry.key) ||
                entry.sprite == null)
            {
                Debug.LogError(
                    $"{name}：第 {i + 1} 条的 Key 或 Sprite 无效。", this);
                return;
            }

            if (candidate.ContainsKey(entry.key))
            {
                Debug.LogError(
                    $"{name}：资源键重复：{entry.key}", this);
                return;
            }

            candidate.Add(entry.key, entry.sprite);
        }

        lookup = candidate;
    }

    private void OnEnable()
    {
        lookup = null;
    }

    private void OnValidate()
    {
        lookup = null;
    }
}
