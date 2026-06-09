using System.Collections;
using System.Collections.Generic;
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
    [SerializeField] private string worldMapSceneName = "2.0_WorldMap";
    [SerializeField] private HeroData heroData;

    // 大地图可序列化状态，跨场景保存/恢复
    public WorldMapState WorldMapState = new();

    // 当前待加载的遭遇标识（cellType + floor），由 WorldMapMovementSystem 写入
    public EncounterConfig PendingEncounter { get; set; }

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
        if (SceneManager.GetSceneByName("1_Mainmenu").isLoaded == false)
        {
            SceneManager.LoadScene("1_Mainmenu", LoadSceneMode.Additive);
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


    public void GameStart()
    {
        NewGame();
    }

    // ========== 大地图场景管理 ==========

    // 开始新游戏：卸载 MainMenu，加载大地图

    public void NewGame()
    {
        WorldMapState = new WorldMapState();
        WorldMapState.playerPosX = -999;
        WorldMapState.playerPosZ = -999;
        if (heroData != null)
        {
            WorldMapState.maxHealth = heroData.Health;
            WorldMapState.currentHealth = heroData.Health;
            WorldMapState.currentDeck = heroData.Deck != null
                ? heroData.Deck.FindAll(cd => cd != null).ConvertAll(cd => new DeckCardEntry(cd, false))
                : new List<DeckCardEntry>();
        }
        if (SceneManager.GetSceneByName("1_MainMenu").isLoaded)
            SceneManager.UnloadSceneAsync("1_MainMenu");
        SceneManager.LoadScene(worldMapSceneName, LoadSceneMode.Additive);
    }

    // 保存大地图状态（由 WorldMapMovementSystem 在场景跳转前调用）
    public void SaveWorldMapState(int x, int z, int movePoints, int currentHealth, int maxHealth)
    {
        WorldMapState.playerPosX = x;
        WorldMapState.playerPosZ = z;
        WorldMapState.remainingMovePoints = movePoints;
        WorldMapState.currentHealth = currentHealth;
        WorldMapState.maxHealth = maxHealth;
        WorldMapState.isNewGame = false;
    }

    // 战斗结束时保存玩家生命值（由战斗场景退出按钮调用）
    public void SaveBattleResult(int currentHealth, int maxHealth)
    {
        WorldMapState.currentHealth = currentHealth;
        WorldMapState.maxHealth = maxHealth;
        WorldMapState.isNewGame = false;
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
        yield return null;
        SceneManager.SetActiveScene(SceneManager.GetSceneByName(sceneName));
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
        yield return null;
        SceneManager.SetActiveScene(SceneManager.GetSceneByName(worldMapSceneName));
    }

    // 战斗失败时返回主菜单
    public void ReturnToMainMenu()
    {
        StartCoroutine(ReturnToMainMenuRoutine());
    }

    private IEnumerator ReturnToMainMenuRoutine()
    {
        if (!string.IsNullOrEmpty(currentEncounterScene))
        {
            AsyncOperation unload = SceneManager.UnloadSceneAsync(currentEncounterScene);
            if (unload != null)
                yield return unload;
        }
        if (SceneManager.GetSceneByName(worldMapSceneName).isLoaded)
        {
            AsyncOperation unload = SceneManager.UnloadSceneAsync(worldMapSceneName);
            if (unload != null)
                yield return unload;
        }
        if (!SceneManager.GetSceneByName("1_Mainmenu").isLoaded)
            SceneManager.LoadScene("1_Mainmenu", LoadSceneMode.Additive);
    }

    // GameOver：移动点耗尽且未到 BOSS 格（暂留空）
    public void OnGameOver()
    {
        Debug.Log("[GameManager] 移动点耗尽，游戏失败");
    }
}