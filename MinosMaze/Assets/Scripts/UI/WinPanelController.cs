using UnityEngine;
using UnityEngine.UI;

public class WinPanelController : MonoBehaviour
{
    [SerializeField] private Button claimButton;

    void Awake()
    {
        if (claimButton == null)
            claimButton = GetComponentInChildren<Button>();
        if (claimButton != null)
            claimButton.onClick.AddListener(OnClaimReward);
    }

    private void OnClaimReward()
    {
        Debug.Log("[WinPanel] 领取奖励，回大地图");
        var hero = HeroSystem.Instance?.HeroView;
        if (hero != null)
            GameManager.Instance.SaveBattleResult(hero.CurrentHealth);
        GameManager.Instance.ExitEncounter();
    }
}
