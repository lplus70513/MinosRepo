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
                _instance = FindObjectOfType<T>();

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
            _instance = this as T;
            // Debug.Log($"[Singleton] {typeof(T).Name} 实例接管: '{gameObject.name}', scene={gameObject.scene.name}");
        }
        else if (_instance != this)
        {
            // Debug.LogWarning($"[Singleton] {typeof(T).Name} 检测到重复实例，销毁旧实例 '{_instance.gameObject.name}' (scene={_instance.gameObject.scene.name})，新实例 '{gameObject.name}' (scene={gameObject.scene.name}) 接管");
            Destroy(_instance.gameObject);
            _instance = this as T;
        }
    }

    protected virtual void OnDestroy()
    {
        if (_instance == this as T)
        {
            // Debug.Log($"[Singleton] {typeof(T).Name} 实例销毁，_instance 置 null, scene={gameObject.scene.name}");
            _instance = null;
        }
    }
}