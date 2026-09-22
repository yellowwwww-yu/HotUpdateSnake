using UnityEngine;

/// <summary>
/// MonoBehaviour 单例基类。子类通过 Instance 访问，场景里出现第二个实例时只保留先到的那个。
/// </summary>
public class Singleton<T> : MonoBehaviour where T : MonoBehaviour
{
    private static T instance;

    public static T Instance => instance;

    protected virtual void Awake()
    {
        if (instance == null)
        {
            instance = this as T;
        }

        // 场景里挂了第二个：销毁它，保证 Instance 只有一个
        if (instance != this)
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// 自身销毁时清掉静态引用，避免后面拿到已经销毁的对象。
    /// 子类如果重写 OnDestroy，需要调用 base.OnDestroy()。
    /// </summary>
    protected virtual void OnDestroy()
    {
        if (ReferenceEquals(instance, this))
        {
            instance = null;
        }
    }
}
