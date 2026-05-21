using System.Collections;
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

    [Header("大地图配置")]
    [SerializeField] private string worldMapSceneName = "WorldMap";

    // 大地图可序列化状态，跨场景保存/恢复
    public WorldMapState WorldMapState = new();

    // 当前加载的遭遇子场景名
    private string currentEncounterScene;

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
            // GameObject.Find 找不到非激活物体，SettingsPanel 默认关闭，需要换用能查找非激活的 API
            var allObjects = FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var obj in allObjects)
            {
                if (obj.name == "SettingsPanel")
                {
                    SettingsPanel = obj;
                    Debug.Log("自动找到了 SettingsPanel！");
                    break;
                }
            }
        }
    }

    // ========== 大地图场景管理 ==========

    // 开始新游戏：卸载 MainMenu，加载大地图
    public void NewGame()
    {
        WorldMapState = new WorldMapState();
        if (SceneManager.GetSceneByName("MainMenu").isLoaded)
            SceneManager.UnloadSceneAsync("MainMenu");
        SceneManager.LoadScene(worldMapSceneName, LoadSceneMode.Additive);
    }

    // 保存大地图状态（由 WorldMapMovementSystem 在场景跳转前调用）
    public void SaveWorldMapState(int x, int z, int movePoints)
    {
        WorldMapState.playerPosX = x;
        WorldMapState.playerPosZ = z;
        WorldMapState.remainingMovePoints = movePoints;
    }

    // 进入遭遇子场景：保存状态，卸载大地图，加载子场景
    public void EnterEncounter(string sceneName)
    {
        currentEncounterScene = sceneName;
        StartCoroutine(EnterEncounterRoutine(sceneName));
    }

    private IEnumerator EnterEncounterRoutine(string sceneName)
    {
        AsyncOperation unload = SceneManager.UnloadSceneAsync(worldMapSceneName);
        if (unload != null)
            yield return unload;
        SceneManager.LoadScene(sceneName, LoadSceneMode.Additive);
    }

    // 退出遭遇子场景：标记当前格为已清除，卸载子场景，重新加载大地图
    public void ExitEncounter()
    {
        Vector2Int cell = new(WorldMapState.playerPosX, WorldMapState.playerPosZ);
        if (!WorldMapState.clearedCells.Contains(cell))
            WorldMapState.clearedCells.Add(cell);

        StartCoroutine(ExitEncounterRoutine());
    }

    private IEnumerator ExitEncounterRoutine()
    {
        if (!string.IsNullOrEmpty(currentEncounterScene))
        {
            AsyncOperation unload = SceneManager.UnloadSceneAsync(currentEncounterScene);
            if (unload != null)
                yield return unload;
        }
        SceneManager.LoadScene(worldMapSceneName, LoadSceneMode.Additive);
    }

    // GameOver：移动点耗尽且未到 BOSS 格（暂留空）
    public void OnGameOver()
    {
        Debug.Log("[GameManager] 移动点耗尽，游戏失败");
    }
}