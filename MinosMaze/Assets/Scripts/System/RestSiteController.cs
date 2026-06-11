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
    [SerializeField] private CardSelectDialog cardSelectDialog;

    [Header("配置")]
    [SerializeField, Range(0f, 1f)] private float healPercent = 0.3f;
    [SerializeField] private float exitDelay = 1f;

    private bool optionChosen;

    void Start()
    {
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

        GameManager gm = GameManager.Instance;
        var deck = gm.WorldMapState.currentDeck;
        List<CardData> cards = deck.ConvertAll(e => e.CardData);

        cardSelectDialog.Show(cards, (selectedCard) =>
        {
            DeckCardEntry entry = deck.Find(e => e.CardData == selectedCard);
            if (entry != null)
                deck.Remove(entry);
            OnOptionComplete();
        }, () =>
        {
            optionChosen = false;
        });
    }

    private void OnUpgrade()
    {
        if (optionChosen) return;
        optionChosen = true;

        GameManager gm = GameManager.Instance;
        var deck = gm.WorldMapState.currentDeck;
        List<CardData> upgradeable = deck
            .FindAll(e => !e.IsUpgraded && e.CardData != null && e.CardData.UpgradeGrade != null)
            .ConvertAll(e => e.CardData);

        cardSelectDialog.Show(upgradeable, (selectedCard) =>
        {
            DeckCardEntry entry = deck.Find(e => e.CardData == selectedCard && !e.IsUpgraded);
            if (entry != null)
                entry.IsUpgraded = true;
            OnOptionComplete();
        }, () =>
        {
            optionChosen = false;
        });
    }

    private void OnOptionComplete()
    {
        if (burningBackground != null) burningBackground.SetActive(false);
        if (extinguishedBackground != null) extinguishedBackground.SetActive(true);

        healButton.interactable = false;
        discardButton.interactable = false;
        upgradeButton.interactable = false;

        StartCoroutine(DelayedExit());
    }

    private IEnumerator DelayedExit()
    {
        yield return new WaitForSeconds(exitDelay);
        GameManager.Instance.ExitEncounter();
    }
}
