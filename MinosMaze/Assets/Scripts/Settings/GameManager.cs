using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using DG.Tweening;

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
    [SerializeField] private CanvasGroup settingsCanvasGroup;
    [SerializeField] private float settingsFadeDuration = 0.15f;

    [Header("游戏结束面板")]
    [SerializeField] private GameObject gameWinPanel;
    [SerializeField] private GameObject gameLosePanel;

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

    // 进入遭遇格之前的大地图快照，供遭遇场景内"保存并退出"回退到进入前状态
    [System.NonSerialized]
    private WorldMapState preEncounterSnapshot;

    // 当前待加载的遭遇标识（cellType + floor），由 WorldMapMovementSystem 写入
    public EncounterConfig PendingEncounter { get; set; }

    // 当前遭遇是否为 BOSS 格（中心格）战斗，由 WorldMapMovementSystem.SetPendingEncounter 同步写入
    public bool IsBossEncounter { get; set; }

    // 当前加载的遭遇子场景名
    private string currentEncounterScene;

    private bool isStartingGame;
    private bool isExitingEncounter;

    public static bool PendingNewGame { get; set; }

    public bool IsInGame
    {
        get
        {
            // 只要不在主菜单，即视为游戏进行中（大地图或任意遭遇场景：宝箱/战斗/雕像/火堆）
            return !SceneManager.GetSceneByName("1_MainMenu").isLoaded;
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

        FindGamePanelsIfNull();
        if (gameWinPanel != null) gameWinPanel.SetActive(false);
        if (gameLosePanel != null) gameLosePanel.SetActive(false);

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

        AudioManager.Instance?.PlayBGMForScene("1_MainMenu");

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

            if (settingsCanvasGroup == null)
                settingsCanvasGroup = SettingsPanel.GetComponent<CanvasGroup>();
            if (settingsCanvasGroup == null)
                settingsCanvasGroup = SettingsPanel.AddComponent<CanvasGroup>();

            settingsCanvasGroup.alpha = 0f;
            settingsCanvasGroup.DOFade(1f, settingsFadeDuration).SetEase(Ease.OutQuad);

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
            if (settingsCanvasGroup == null)
                settingsCanvasGroup = SettingsPanel.GetComponent<CanvasGroup>();

            if (settingsCanvasGroup != null)
            {
                settingsCanvasGroup.DOFade(0f, settingsFadeDuration)
                    .SetEase(Ease.OutQuad)
                    .OnComplete(() => SettingsPanel.SetActive(false));
            }
            else
            {
                SettingsPanel.SetActive(false);
            }
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

    private void FindGamePanelsIfNull()
    {
        if (gameWinPanel == null)
        {
            var allObjects = FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var obj in allObjects)
            {
                if (obj.name == "GameWinPanel")
                {
                    gameWinPanel = obj;
                    Debug.Log("[GameManager] 自动找到了 GameWinPanel");
                    break;
                }
            }
        }
        if (gameLosePanel == null)
        {
            var allObjects = FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var obj in allObjects)
            {
                if (obj.name == "GameLosePanel")
                {
                    gameLosePanel = obj;
                    Debug.Log("[GameManager] 自动找到了 GameLosePanel");
                    break;
                }
            }
        }
    }


    public void GameStart()
    {
        isStartingGame = false;
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
        Debug.Log($"[GameManager] ResetWorldMapStateForNewGame: 清空 PendingEncounter (此前 cellType={PendingEncounter?.cellType.ToString() ?? "null"}), IsBossEncounter={IsBossEncounter}");
        PendingEncounter = null;
        IsBossEncounter = false;
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

        if (!string.IsNullOrEmpty(currentEncounterScene)
            && SceneManager.GetSceneByName(currentEncounterScene).isLoaded)
        {
            AsyncOperation unloadEnc = SceneManager.UnloadSceneAsync(currentEncounterScene);
            if (unloadEnc != null)
                yield return unloadEnc;
        }
        currentEncounterScene = null;

        Instance.ResetWorldMapStateForNewGame();
        PendingNewGame = true;
        if (SceneManager.GetSceneByName("1_MainMenu").isLoaded)
            SceneManager.UnloadSceneAsync("1_MainMenu");
        SceneManager.LoadScene(worldMapSceneName, LoadSceneMode.Additive);
        yield return null;

        yield return SceneTransitionSystem.Instance.BlackScreenWait();
        AudioManager.Instance?.PlayBGMForScene(worldMapSceneName);
        yield return SceneTransitionSystem.Instance.FadeIn();
        isStartingGame = false;
    }

    // 玩家踏入遭遇格之前，记录"进入前"大地图快照，供保存并退出回退
    public void CaptureEncounterEntrySnapshot(int playerX, int playerZ, int movePoints)
    {
        preEncounterSnapshot = WorldMapState.Clone();
        preEncounterSnapshot.playerPosX = playerX;
        preEncounterSnapshot.playerPosZ = playerZ;
        preEncounterSnapshot.stringCount = movePoints;
        preEncounterSnapshot.isNewGame = false;
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
        AudioManager.Instance?.PlayBGMForScene(sceneName, PendingEncounter?.cellType ?? MapCellType.Battle_Empty);
        yield return SceneTransitionSystem.Instance.FadeIn();
    }

    // 退出遭遇子场景：标记当前格为已清除，卸载子场景，重新加载大地图
    public void ExitEncounter()
    {
        if (isExitingEncounter) return;
        isExitingEncounter = true;

        Debug.Log($"[GameManager] ExitEncounter: 清空 PendingEncounter (此前 cellType={PendingEncounter?.cellType.ToString() ?? "null"}), IsBossEncounter={IsBossEncounter}");
        PendingEncounter = null;
        IsBossEncounter = false;

        // 遭遇已正常完成，进入前快照作废
        preEncounterSnapshot = null;

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
        AudioManager.Instance?.PlayBGMForScene(worldMapSceneName);
        yield return SceneTransitionSystem.Instance.FadeIn();

        isExitingEncounter = false;
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
        AudioManager.Instance?.PlayBGMForScene(newSceneName, PendingEncounter?.cellType ?? MapCellType.Battle_Empty);
        yield return SceneTransitionSystem.Instance.FadeIn();
    }

    // 战斗失败/游戏胜利/游戏失败时返回主菜单
    public void ReturnToMainMenu()
    {
        Debug.Log($"[GameManager] ReturnToMainMenu: 清空 PendingEncounter (此前 cellType={PendingEncounter?.cellType.ToString() ?? "null"}), IsBossEncounter={IsBossEncounter}");
        PendingEncounter = null;
        IsBossEncounter = false;
        if (gameWinPanel != null) gameWinPanel.SetActive(false);
        if (gameLosePanel != null) gameLosePanel.SetActive(false);
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
        AudioManager.Instance?.PlayBGMForScene("1_MainMenu");
        yield return SceneTransitionSystem.Instance.FadeIn();
    }

    // GameOver：移动点耗尽/周围坍塌触发游戏失败
    public void OnGameOver()
    {
        Debug.Log("[GameManager] 游戏失败");
        ShowGameLose();
    }

    // 游戏胜利：中心格战斗完成后调用
    public void ShowGameWin()
    {
        Debug.Log("[GameManager] 游戏胜利！");
        FindGamePanelsIfNull();
        if (Interactions.Instance != null) Interactions.Instance.IsShowingReward = false;
        if (gameWinPanel == null)
        {
            Debug.LogError("[GameManager] gameWinPanel 未找到！请在 0_Manager 场景中创建名为 GameWinPanel 的 GameObject 或拖入引用");
            return;
        }
        gameWinPanel.SetActive(true);
    }

    // 游戏失败：血量归0/线耗尽/周围坍塌时调用
    public void ShowGameLose()
    {
        Debug.Log("[GameManager] 游戏失败！");
        FindGamePanelsIfNull();
        if (Interactions.Instance != null) Interactions.Instance.IsShowingReward = false;
        if (gameLosePanel == null)
        {
            Debug.LogError("[GameManager] gameLosePanel 未找到！请在 0_Manager 场景中创建名为 GameLosePanel 的 GameObject 或拖入引用");
            return;
        }
        gameLosePanel.SetActive(true);
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

        if (!string.IsNullOrEmpty(currentEncounterScene)
            && SceneManager.GetSceneByName(currentEncounterScene).isLoaded)
        {
            AsyncOperation unloadEnc = SceneManager.UnloadSceneAsync(currentEncounterScene);
            if (unloadEnc != null)
                yield return unloadEnc;
        }
        currentEncounterScene = null;

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
        AudioManager.Instance?.PlayBGMForScene(worldMapSceneName);
        yield return SceneTransitionSystem.Instance.FadeIn();
        isStartingGame = false;
    }

    public void AbandonGame()
    {
        SaveSystem.DeleteSave();
        isStartingGame = false;
        preEncounterSnapshot = null;
        CloseSettings();
        ReturnToMainMenu();
    }

    public void SaveAndExit()
    {
        bool onWorldMap = string.IsNullOrEmpty(currentEncounterScene)
            && SceneManager.GetSceneByName(worldMapSceneName).isLoaded;
        if (onWorldMap)
        {
            var move = FindObjectOfType<WorldMapMovementSystem>();
            if (move != null) move.SaveStateToGameManager();
            SaveSystem.Save(WorldMapState);
        }
        else
        {
            // 遭遇场景内退出：回退到进入该格之前的快照
            SaveSystem.Save(preEncounterSnapshot != null ? preEncounterSnapshot : WorldMapState);
        }

        preEncounterSnapshot = null;
        CloseSettings();
        ReturnToMainMenu();
    }
}