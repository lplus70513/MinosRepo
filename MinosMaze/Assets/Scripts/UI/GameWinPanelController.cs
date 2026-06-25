using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GameWinPanelController : MonoBehaviour
{
    [Header("返回")]
    [SerializeField] private Button returnButton;

    [Header("得分显示")]
    [SerializeField] private TMP_Text playerNameText;
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private TMP_Text statsText;
    [SerializeField] private Button saveToLeaderboardButton;

    void Awake()
    {
        if (returnButton == null)
            returnButton = GetComponentInChildren<Button>();
        if (returnButton != null)
            returnButton.onClick.AddListener(OnReturnMainMenu);

        if (saveToLeaderboardButton != null)
            saveToLeaderboardButton.onClick.AddListener(OnSaveToLeaderboard);
    }

    void OnEnable()
    {
        RefreshStats();
    }

    private void RefreshStats()
    {
        var stats = RunStatistics.Instance;
        if (stats == null) return;

        stats.Finalize(true);
        int score = stats.CalculateScore();

        if (playerNameText != null)
            playerNameText.text = stats.PlayerName;
        if (scoreText != null)
            scoreText.text = "得分: " + score;
        if (statsText != null)
            statsText.text = "最高层数: " + stats.MaxFloorReached
                + "\n击杀敌人: " + stats.EnemiesKilled
                + "\n造成伤害: " + stats.TotalDamageDealt
                + "\n受到伤害: " + stats.TotalDamageTaken
                + "\n回合数: " + stats.TurnsTaken
                + "\n金币: " + stats.GoldCollected
                + "\n卡牌数: " + stats.CardsObtained
                + "\n祝福数: " + stats.BlessingsObtained
                + "\n用时: " + stats.ElapsedTime.ToString("F1") + "秒";
    }

    private void OnSaveToLeaderboard()
    {
        var leaderboard = LeaderboardSystem.Instance;
        var stats = RunStatistics.Instance;
        if (leaderboard != null && stats != null)
        {
            leaderboard.AddEntry(stats);
            Debug.Log("[GameWinPanel] 已保存排行榜记录");
        }
    }

    private void OnReturnMainMenu()
    {
        Debug.Log("[GameWinPanel] 返回主界面");
        GameManager.Instance.ReturnToMainMenu();
    }
}
