using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    private static GameManager _instance;

    // 优化点：提供一个公共的只读属性，外部无法修改
    public static GameManager Instance
    {
        get
        {
            // 如果实例为空，尝试在场景中查找（防止Awake还没执行时的访问）
            if (_instance == null)
            {
                _instance = FindObjectOfType<GameManager>();
            }
            return _instance;
        }
    }

    [Header("UI 引用")]
    public GameObject SettingsPanel;

    private void Awake()
    {
        // 1. 单例检查
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        // 2. 赋值并持久化
        _instance = this;
        DontDestroyOnLoad(gameObject);

        // 3. 加载主菜单
        // 建议：加一个判断，防止重复加载
        if (SceneManager.GetSceneByName("MainMenu").isLoaded == false)
        {
            SceneManager.LoadScene("MainMenu", LoadSceneMode.Additive);
        }

        if (SettingsPanel == null)
        {
            // 尝试在场景中通过名字查找
            GameObject panelObj = GameObject.Find("SettingsPanel");
            if (panelObj != null)
            {
                SettingsPanel = panelObj;
                Debug.Log("自动找到了 SettingsPanel！");
            }
        }
    }

    public void OpenSettings()
    {
        if (SettingsPanel != null)
        {
            SettingsPanel.SetActive(true);
            Debug.Log("设置界面已打开");
        }
        else
        {
            Debug.LogError("错误：SettingsPanel 引用未赋值！请检查场景0的Inspector。");
        }
    }

    public void CloseSettings()
    {
        if (SettingsPanel != null)
        {
            SettingsPanel.SetActive(false);
            Debug.Log("设置界面已关闭");
        }
    }
}