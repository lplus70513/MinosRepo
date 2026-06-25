using TMPro;
using UnityEngine;

public class LeaderboardEntryView : MonoBehaviour
{
    [SerializeField] private TMP_Text rankText;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private TMP_Text detailText;

    public void Setup(int rank, LeaderboardEntry entry)
    {
        if (rankText != null)
            rankText.text = rank.ToString();

        if (nameText != null)
            nameText.text = entry.playerName;

        if (scoreText != null)
            scoreText.text = entry.score.ToString();

        if (detailText != null)
        {
            string winTag = entry.isWin ? "胜利" : "失败";
            detailText.text = $"第{entry.floorLevel}层 | 击杀{entry.enemiesKilled} | 金币{entry.goldCollected} | {winTag} | {entry.timestamp}";
        }
    }
}
