using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    private static GameManager _instance;

    // �Ż��㣺�ṩһ��������ֻ�����ԣ��ⲿ�޷��޸�
    public static GameManager Instance
    {
        get
        {
            // ���ʵ��Ϊ�գ������ڳ����в��ң���ֹAwake��ûִ��ʱ�ķ��ʣ�
            if (_instance == null)
            {
                _instance = FindObjectOfType<GameManager>();
            }
            return _instance;
        }
    }

    [Header("UI ����")]
    public GameObject SettingsPanel;

    private void Awake()
    {
        // 1. �������
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        // 2. ��ֵ���־û�
        _instance = this;
        DontDestroyOnLoad(gameObject);

        // 3. �������˵�
        // ���飺��һ���жϣ���ֹ�ظ�����
        if (SceneManager.GetSceneByName("MainMenu").isLoaded == false)
        {
            SceneManager.LoadScene("MainMenu", LoadSceneMode.Additive);
        }

        if (SettingsPanel == null)
        {
            // �����ڳ�����ͨ�����ֲ���
            GameObject panelObj = GameObject.Find("SettingsPanel");
            if (panelObj != null)
            {
                SettingsPanel = panelObj;
                Debug.Log("�Զ��ҵ��� SettingsPanel��");
            }
        }
    }

    public void OpenSettings()
    {
        FindSettingsPanelIfNull();
        if (SettingsPanel != null)
        {
            SettingsPanel.SetActive(true);
            Debug.Log("设置面板已打开");
        }
        else
        {
            Debug.LogError("设置SettingsPanel 引用未赋值！请检查场景0的Inspector。");
        }
    }

    public void CloseSettings()
    {
        FindSettingsPanelIfNull();
        if (SettingsPanel != null)
        {
            SettingsPanel.SetActive(false);
            Debug.Log("设置面板已关闭");
        }
    }

    private void FindSettingsPanelIfNull()
    {
        if (SettingsPanel == null)
        {
            GameObject panelObj = GameObject.Find("SettingsPanel");
            if (panelObj != null)
            {
                SettingsPanel = panelObj;
                Debug.Log("自动找到了 SettingsPanel！");
            }
        }
    }
}