using UnityEngine;

/// <summary>
/// 一个通用的 MonoBehaviour 单例基类。
/// 继承此类可以方便地创建自动初始化的单例。
/// </summary>
/// <typeparam name="T">单例的具体类型，必须是 MonoBehaviour 或其子类。</typeparam>
public class Singleton<T> : MonoBehaviour where T : MonoBehaviour
{
    // 存储单例实例的静态变量
    private static T _instance;

    // 提供一个全局访问点
    public static T Instance
    {
        get
        {
            // 如果实例为空，则自动创建
            if (_instance == null)
            {
                // 尝试在场景中查找是否已存在该单例
                _instance = FindObjectOfType<T>();

                // 如果场景中也没有，则创建一个新的 GameObject 并挂载脚本
                if (_instance == null)
                {
                    GameObject singletonObject = new GameObject();
                    singletonObject.name = typeof(T).Name + " (Singleton)";
                    _instance = singletonObject.AddComponent<T>();
                    Debug.Log($"[Singleton] 自动创建了单例: {singletonObject.name}");
                }
            }
            return _instance;
        }
    }

    // 在 Awake 生命周期中确保单例的唯一性
    protected virtual void Awake()
    {
        if (_instance == null)
        {
            // 如果还没有实例，就将当前对象设为实例
            _instance = this as T;
            // 如果希望单例在场景切换时不被销毁，可以取消下面这行的注释
            // DontDestroyOnLoad(gameObject);
            Debug.Log($"[Singleton] '{gameObject.name}' 被确认为单例。");
        }
        else if (_instance != this)
        {
            // 如果已存在另一个实例，则销毁当前这个重复的实例
            Debug.LogWarning($"[Singleton] 检测到重复实例，销毁: '{gameObject.name}'。");
            Destroy(gameObject);
        }
    }
}