using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "AudioCatalog",
    menuName = "Game/Audio Catalog")]
public sealed class AudioCatalog : ScriptableObject
{
    [Serializable]
    public sealed class Entry
    {
        public string key;
        public AudioClip clip;
    }

    [SerializeField] private List<Entry> entries = new List<Entry>();

    [NonSerialized] private Dictionary<string, AudioClip> lookup;

    public bool TryGet(string key, out AudioClip clip)
    {
        clip = null;
        if (string.IsNullOrWhiteSpace(key)) return false;

        if (lookup == null) BuildLookup();

        return lookup.TryGetValue(key, out clip) && clip != null;
    }

    private void BuildLookup()
    {
        // 配置有误时保留空索引，避免使用只构建了一半的目录。
        lookup = new Dictionary<string, AudioClip>(StringComparer.Ordinal);
        var candidate = new Dictionary<string, AudioClip>(StringComparer.Ordinal);

        if (entries == null)
        {
            Debug.LogError($"{name}：音频条目列表为空引用。", this);
            return;
        }

        for (int i = 0; i < entries.Count; i++)
        {
            Entry entry = entries[i];
            if (entry == null ||
                string.IsNullOrWhiteSpace(entry.key) ||
                entry.clip == null)
            {
                Debug.LogError(
                    $"{name}：第 {i + 1} 条的 Key 或 AudioClip 无效。", this);
                return;
            }

            if (candidate.ContainsKey(entry.key))
            {
                Debug.LogError(
                    $"{name}：资源键重复：{entry.key}", this);
                return;
            }

            candidate.Add(entry.key, entry.clip);
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
