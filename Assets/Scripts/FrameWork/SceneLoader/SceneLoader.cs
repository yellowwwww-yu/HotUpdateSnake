using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using XLua;

namespace Framework
{
    /// <summary>
    /// 场景加载与卸载。加载走异步 + 进度显示，进度条复用热更下载那条 LoadingProgressBar。
    /// </summary>
    [LuaCallCSharp]
    public class SceneLoader : Singleton<SceneLoader>
    {
        [Header("进度 UI，和 Downloader 拖的是同一个对象")]
        [SerializeField] private GameObject progressRoot;
        [SerializeField] private Slider slider;
        [SerializeField] private Text text;

        private bool loading;

        private void Start()
        {
            if (LuaManager.Instance != null)
            {
                LuaManager.Instance.Inject("SceneLoader", this);
            }
        }

        // ==================== 给 Lua 用的接口 ====================

        /// <summary>Additive 加载：新场景叠加进来，当前场景（PersistentScene）保留。</summary>
        public void Load(string sceneName)
        {
            if (loading)
            {
                return;   // 连点两下会加载两份
            }

            loading = true;
            StartCoroutine(LoadRoutine(sceneName));
        }

        /// <summary>
        /// 卸载一个 Additive 加载进来的场景（PersistentScene 这种启动场景卸不掉）。
        /// 场景里的 SceneBundleLoader 会在自身销毁时清 Lua 模块缓存再卸包，这里不用管。
        /// </summary>
        public void Unload(string sceneName)
        {
            if (loading)
            {
                return;
            }

            if (!SceneManager.GetSceneByName(sceneName).isLoaded)
            {
                // 不先判的话，UnloadSceneAsync 会打一条 Unity 自己的红字，看不出是谁调用的
                Debug.LogError($"[场景] {sceneName} 没有加载，无法卸载");
                return;
            }

            loading = true;
            StartCoroutine(UnloadRoutine(sceneName));
        }

        // 卸载不驱动进度条：整个过程通常只有一两帧，进度条底是不透明的，闪一下反而更难看。
        // 想遮住过渡的话，调 Unload 之前先把该显示的面板 Open 出来。
        private IEnumerator UnloadRoutine(string sceneName)
        {
            AsyncOperation op = SceneManager.UnloadSceneAsync(sceneName);

            while (op != null && !op.isDone)
            {
                yield return null;
            }

            loading = false;
        }

        private IEnumerator LoadRoutine(string sceneName)
        {
            Show(0f, sceneName + " 加载中 0%");

            AsyncOperation op = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);

            if (op == null)
            {
                // 场景不在 Build Settings 里，或者名字拼错。错误留在屏幕上，不藏起来
                Show(0f, "场景 " + sceneName + " 加载失败，检查 Build Settings");
                Debug.LogError($"[场景] {sceneName} 加载失败：不在 Build Settings 里，或者名字拼错");
                loading = false;
                yield break;
            }

            while (!op.isDone)
            {
                // progress 爬到 0.9 就停住，剩下那 0.1 是场景激活阶段。
                // 不除以 0.9 的话进度条永远停在 90%
                float p = Mathf.Clamp01(op.progress / 0.9f);
                Show(p, sceneName + " 加载中 " + Mathf.RoundToInt(p * 100) + "%");
                yield return null;
            }

            Show(1f, sceneName + " 加载中 100%");
            Hide();
            loading = false;
        }

        private void Show(float progress, string status)
        {
            if (progressRoot != null) { progressRoot.SetActive(true); }
            if (slider != null) { slider.value = progress; }
            if (text != null) { text.text = status; }
        }

        private void Hide()
        {
            if (progressRoot != null) { progressRoot.SetActive(false); }
        }
    }
}
