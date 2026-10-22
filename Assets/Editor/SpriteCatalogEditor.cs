// [UNITY-SKILL:SPRITEATLAS]
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.U2D;

[CustomEditor(typeof(SpriteCatalog))]
public sealed class SpriteCatalogEditor : Editor
{
    private UnityEngine.Object importSource;
    private string keyPrefix = "";

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("批量导入切片", EditorStyles.boldLabel);

        importSource = EditorGUILayout.ObjectField(
            "图片或 SpriteAtlas", importSource, typeof(UnityEngine.Object), false);
        keyPrefix = EditorGUILayout.TextField("Key 前缀（可选）", keyPrefix);

        EditorGUILayout.HelpBox(
            "拖入已切片图片或 SpriteAtlas，自动登记原始 Sprite 及名称。" +
            "已登记的 Sprite 保留原 Key；" +
            "新条目的 Key 为“前缀 + 切片名”。同名冲突时请设置不同前缀。",
            MessageType.Info);

        using (new EditorGUI.DisabledScope(
            importSource == null || Application.isPlaying))
        {
            if (GUILayout.Button("导入新增切片"))
                ImportSprites();
        }
    }

    private void ImportSprites()
    {
        var uniqueSprites = new HashSet<Sprite>();
        try
        {
            UnityEngine.Object source = ResolveSource(importSource);
            if (!(source is Texture2D) && !(source is Sprite) &&
                !(source is SpriteAtlas))
                throw new InvalidOperationException("请选择已切片图片或 SpriteAtlas。");

            CollectSprites(source, uniqueSprites, new HashSet<SpriteAtlas>());
        }
        catch (InvalidOperationException exception)
        {
            Debug.LogError(exception.Message, target);
            return;
        }

        var sourceSprites = new List<Sprite>(uniqueSprites);
        if (sourceSprites.Count == 0)
        {
            Debug.LogError(
                "没有读取到 Sprite，请检查图片切片或图集的打包对象。", target);
            return;
        }

        sourceSprites.Sort((a, b) => string.CompareOrdinal(a.name, b.name));

        serializedObject.Update();
        SerializedProperty entries = serializedObject.FindProperty("entries");
        var knownKeys = new HashSet<string>(StringComparer.Ordinal);
        var knownSprites = new HashSet<Sprite>();

        for (int i = 0; i < entries.arraySize; i++)
        {
            SerializedProperty entry = entries.GetArrayElementAtIndex(i);
            string key = entry.FindPropertyRelative("key").stringValue;
            Sprite sprite = entry.FindPropertyRelative("sprite")
                .objectReferenceValue as Sprite;

            if (string.IsNullOrWhiteSpace(key) || sprite == null ||
                !knownKeys.Add(key))
            {
                Debug.LogError(
                    $"请先修复目录第 {i + 1} 条的空 Key、缺失 Sprite 或重复 Key。",
                    target);
                return;
            }

            knownSprites.Add(sprite);
        }

        var additions = new List<Sprite>();
        string prefix = keyPrefix.Trim();

        // 先检查全部候选，确认没有冲突后才写入目录。
        foreach (Sprite sprite in sourceSprites)
        {
            if (knownSprites.Contains(sprite)) continue;

            string key = prefix + sprite.name;
            if (string.IsNullOrWhiteSpace(sprite.name) || !knownKeys.Add(key))
            {
                Debug.LogError(
                    $"无法导入，资源键冲突或切片名无效：{key}。" +
                    "与已有条目重名可换前缀；来源内部重名需按图片分别加前缀导入。" +
                    "本次未新增条目。",
                    target);
                return;
            }

            additions.Add(sprite);
        }

        if (additions.Count == 0)
        {
            Debug.Log("这些切片已经登记，无需新增。", target);
            return;
        }

        foreach (Sprite sprite in additions)
        {
            int index = entries.arraySize;
            entries.arraySize++;
            SerializedProperty entry = entries.GetArrayElementAtIndex(index);
            entry.FindPropertyRelative("key").stringValue = prefix + sprite.name;
            entry.FindPropertyRelative("sprite").objectReferenceValue = sprite;
        }

        serializedObject.ApplyModifiedProperties();
        AssetDatabase.SaveAssetIfDirty(target);

        Debug.Log(
            $"新增 {additions.Count} 条，跳过已登记的 " +
            $"{sourceSprites.Count - additions.Count} 个 Sprite。", target);
    }
    private static UnityEngine.Object ResolveSource(UnityEngine.Object source)
    {
        string path = AssetDatabase.GetAssetPath(source);
        if (path.EndsWith(".spriteatlas", StringComparison.OrdinalIgnoreCase) ||
            path.EndsWith(".spriteatlasv2", StringComparison.OrdinalIgnoreCase))
        {
            // 读取导入后的图集，仅查询其打包来源，不修改图集。
            SpriteAtlas atlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(path);
            if (atlas == null)
                throw new InvalidOperationException("无法读取图集，请检查导入状态。");
            return atlas;
        }

        return source;
    }

    private static void CollectSprites(
        UnityEngine.Object source,
        HashSet<Sprite> sprites,
        HashSet<SpriteAtlas> visitedAtlases)
    {
        if (source == null)
            throw new InvalidOperationException("导入来源中存在缺失引用，请先修复。");

        string path = AssetDatabase.GetAssetPath(source);
        if (path != "Assets" && !path.StartsWith("Assets/", StringComparison.Ordinal))
            throw new InvalidOperationException("导入来源必须位于项目 Assets 目录中。");

        if (source is Sprite sprite)
        {
            sprites.Add(sprite);
            return;
        }

        if (source is SpriteAtlas atlas)
        {
            if (!visitedAtlases.Add(atlas)) return;

            if (atlas.isVariant)
            {
                SpriteAtlas master = atlas.GetMasterAtlas();
                if (master == null)
                    throw new InvalidOperationException("变体图集没有有效的主图集。");
                CollectSprites(master, sprites, visitedAtlases);
                return;
            }

            foreach (UnityEngine.Object packable in atlas.GetPackables())
                CollectSprites(packable, sprites, visitedAtlases);
            return;
        }

        if (AssetDatabase.IsValidFolder(path))
        {
            foreach (string guid in AssetDatabase.FindAssets(
                "t:Texture2D", new[] { path }))
            {
                string texturePath = AssetDatabase.GUIDToAssetPath(guid);
                if (texturePath.EndsWith(".spriteatlas", StringComparison.OrdinalIgnoreCase) ||
                    texturePath.EndsWith(".spriteatlasv2", StringComparison.OrdinalIgnoreCase))
                    continue;

                Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
                if (texture != null)
                    CollectSprites(texture, sprites, visitedAtlases);
            }
            return;
        }

        if (source is Texture2D)
        {
            foreach (UnityEngine.Object asset in AssetDatabase.LoadAllAssetsAtPath(path))
            {
                if (asset is Sprite subSprite)
                    sprites.Add(subSprite);
            }
            return;
        }

        throw new InvalidOperationException(
            $"不支持的打包对象：{source.name}（{source.GetType().Name}）。");
    }

}
