using System;
using UnityEngine;

[CreateAssetMenu(
    fileName = "SpriteBundleSettings",
    menuName = "Game/Sprite Bundle Settings")]
public class SpriteBundleSettings : ScriptableObject
{
    [Serializable]
    public class Group
    {
        public string id;
        public string bundle;
        public string catalog;
    }

    // 这里只保存地址，不直接引用全部精灵目录或图集。
    public Group[] groups = Array.Empty<Group>();
}
