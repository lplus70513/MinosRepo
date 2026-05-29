using UnityEngine;
using UnityEngine.UI;

public class WinPanelController : MonoBehaviour
{
    [SerializeField] private Button goldButton;
    [SerializeField] private Button cardButton;
    [SerializeField] private Button stringButton;
    [SerializeField] private Button claimButton;

    [SerializeField] private GoldDialog goldDialog;
    [SerializeField] private CardSelectDialog cardSelectDialog;
    [SerializeField] private StringDialog stringDialog;

    private BattleReward _reward;
    private bool _goldProcessed;
    private bool _cardProcessed;
    private bool _stringProcessed;

    void Awake()
    {
        if (claimButton != null)
            claimButton.onClick.AddListener(OnClaim);
        if (goldButton != null)
            goldButton.onClick.AddListener(OnGoldClick);
        if (cardButton != null)
            cardButton.onClick.AddListener(OnCardClick);
        if (stringButton != null)
            stringButton.onClick.AddListener(OnStringClick);
    }

    public void Initialize(BattleReward reward)
    {
        _reward = reward;
        _goldProcessed = false;
        _cardProcessed = false;
        _stringProcessed = false;

        if (goldButton != null)
            goldButton.interactable = true;
        if (cardButton != null)
            cardButton.interactable = true;

        if (reward.HasString && stringButton != null)
        {
            stringButton.gameObject.SetActive(true);
            stringButton.interactable = true;
        }
        else if (stringButton != null)
        {
            stringButton.gameObject.SetActive(false);
            _stringProcessed = true;
        }

        if (claimButton != null)
            claimButton.interactable = true;
    }

    private void OnGoldClick()
    {
        if (_goldProcessed) return;
        if (goldDialog == null) return;
        goldDialog.Show(_reward.GoldAmount,
            onCollected: () => { _goldProcessed = true; if (goldButton != null) goldButton.interactable = false; },
            onSkipped: () => { _goldProcessed = true; if (goldButton != null) goldButton.interactable = false; });
    }

    private void OnCardClick()
    {
        if (_cardProcessed) return;
        if (cardSelectDialog == null) return;
        cardSelectDialog.Show(_reward.CardChoices,
            onSelected: (card) =>
            {
                GameManager.Instance.WorldMapState.currentDeck.Add(card);
                _cardProcessed = true;
                if (cardButton != null) cardButton.interactable = false;
            },
            onSkipped: () => { });
    }

    private void OnStringClick()
    {
        if (_stringProcessed) return;
        if (stringDialog == null) return;
        stringDialog.Show(
            onCollected: () => { _stringProcessed = true; if (stringButton != null) stringButton.interactable = false; },
            onSkipped: () => { _stringProcessed = true; if (stringButton != null) stringButton.interactable = false; });
    }

    private void OnClaim()
    {
        Debug.Log("[WinPanel] 退出战斗");
        var hero = HeroSystem.Instance?.HeroView;
        if (hero != null)
            GameManager.Instance.SaveBattleResult(hero.CurrentHealth);
        GameManager.Instance.ExitEncounter();
    }
}
