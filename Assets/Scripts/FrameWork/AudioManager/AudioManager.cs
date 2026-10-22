using System.Collections.Generic;
using UnityEngine;
using XLua;

namespace Framework.Audio
{
    /// <summary>
    /// 常驻的 2D 音频管理：一个音乐播放器，加一个内部短音效对象池。
    /// 挂在 PersistentScene 的独立物体上；音频包由外部加载并保持存活。
    /// </summary>
    [LuaCallCSharp]
    public class AudioManager : Singleton<AudioManager>
    {
        [Header("最多同时播放多少个短音效，满额时忽略新请求")]
        [SerializeField, Min(1)] private int maxSounds = 16;

        [Header("初始音量；运行时通过 Set 方法修改，保存由设置系统负责")]
        [SerializeField, Range(0f, 1f)] private float musicVolume = 1f;
        [SerializeField, Range(0f, 1f)] private float soundVolume = 1f;

        private readonly Stack<AudioSource> idle = new Stack<AudioSource>(16);
        private readonly List<AudioSource> playing = new List<AudioSource>(16);

        private Transform audioRoot;
        private AudioSource music;
        private int createdCount;
        private int reusedCount;

        public float MusicVolume => musicVolume;
        public float SoundVolume => soundVolume;
        public int PlayingCount => playing.Count;
        public int IdleCount => idle.Count;
        public int CreatedCount => createdCount;
        public int ReusedCount => reusedCount;

        protected override void Awake()
        {
            base.Awake();
            if (Instance != this) return;

            maxSounds = Mathf.Max(1, maxSounds);
            musicVolume = NormalizeVolume(musicVolume);
            soundVolume = NormalizeVolume(soundVolume);

            audioRoot = new GameObject("AudioPlayers").transform;
            audioRoot.SetParent(transform, false);
            music = CreateSource("MusicPlayer");
            music.loop = true;
            music.volume = musicVolume;
        }

        private void Start()
        {
            if (Instance == this && LuaManager.Instance != null)
            {
                LuaManager.Instance.Inject("Audio", this);
            }
        }

        /// <summary>
        /// 从已加载的 AB 借用音频，不加载包、不增加包引用。
        /// 调用方可保存返回值，点击时直接 PlaySound，避免每次触发都读包。
        /// </summary>
        public AudioClip GetClip(string bundleName, string assetName)
        {
            if (string.IsNullOrEmpty(bundleName) || string.IsNullOrEmpty(assetName))
            {
                Debug.LogWarning("[Audio] 音频包名和资源名不能为空", this);
                return null;
            }

            if (Downloader.Instance == null)
            {
                Debug.LogWarning("[Audio] Downloader 不存在，无法借用音频", this);
                return null;
            }

            AudioClip clip = Downloader.Instance.GetAsset<AudioClip>(bundleName, assetName);
            if (clip == null)
            {
                Debug.LogWarning($"[Audio] 未找到音频 {bundleName}/{assetName}", this);
            }
            return clip;
        }

        /// <summary>循环播放音乐。同一首已指定的音乐不重启，暂停期间也不重启。</summary>
        public bool PlayMusic(AudioClip clip)
        {
            if (!CanPlay(clip)) return false;
            if (music.clip == clip) return true;

            StopMusic();
            music.clip = clip;
            music.volume = musicVolume;
            music.Play();
            return true;
        }

        public void StopMusic()
        {
            if (music == null) return;
            music.Stop();
            music.clip = null;
        }

        /// <summary>播放一个短音效，结束后自动回池；播放器不暴露给调用方。</summary>
        public bool PlaySound(AudioClip clip)
        {
            if (!CanPlay(clip)) return false;
            RecycleFinishedSounds();
            if (playing.Count >= maxSounds) return false;

            AudioSource source = null;
            while (idle.Count > 0 && source == null)
            {
                source = idle.Pop();
            }

            if (source == null)
            {
                source = CreateSource("SoundPlayer");
                createdCount++;
            }
            else
            {
                reusedCount++;
            }

            source.gameObject.SetActive(true);
            source.clip = clip;
            source.loop = false;
            source.pitch = 1f;
            source.volume = soundVolume;
            source.Play();
            playing.Add(source);
            return true;
        }

        public void StopAllSounds()
        {
            for (int i = playing.Count - 1; i >= 0; i--)
            {
                RecycleAt(i);
            }
        }

        public void SetMusicVolume(float value)
        {
            musicVolume = NormalizeVolume(value);
            if (music != null) music.volume = musicVolume;
        }

        public void SetSoundVolume(float value)
        {
            soundVolume = NormalizeVolume(value);
            for (int i = 0; i < playing.Count; i++)
            {
                if (playing[i] != null) playing[i].volume = soundVolume;
            }
        }

        private void Update()
        {
            RecycleFinishedSounds();
        }

        private void RecycleFinishedSounds()
        {
            // Listener 暂停时，不能把暂时不播放的音效误判为结束。
            if (AudioListener.pause) return;

            for (int i = playing.Count - 1; i >= 0; i--)
            {
                AudioSource source = playing[i];
                if (source == null || !source.isPlaying) RecycleAt(i);
            }
        }

        private void RecycleAt(int index)
        {
            AudioSource source = playing[index];
            playing.RemoveAt(index);
            if (source == null) return;

            source.Stop();
            source.clip = null;
            source.volume = 1f;
            source.gameObject.SetActive(false);
            idle.Push(source);
        }

        private AudioSource CreateSource(string objectName)
        {
            GameObject player = new GameObject(objectName);
            player.SetActive(false);
            player.transform.SetParent(audioRoot, false);

            AudioSource source = player.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            source.dopplerLevel = 0f;
            source.pitch = 1f;
            source.loop = false;
            source.ignoreListenerPause = false;
            player.SetActive(true);
            return source;
        }

        private bool CanPlay(AudioClip clip)
        {
            if (Instance != this || !isActiveAndEnabled || music == null || audioRoot == null)
            {
                Debug.LogWarning("[Audio] 音频管理尚未就绪或已禁用", this);
                return false;
            }

            if (clip == null || clip.loadState != AudioDataLoadState.Loaded)
            {
                Debug.LogWarning("[Audio] 音频为空或数据尚未加载完成，本次不播放", this);
                return false;
            }
            return true;
        }

        private static float NormalizeVolume(float value)
        {
            if (float.IsNaN(value) || float.IsInfinity(value)) return 0f;
            return Mathf.Clamp01(value);
        }

        private void OnDisable()
        {
            // 禁用管理组件时子物体仍可能活着，因此主动停止全部播放。
            StopMusic();
            StopAllSounds();
        }

        protected override void OnDestroy()
        {
            // 只移除组件时，也要清理本组件创建的子物体。
            if (audioRoot != null) Destroy(audioRoot.gameObject);
            playing.Clear();
            idle.Clear();
            base.OnDestroy();
        }
    }
}
