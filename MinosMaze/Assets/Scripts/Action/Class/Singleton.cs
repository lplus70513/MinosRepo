using UnityEngine;

public class Singleton<T> : MonoBehaviour where T : MonoBehaviour
{
    private static T _instance;

    public static T Instance
    {
        get
        {
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
                }
            }
            return _instance;
        }
    }

    protected virtual void Awake()
    {
        if (_instance == null)
        {
            // 如果还没有实例，就将当前对象设为实例
            _instance = this as T;
            // 如果希望单例在场景切换时不被销毁，可以取消下面这行的注释
            // DontDestroyOnLoad(gameObject);
        }
        else if (_instance != this)
        {
            // 如果已存在另一个实例，则销毁当前这个重复的实例
            Debug.LogWarning($"[Singleton] 检测到重复实例，销毁: '{gameObject.name}'。");
            Destroy(gameObject);
        }
    }
}