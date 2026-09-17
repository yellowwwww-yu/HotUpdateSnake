using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Singleton<T> : MonoBehaviour where T : MonoBehaviour
{
    private static T instance;
    public static T Instance=>instance;

    protected virtual void Awake()
    {
        if(instance == null)
        {
            instance = this as T;
        }
        
        if(instance != this)
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// 自身销毁时清空静态引用，防止后续 Instance 拿到"已销毁但非空"的悬挂对象。
    /// 子类若重写 OnDestroy，必须 override 并调用 base.OnDestroy()。
    /// </summary>
    protected virtual void OnDestroy()
    {
        if (ReferenceEquals(instance, this))
        {
            instance = null;
        }
    }
}
