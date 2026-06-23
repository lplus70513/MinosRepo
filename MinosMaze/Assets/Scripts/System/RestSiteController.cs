using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class RestSiteController : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Button healButton;
    [SerializeField] private Button discardButton;
    [SerializeField] private Button upgradeButton;
    [SerializeField] private GameObject burningBackground;
    [SerializeField] private GameObject extinguishedBackground;
    [SerializeField] private DeckViewer deckViewer;
    [SerializeField] private HealthBarPanel healthBarPanel;

    [Header("悬停描述")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private string healTitle = "小憩";
    [SerializeField] private string discardTitle = "丢弃";
    [SerializeField] private string upgradeTitle = "升级";
    [SerializeField] private string healDescription = "恢复30%最大生命值";
    [SerializeField] private string discardDescription = "从牌组中选择并移除一张牌";
    [SerializeField] private string upgradeDescription = "选择一张牌进行升级";

    [Header("配置")]
    [SerializeField, Range(0f, 1f)] private float healPercent = 0.3f;
    [SerializeField] private float exitDelay = 1f;

    private bool optionChosen;

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

        healButton.onClick.AddListener(OnHeal);
        discardButton.onClick.AddListener(OnDiscard);
        upgradeButton.onClick.AddListener(OnUpgrade);

        AddHoverEffect(healButton, healTitle, healDescription);
        AddHoverEffect(discardButton, discardTitle, discardDescription);
        AddHoverEffect(upgradeButton, upgradeTitle, upgradeDescription);
        EnsureTitleText();
        EnsureDescriptionText();

        UpdateButtonStates();
    }

    private void AddHoverEffect(Button button, string title, string description)
    {
        if (button == null) return;

        EventTrigger trigger = button.GetComponent<EventTrigger>();
        if (trigger == null)
            trigger = button.gameObject.AddComponent<EventTrigger>();

        EventTrigger.Entry enterEntry = new EventTrigger.Entry();
        enterEntry.eventID = EventTriggerType.PointerEnter;
        enterEntry.callback.AddListener((data) => ShowHoverInfo(title, description));
        trigger.triggers.Add(enterEntry);

        EventTrigger.Entry exitEntry = new EventTrigger.Entry();
        exitEntry.eventID = EventTriggerType.PointerExit;
        exitEntry.callback.AddListener((data) => HideHoverInfo());
        trigger.triggers.Add(exitEntry);
    }

    private void EnsureTitleText()
    {
        if (titleText != null) return;

        Canvas parentCanvas = healButton?.GetComponentInParent<Canvas>();
        if (parentCanvas == null) return;

        GameObject textGo = new GameObject("悬停标题文本");
        textGo.transform.SetParent(parentCanvas.transform, false);

        titleText = textGo.AddComponent<TextMeshProUGUI>();
        titleText.fontSize = 48;
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.color = Color.white;
        titleText.fontStyle = FontStyles.Bold;

        RectTransform rt = titleText.rectTransform;
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(0, 60);
        rt.sizeDelta = new Vector2(600, 60);

        textGo.SetActive(false);
    }

    private void EnsureDescriptionText()
    {
        if (descriptionText != null) return;

        Canvas parentCanvas = healButton?.GetComponentInParent<Canvas>();
        if (parentCanvas == null) return;

        GameObject textGo = new GameObject("悬停描述文本");
        textGo.transform.SetParent(parentCanvas.transform, false);

        descriptionText = textGo.AddComponent<TextMeshProUGUI>();
        descriptionText.fontSize = 36;
        descriptionText.alignment = TextAlignmentOptions.Center;
        descriptionText.color = Color.white;
        descriptionText.fontStyle = FontStyles.Bold;

        RectTransform rt = descriptionText.rectTransform;
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(800, 100);

        textGo.SetActive(false);
    }

    private void ShowHoverInfo(string title, string description)
    {
        if (titleText != null)
        {
            titleText.text = title;
            titleText.gameObject.SetActive(true);
        }
        if (descriptionText != null)
        {
            descriptionText.text = description;
            descriptionText.gameObject.SetActive(true);
        }
    }

    private void HideHoverInfo()
    {
        if (titleText != null)
            titleText.gameObject.SetActive(false);
        if (descriptionText != null)
            descriptionText.gameObject.SetActive(false);
    }

    private void UpdateButtonStates()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null) return;

        var deck = gm.WorldMapState.currentDeck;

        if (deck == null || deck.Count <= 1)
            discardButton.interactable = false;

        bool hasUpgradeable = deck != null && deck.Exists(e => !e.IsUpgraded && e.CardData != null && e.CardData.UpgradeGrade != null);
        if (!hasUpgradeable)
            upgradeButton.interactable = false;
    }

    private void OnHeal()
    {
        if (optionChosen) return;
        optionChosen = true;

        GameManager gm = GameManager.Instance;
        if (gm != null)
        {
            int heal = Mathf.CeilToInt(gm.WorldMapState.maxHealth * healPercent);
            gm.WorldMapState.currentHealth = Mathf.Min(
                gm.WorldMapState.currentHealth + heal,
                gm.WorldMapState.maxHealth);
        }

        AudioManager.Instance?.PlaySFX(AudioManager.Instance?.Config?.healSFX);
        OnOptionComplete();
    }

    private void OnDiscard()
    {
        if (optionChosen) return;
        optionChosen = true;
        HideButtons();

        GameManager gm = GameManager.Instance;
        var deck = gm.WorldMapState.currentDeck;

        // 启用燃烧消融特效
        deckViewer.SetBurnMode(true);

        deckViewer.OpenForSelection(deck, (entry) =>
        {
            deck.Remove(entry);
            OnOptionComplete();
        }, () =>
        {
            deckViewer.SetBurnMode(false);
            ShowButtons();
            optionChosen = false;
        });
    }

    private void OnUpgrade()
    {
        if (optionChosen) return;
        optionChosen = true;
        HideButtons();

        GameManager gm = GameManager.Instance;
        var deck = gm.WorldMapState.currentDeck;
        var upgradeable = deck.FindAll(e => !e.IsUpgraded && e.CardData != null && e.CardData.UpgradeGrade != null);

        deckViewer.OpenForSelection(upgradeable, (entry) =>
        {
            entry.IsUpgraded = true;
            OnOptionComplete();
        }, () =>
        {
            ShowButtons();
            optionChosen = false;
        }, upgradePreview: true);
    }

    private void OnOptionComplete()
    {
        if (burningBackground != null) burningBackground.SetActive(false);
        if (extinguishedBackground != null) extinguishedBackground.SetActive(true);

        HideButtons();
        StartCoroutine(DelayedExit());
    }

    private IEnumerator DelayedExit()
    {
        yield return new WaitForSeconds(exitDelay);
        GameManager.Instance.ExitEncounter();
    }

    private void HideButtons()
    {
        HideHoverInfo();
        healButton.gameObject.SetActive(false);
        discardButton.gameObject.SetActive(false);
        upgradeButton.gameObject.SetActive(false);
    }

    private void ShowButtons()
    {
        healButton.gameObject.SetActive(true);
        discardButton.gameObject.SetActive(true);
        upgradeButton.gameObject.SetActive(true);
    }
}
