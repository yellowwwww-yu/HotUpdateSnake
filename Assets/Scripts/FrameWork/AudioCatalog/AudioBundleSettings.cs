using System;
using UnityEngine;

[CreateAssetMenu(
    fileName = "AudioBundleSettings",
    menuName = "Game/Audio Bundle Settings")]
public class AudioBundleSettings : ScriptableObject
{
    [Serializable]
    public class Group
    {
        public string id;
        public string bundle;
        public string catalog;
    }

    // 这里只保存地址，不直接引用全部音频目录或 AudioClip。
    public Group[] groups = Array.Empty<Group>();
}
