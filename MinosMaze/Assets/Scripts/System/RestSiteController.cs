using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

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
        UpdateButtonStates();
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

        OnOptionComplete();
    }

    private void OnDiscard()
    {
        if (optionChosen) return;
        optionChosen = true;
        HideButtons();

        GameManager gm = GameManager.Instance;
        var deck = gm.WorldMapState.currentDeck;

        deckViewer.OpenForSelection(deck, (entry) =>
        {
            deck.Remove(entry);
            OnOptionComplete();
        }, () =>
        {
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
