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
    [SerializeField] private CardDatabase cardDatabase;

    [Header("场景切换渐变")]
    [SerializeField] private float fadeOutDuration = 0.3f;
    [SerializeField] private float fadeInDuration = 0.3f;
    [SerializeField] private float blackScreenDuration = 0.8f;
    [SerializeField] private int overlaySortingOrder = 100;
    [SerializeField] private string dontCoverTag = "NoFade";

    // 大地图状态，跨场景保存/恢复（NonSerialized 防止 Unity 将运行时数据烘焙到场景文件）
    [System.NonSerialized]
    public WorldMapState WorldMapState = new();

    // 当前待加载的遭遇标识（cellType + floor），由 WorldMapMovementSystem 写入
    public EncounterConfig PendingEncounter { get; set; }

    // 当前加载的遭遇子场景名
    private string currentEncounterScene;

    private bool isStartingGame;

    public static bool PendingNewGame { get; set; }

    public bool IsInGame
    {
        get
        {
            return SceneManager.GetSceneByName(worldMapSceneName).isLoaded
                || (!string.IsNullOrEmpty(currentEncounterScene)
                    && SceneManager.GetSceneByName(currentEncounterScene).isLoaded);
        }
    }

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

        WorldMapState = new WorldMapState();
        WorldMapState.stringCount = 15;
        WorldMapState.gold = 50;
        if (heroData != null)
        {
            WorldMapState.maxHealth = heroData.Health;
            WorldMapState.currentHealth = heroData.Health;
            WorldMapState.currentDeck = heroData.Deck != null
                ? heroData.Deck.FindAll(cd => cd != null).ConvertAll(cd => new DeckCardEntry(cd, false))
                : new List<DeckCardEntry>();
        }

        if (SceneTransitionSystem.Instance == null)
        {
            GameObject stsGo = new GameObject("SceneTransitionSystem");
            DontDestroyOnLoad(stsGo);
            stsGo.AddComponent<SceneTransitionSystem>();
        }
        SceneTransitionSystem.Instance.SetConfig(fadeOutDuration, fadeInDuration, blackScreenDuration, overlaySortingOrder, dontCoverTag);

        if (SceneManager.GetSceneByName("1_MainMenu").isLoaded == false)
        {
            SceneManager.LoadScene("1_MainMenu", LoadSceneMode.Additive);
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

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStaticFields()
    {
        _instance = null;
        PendingNewGame = false;
    }

    private void OnDestroy()
    {
        if (_instance == this)
            _instance = null;
    }

    public void OpenSettings()
    {
        FindSettingsPanelIfNull();
        if (SettingsPanel != null)
        {
            SettingsPanel.SetActive(true);

            var sm = SettingsPanel.GetComponent<SettingManager>();
            if (sm == null) sm = SettingsPanel.GetComponentInChildren<SettingManager>(true);
            if (sm != null) sm.RefreshButtons(IsInGame);
        }
        else
        {
            Debug.LogError("SettingsPanel 引用未赋值！请检查场景0的Inspector。");
        }
    }

    public void CloseSettings()
    {
        FindSettingsPanelIfNull();
        if (SettingsPanel != null)
        {
            SettingsPanel.SetActive(false);
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
        if (SaveSystem.HasSave())
            ContinueGame();
        else
            NewGame();
    }

    // ========== 大地图场景管理 ==========

    // 开始新游戏：卸载 MainMenu，加载大地图

    public void NewGame()
    {
        if (isStartingGame) return;
        isStartingGame = true;
        SceneTransitionSystem.Instance.StartCoroutine(NewGameRoutine());
    }

    public void ResetWorldMapStateForNewGame()
    {
        WorldMapState = new WorldMapState();
        WorldMapState.playerPosX = -999;
        WorldMapState.playerPosZ = -999;
        WorldMapState.stringCount = 15;
        WorldMapState.gold = 50;
        WorldMapState.isNewGame = true;
        if (heroData != null)
        {
            WorldMapState.maxHealth = heroData.Health;
            WorldMapState.currentHealth = heroData.Health;
            WorldMapState.currentDeck = heroData.Deck != null
                ? heroData.Deck.FindAll(cd => cd != null).ConvertAll(cd => new DeckCardEntry(cd, false))
                : new List<DeckCardEntry>();
        }
    }

    private IEnumerator NewGameRoutine()
    {
        yield return SceneTransitionSystem.Instance.FadeOut();

        Instance.ResetWorldMapStateForNewGame();
        PendingNewGame = true;
        if (SceneManager.GetSceneByName("1_MainMenu").isLoaded)
            SceneManager.UnloadSceneAsync("1_MainMenu");
        SceneManager.LoadScene(worldMapSceneName, LoadSceneMode.Additive);
        yield return null;

        yield return SceneTransitionSystem.Instance.BlackScreenWait();
        yield return SceneTransitionSystem.Instance.FadeIn();
        isStartingGame = false;
    }

    // 保存大地图状态（由 WorldMapMovementSystem 在场景跳转前调用）
    public void SaveWorldMapState(int x, int z, int stringCount, int currentHealth, int maxHealth)
    {
        WorldMapState.playerPosX = x;
        WorldMapState.playerPosZ = z;
        WorldMapState.stringCount = stringCount;
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
        SceneTransitionSystem.Instance.StartCoroutine(EnterEncounterRoutine(sceneName));
    }

    private IEnumerator EnterEncounterRoutine(string sceneName)
    {
        yield return SceneTransitionSystem.Instance.FadeOut();

        AsyncOperation unload = SceneManager.UnloadSceneAsync(worldMapSceneName);
        if (unload != null)
            yield return unload;
        SceneManager.LoadScene(sceneName, LoadSceneMode.Additive);
        yield return null;
        SceneManager.SetActiveScene(SceneManager.GetSceneByName(sceneName));

        yield return SceneTransitionSystem.Instance.BlackScreenWait();
        yield return SceneTransitionSystem.Instance.FadeIn();
    }

    // 退出遭遇子场景：标记当前格为已清除，卸载子场景，重新加载大地图
    public void ExitEncounter()
    {
        Vector2Int cell = new(WorldMapState.playerPosX, WorldMapState.playerPosZ);
        if (!WorldMapState.clearedCells.Contains(cell))
            WorldMapState.clearedCells.Add(cell);

        SceneTransitionSystem.Instance.StartCoroutine(ExitEncounterRoutine());
    }

    private IEnumerator ExitEncounterRoutine()
    {
        yield return SceneTransitionSystem.Instance.FadeOut();

        if (!string.IsNullOrEmpty(currentEncounterScene)
            && SceneManager.GetSceneByName(currentEncounterScene).isLoaded)
        {
            AsyncOperation unload = SceneManager.UnloadSceneAsync(currentEncounterScene);
            if (unload != null)
                yield return unload;
        }
        currentEncounterScene = null;
        SceneManager.LoadScene(worldMapSceneName, LoadSceneMode.Additive);
        yield return null;
        SceneManager.SetActiveScene(SceneManager.GetSceneByName(worldMapSceneName));

        yield return SceneTransitionSystem.Instance.BlackScreenWait();
        yield return SceneTransitionSystem.Instance.FadeIn();
    }

    // 从一个遭遇场景重定向到另一个遭遇场景（如宝箱怪跳转战斗）
    public void RedirectEncounter(string newSceneName)
    {
        SceneTransitionSystem.Instance.StartCoroutine(RedirectEncounterRoutine(newSceneName));
    }

    private IEnumerator RedirectEncounterRoutine(string newSceneName)
    {
        yield return SceneTransitionSystem.Instance.FadeOut();

        if (!string.IsNullOrEmpty(currentEncounterScene)
            && SceneManager.GetSceneByName(currentEncounterScene).isLoaded)
        {
            AsyncOperation unload = SceneManager.UnloadSceneAsync(currentEncounterScene);
            if (unload != null)
                yield return unload;
        }

        currentEncounterScene = newSceneName;
        SceneManager.LoadScene(newSceneName, LoadSceneMode.Additive);
        yield return null;
        SceneManager.SetActiveScene(SceneManager.GetSceneByName(newSceneName));

        yield return SceneTransitionSystem.Instance.BlackScreenWait();
        yield return SceneTransitionSystem.Instance.FadeIn();
    }

    // 战斗失败时返回主菜单
    public void ReturnToMainMenu()
    {
        isStartingGame = false;
        SceneTransitionSystem.Instance.StartCoroutine(ReturnToMainMenuRoutine());
    }

    private IEnumerator ReturnToMainMenuRoutine()
    {
        yield return SceneTransitionSystem.Instance.FadeOut();

        if (!string.IsNullOrEmpty(currentEncounterScene)
            && SceneManager.GetSceneByName(currentEncounterScene).isLoaded)
        {
            AsyncOperation unload = SceneManager.UnloadSceneAsync(currentEncounterScene);
            if (unload != null)
                yield return unload;
        }
        currentEncounterScene = null;
        if (SceneManager.GetSceneByName(worldMapSceneName).isLoaded)
        {
            AsyncOperation unload = SceneManager.UnloadSceneAsync(worldMapSceneName);
            if (unload != null)
                yield return unload;
        }
        if (!SceneManager.GetSceneByName("1_MainMenu").isLoaded)
            SceneManager.LoadScene("1_MainMenu", LoadSceneMode.Additive);

        yield return SceneTransitionSystem.Instance.BlackScreenWait();
        yield return SceneTransitionSystem.Instance.FadeIn();
    }

    // GameOver：移动点耗尽且未到 BOSS 格（暂留空）
    public void OnGameOver()
    {
        Debug.Log("[GameManager] 移动点耗尽，游戏失败");
    }

    // ========== 存档相关 ==========

    public void ContinueGame()
    {
        if (isStartingGame) return;
        isStartingGame = true;
        SceneTransitionSystem.Instance.StartCoroutine(ContinueGameRoutine());
    }

    private IEnumerator ContinueGameRoutine()
    {
        yield return SceneTransitionSystem.Instance.FadeOut();

        WorldMapState loaded = SaveSystem.Load(Instance.cardDatabase);
        if (loaded != null)
        {
            Instance.WorldMapState = loaded;
        }

        if (SceneManager.GetSceneByName("1_MainMenu").isLoaded)
            SceneManager.UnloadSceneAsync("1_MainMenu");

        SceneManager.LoadScene(worldMapSceneName, LoadSceneMode.Additive);
        yield return null;

        yield return SceneTransitionSystem.Instance.BlackScreenWait();
        yield return SceneTransitionSystem.Instance.FadeIn();
        isStartingGame = false;
    }

    public void AbandonGame()
    {
        SaveSystem.DeleteSave();
        isStartingGame = false;
        currentEncounterScene = null;
        CloseSettings();
        ReturnToMainMenu();
    }

    public void SaveAndExit()
    {
        if (WorldMapMovementSystem.Instance != null)
            WorldMapMovementSystem.Instance.SaveStateToGameManager();

        SaveSystem.Save(WorldMapState);
        CloseSettings();
        ReturnToMainMenu();
    }
}