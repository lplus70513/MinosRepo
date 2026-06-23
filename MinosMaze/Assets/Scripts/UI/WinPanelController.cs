using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class WinPanelController : MonoBehaviour
{
    [SerializeField] private Button goldButton;
    [SerializeField] private Button cardButton;
    [SerializeField] private Button stringButton;
    [SerializeField] private Button claimButton;

    [SerializeField] private GoldDialog goldDialog;
    [SerializeField] private CardSelectDialog cardSelectDialog;
    [SerializeField] private StringDialog stringDialog;
    [SerializeField] private ResourceHUD resourceHUD;

    [Header("弹出动画")]
    [SerializeField] private Transform scaleRoot;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private float popDuration = 0.2f;

    private BattleReward _reward;
    private bool _goldProcessed;
    private bool _cardProcessed;
    private bool _stringProcessed;

    void Awake()
    {
        if (scaleRoot == null)
            scaleRoot = transform;

        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

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

        scaleRoot.localScale = Vector3.zero;
        canvasGroup.alpha = 0f;

        scaleRoot.DOScale(1f, popDuration).SetEase(Ease.OutBack);
        canvasGroup.DOFade(1f, popDuration).SetEase(Ease.OutQuad);

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
        _goldProcessed = true;
        if (goldButton != null) goldButton.interactable = false;

        if (GameManager.Instance == null) return;
        int oldGold = GameManager.Instance.WorldMapState.gold;
        GameManager.Instance.WorldMapState.gold += _reward.GoldAmount;
        if (resourceHUD != null)
            resourceHUD.AnimateGold(oldGold, GameManager.Instance.WorldMapState.gold);
        AudioManager.Instance?.PlaySFX(AudioManager.Instance?.Config?.goldCollectSFX);
    }

    private void OnCardClick()
    {
        if (_cardProcessed) return;
        if (cardSelectDialog == null) return;
        cardSelectDialog.Show(_reward.CardChoices,
            onSelected: (card) =>
            {
                if (GameManager.Instance != null)
                    GameManager.Instance.WorldMapState.currentDeck.Add(new DeckCardEntry(card, false));
                else
                    Debug.LogWarning("[WinPanel] GameManager.Instance 为 null，卡牌未加入牌组。请确保通过 Manager 场景启动游戏。");
                _cardProcessed = true;
                if (cardButton != null) cardButton.interactable = false;
            },
            onSkipped: () => { });
    }

    private void OnStringClick()
    {
        if (_stringProcessed) return;
        _stringProcessed = true;
        if (stringButton != null) stringButton.interactable = false;

        if (GameManager.Instance == null) return;
        int oldCount = GameManager.Instance.WorldMapState.stringCount;
        GameManager.Instance.WorldMapState.stringCount += 1;
        if (resourceHUD != null)
            resourceHUD.AnimateString(oldCount, GameManager.Instance.WorldMapState.stringCount);
        AudioManager.Instance?.PlaySFX(AudioManager.Instance?.Config?.stringCollectSFX);
    }

    private void OnClaim()
    {
        var hero = HeroSystem.Instance?.HeroView;
        if (hero != null)
            GameManager.Instance.SaveBattleResult(hero.CurrentHealth, hero.MaxHealth);
        GameManager.Instance.ExitEncounter();
    }
}
