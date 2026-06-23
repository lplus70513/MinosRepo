using UnityEngine;
using UnityEngine.UI;

public class TreasureController : MonoBehaviour
{
    [Header("背景")]
    [SerializeField] private GameObject normalBackground;
    [SerializeField] private GameObject mimicBackground;

    [Header("宝箱按钮")]
    [SerializeField] private Button normalChestButton;
    [SerializeField] private Button mimicChestButton;

    [Header("宝箱怪 UI")]
    [SerializeField] private Button escapeButton;

    [Header("奖励")]
    [SerializeField] private GameObject winPanel;
    [SerializeField] private RewardConfig rewardConfig;

    [Header("UI")]
    [SerializeField] private HealthBarPanel healthBarPanel;

    [Header("配置")]
    [SerializeField, Range(0f, 1f)] private float mimicChance = 0.1f;
    [SerializeField] private string battleSceneName = "2.1_BattleScene";

    private bool isMimic;

    void Start()
    {
        GameManager gm = GameManager.Instance;
        if (gm != null && gm.WorldMapState != null)
        {
            if (gm.WorldMapState.currentDeck != null)
                CardSystem.Instance.SetUp(gm.WorldMapState.currentDeck);

            if (healthBarPanel != null)
                healthBarPanel.SetupWorldMap(gm.WorldMapState.maxHealth, gm.WorldMapState.currentHealth);
        }

        isMimic = Random.value < mimicChance;

        if (isMimic)
        {
            normalBackground.SetActive(false);
            mimicBackground.SetActive(true);
            normalChestButton.gameObject.SetActive(false);
            mimicChestButton.gameObject.SetActive(true);
            escapeButton.gameObject.SetActive(true);
            mimicChestButton.onClick.AddListener(OnFight);
            escapeButton.onClick.AddListener(OnEscape);
        }
        else
        {
            normalBackground.SetActive(true);
            mimicBackground.SetActive(false);
            normalChestButton.gameObject.SetActive(true);
            mimicChestButton.gameObject.SetActive(false);
            escapeButton.gameObject.SetActive(false);
            normalChestButton.onClick.AddListener(OnOpenChest);
        }
    }

    private void OnOpenChest()
    {
        normalChestButton.gameObject.SetActive(false);

        if (rewardConfig != null)
            RewardSystem.Instance.SetConfig(rewardConfig);

        BattleReward reward = RewardSystem.Instance.GenerateReward();

        if (winPanel != null)
        {
            winPanel.SetActive(true);
            var controller = winPanel.GetComponent<WinPanelController>();
            if (controller != null)
                controller.Initialize(reward);
        }
    }

    private void OnFight()
    {
        GameManager gm = GameManager.Instance;
        int floor = gm.WorldMapState?.floorLevel ?? 1;
        Debug.Log($"[TreasureController] OnFight: 设置 PendingEncounter.cellType=WorldMap_Encounter (此前 cellType={gm.PendingEncounter?.cellType.ToString() ?? "null"})");
        gm.PendingEncounter = new EncounterConfig
        {
            cellType = MapCellType.WorldMap_Encounter,
            floorLevel = floor
        };
        gm.RedirectEncounter(battleSceneName);
    }

    private void OnEscape()
    {
        GameManager.Instance.ExitEncounter();
    }
}
