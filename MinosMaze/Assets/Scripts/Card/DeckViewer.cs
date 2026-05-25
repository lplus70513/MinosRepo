using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public enum PileType
{
    DrawPile,
    DiscardPile,
    ExhaustPile,
    FullDeck
}

public class DeckViewer : MonoBehaviour
{
    [Header("Panel")]
    [SerializeField] private GameObject viewerPanel;
    [SerializeField] private Button closeButton;

    [Header("Tabs")]
    [SerializeField] private Button drawPileTab;
    [SerializeField] private Button discardPileTab;
    [SerializeField] private Button exhaustPileTab;
    [SerializeField] private Button fullDeckTab;

    [Header("List")]
    [SerializeField] private Transform contentParent;
    [SerializeField] private GameObject cardListEntryPrefab;

    [Header("Info")]
    [SerializeField] private TextMeshProUGUI countText;

    private PileType currentPile = PileType.DrawPile;
    private Dictionary<PileType, Button> tabButtons;

    private void Start()
    {
        if (viewerPanel != null) viewerPanel.SetActive(false);

        tabButtons = new Dictionary<PileType, Button>
        {
            { PileType.DrawPile, drawPileTab },
            { PileType.DiscardPile, discardPileTab },
            { PileType.ExhaustPile, exhaustPileTab },
            { PileType.FullDeck, fullDeckTab }
        };

        if (drawPileTab != null) drawPileTab.onClick.AddListener(() => SwitchTab(PileType.DrawPile));
        if (discardPileTab != null) discardPileTab.onClick.AddListener(() => SwitchTab(PileType.DiscardPile));
        if (exhaustPileTab != null) exhaustPileTab.onClick.AddListener(() => SwitchTab(PileType.ExhaustPile));
        if (fullDeckTab != null) fullDeckTab.onClick.AddListener(() => SwitchTab(PileType.FullDeck));
        if (closeButton != null) closeButton.onClick.AddListener(CloseViewer);
    }

    public void OpenViewer()
    {
        if (CardSystem.Instance == null) return;

        Time.timeScale = 0f;
        SwitchTab(currentPile);
        if (viewerPanel != null) viewerPanel.SetActive(true);
    }

    public void CloseViewer()
    {
        Time.timeScale = 1f;
        if (viewerPanel != null) viewerPanel.SetActive(false);
    }

    private void SwitchTab(PileType type)
    {
        currentPile = type;
        UpdateView(type);
        HighlightTab(type);
    }

    private void UpdateView(PileType type)
    {
        if (CardSystem.Instance == null) return;

        foreach (Transform child in contentParent)
        {
            Destroy(child.gameObject);
        }

        List<Card> snapshot = GetSnapshot(type);

        if (type == PileType.DrawPile)
        {
            Shuffle(snapshot);
        }

        foreach (var card in snapshot)
        {
            GameObject entry = Instantiate(cardListEntryPrefab, contentParent);
            entry.GetComponent<CardListEntry>().SetUp(card);
        }

        if (countText != null)
        {
            string label = type switch
            {
                PileType.DrawPile => "抽牌堆",
                PileType.DiscardPile => "弃牌堆",
                PileType.ExhaustPile => "消耗堆",
                PileType.FullDeck => "完整牌组",
                _ => ""
            };
            countText.text = $"{label}: {snapshot.Count} 张";
        }
    }

    private List<Card> GetSnapshot(PileType type)
    {
        return type switch
        {
            PileType.DrawPile => CardSystem.Instance.GetDrawPileCopy(),
            PileType.DiscardPile => CardSystem.Instance.GetDiscardPileCopy(),
            PileType.ExhaustPile => CardSystem.Instance.GetExhaustPileCopy(),
            PileType.FullDeck => CardSystem.Instance.GetFullDeckCopy(),
            _ => new List<Card>()
        };
    }

    private void HighlightTab(PileType active)
    {
        foreach (var kvp in tabButtons)
        {
            if (kvp.Value != null)
                kvp.Value.interactable = (kvp.Key != active);
        }
    }

    private void Shuffle(List<Card> list)
    {
        System.Random rng = new System.Random();
        int n = list.Count;
        while (n > 1)
        {
            n--;
            int k = rng.Next(n + 1);
            (list[k], list[n]) = (list[n], list[k]);
        }
    }
}
