using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public class StatueSceneController : MonoBehaviour
{
    [Header("所有神像数据")]
    [SerializeField] private List<StatueData> allStatues;

    [Header("UI - 3个雕像槽位")]
    [SerializeField] private List<StatueSlotUI> slots;

    [Header("UI")]
    [SerializeField] private HealthBarPanel healthBarPanel;

    [Header("离开按钮")]
    [SerializeField] private Button leaveButton;

    [Header("卡牌操作")]
    [SerializeField] private DeckViewer deckViewer;
    [SerializeField] private RewardConfig rewardConfig;
    [SerializeField] private float cardPreviewDuration = 2f;

    [Header("配置")]
    [SerializeField] private float exitDelay = 1f;

    private StatueData[] assignedStatues = new StatueData[3];
    private BlessingEntry[] assignedBlessings = new BlessingEntry[3];
    private bool[] slotAvailable = new bool[3];
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

        SelectStatues();

        for (int i = 0; i < 3; i++)
        {
            SetupSlot(i);
            slots[i].infoPanel.SetActive(false);

            int idx = i;
            AddHoverEvents(slots[i].statueButton,
                () => { if (!optionChosen) slots[idx].infoPanel.SetActive(true); },
                () => { slots[idx].infoPanel.SetActive(false); });
            slots[i].statueButton.onClick.AddListener(() => OnSelectBlessing(idx));
        }

        if (leaveButton != null)
            leaveButton.onClick.AddListener(OnLeave);
    }

    private void SelectStatues()
    {
        var males = allStatues.FindAll(s => s.gender == StatueGender.Male);
        var females = allStatues.FindAll(s => s.gender == StatueGender.Female);
        Shuffle(males);
        Shuffle(females);

        List<StatueData> selected;
        if (UnityEngine.Random.value < 0.5f)
            selected = new List<StatueData> { males[0], males[1], females[0] };
        else
            selected = new List<StatueData> { females[0], females[1], males[0] };
        Shuffle(selected);

        for (int i = 0; i < 3; i++)
        {
            assignedStatues[i] = selected[i];
            var blessings = selected[i].blessings;
            assignedBlessings[i] = blessings[UnityEngine.Random.Range(0, blessings.Count)];
        }
    }

    private void SetupSlot(int index)
    {
        StatueData statue = assignedStatues[index];
        BlessingEntry blessing = assignedBlessings[index];
        StatueSlotUI slot = slots[index];

        if (slot.nameText != null)
            slot.nameText.text = statue.statueName;
        if (slot.descriptionText != null)
            slot.descriptionText.text = blessing.description;

        bool alreadyOwned = !blessing.repeatable && HasBlessing(blessing.blessingId);
        bool canAfford = CanAfford(blessing);

        if (alreadyOwned)
        {
            if (slot.costText != null)
                slot.costText.text = "<color=red>\u5DF2\u83B7\u5F97</color>";
            slotAvailable[index] = false;
        }
        else if (!canAfford)
        {
            if (slot.costText != null)
                slot.costText.text = $"<color=red>\u8D44\u6E90\u4E0D\u8DB3</color>\n{FormatCost(blessing)}";
            slotAvailable[index] = false;
        }
        else
        {
            if (slot.costText != null)
                slot.costText.text = FormatCost(blessing);
            slotAvailable[index] = true;
        }
    }

    private void OnSelectBlessing(int index)
    {
        if (optionChosen) return;
        if (!slotAvailable[index]) return;

        optionChosen = true;

        StatueData statue = assignedStatues[index];
        BlessingEntry blessing = assignedBlessings[index];

        DeductCost(blessing);
        GrantBlessing(statue, blessing);
        ApplySpecialInteractions(statue);

        for (int i = 0; i < slots.Count; i++)
            slots[i].infoPanel.SetActive(false);

        if (IsCardOperation(blessing.effectType))
            ExecuteCardOperation(blessing, () => StartCoroutine(DelayedExit()));
        else
        {
            ExecuteImmediateEffect(blessing);
            StartCoroutine(DelayedExit());
        }
    }

    private void OnLeave()
    {
        if (leaveButton != null)
            leaveButton.interactable = false;
        GameManager.Instance.ExitEncounter();
    }

    private IEnumerator DelayedExit()
    {
        yield return new WaitForSeconds(exitDelay);
        GameManager.Instance.ExitEncounter();
    }

    // ========== 悬停事件 ==========

    private void AddHoverEvents(Button button, UnityAction onEnter, UnityAction onExit)
    {
        EventTrigger trigger = button.gameObject.GetComponent<EventTrigger>();
        if (trigger == null)
            trigger = button.gameObject.AddComponent<EventTrigger>();

        var enterEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
        enterEntry.callback.AddListener(_ => onEnter());
        trigger.triggers.Add(enterEntry);

        var exitEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
        exitEntry.callback.AddListener(_ => onExit());
        trigger.triggers.Add(exitEntry);
    }

    // ========== 资源检查与扣除 ==========

    private bool CanAfford(BlessingEntry b)
    {
        var state = GameManager.Instance.WorldMapState;
        return b.costType switch
        {
            BlessingCostType.None => true,
            BlessingCostType.Health => state.currentHealth > b.costAmount,
            BlessingCostType.Gold => state.gold >= b.costAmount,
            BlessingCostType.String => state.stringCount >= b.costAmount,
            BlessingCostType.MaxHealth => state.maxHealth > b.costAmount,
            _ => false
        };
    }

    private void DeductCost(BlessingEntry b)
    {
        var state = GameManager.Instance.WorldMapState;
        switch (b.costType)
        {
            case BlessingCostType.Health: state.currentHealth -= b.costAmount; break;
            case BlessingCostType.Gold: state.gold -= b.costAmount; break;
            case BlessingCostType.String: state.stringCount -= b.costAmount; break;
            case BlessingCostType.MaxHealth:
                state.maxHealth -= b.costAmount;
                if (state.currentHealth > state.maxHealth)
                    state.currentHealth = state.maxHealth;
                break;
        }
    }

    // ========== 祝福授予 ==========

    private void GrantBlessing(StatueData statue, BlessingEntry b)
    {
        var blessings = GameManager.Instance.WorldMapState.activeBlessings;
        var existing = blessings.Find(ab => ab.blessingId == b.blessingId);

        if (existing != null)
            existing.count++;
        else
            blessings.Add(new ActiveBlessing(b.blessingId, statue.statueName, b.effectType, b.effectValue));
    }

    // ========== 即时效果（简单资源类） ==========

    private void ExecuteImmediateEffect(BlessingEntry b)
    {
        var state = GameManager.Instance.WorldMapState;
        switch (b.effectType)
        {
            case BlessingEffectType.GainString:
                state.stringCount += b.effectValue;
                break;
            case BlessingEffectType.GainGold:
                state.gold += b.effectValue;
                break;
            case BlessingEffectType.HealPercent:
                int heal = Mathf.CeilToInt(state.maxHealth * b.effectValue / 100f);
                state.currentHealth = Mathf.Min(state.currentHealth + heal, state.maxHealth);
                break;
            case BlessingEffectType.IncreaseMaxHealth:
                state.maxHealth += b.effectValue;
                state.currentHealth += b.effectValue;
                break;
        }
    }

    // ========== 卡牌操作 ==========

    private bool IsCardOperation(BlessingEffectType type)
    {
        return type == BlessingEffectType.DeleteCard
            || type == BlessingEffectType.GainRandomCard
            || type == BlessingEffectType.TransformCard
            || type == BlessingEffectType.UpgradeCards
            || type == BlessingEffectType.DeleteAndGiveCards;
    }

    private void ExecuteCardOperation(BlessingEntry b, Action onComplete)
    {
        switch (b.effectType)
        {
            case BlessingEffectType.DeleteCard:
                DoDeleteCard(onComplete);
                break;
            case BlessingEffectType.GainRandomCard:
                DoGainRandomCard(onComplete);
                break;
            case BlessingEffectType.TransformCard:
                DoTransformCard(onComplete);
                break;
            case BlessingEffectType.UpgradeCards:
                DoUpgradeCards(b.effectValue, onComplete);
                break;
            case BlessingEffectType.DeleteAndGiveCards:
                DoDeleteAndGiveCards(onComplete);
                break;
            default:
                onComplete?.Invoke();
                break;
        }
    }

    private void DoDeleteCard(Action onComplete)
    {
        var deck = GameManager.Instance.WorldMapState.currentDeck;
        deckViewer.OpenForSelection(deck, (entry) =>
        {
            deck.Remove(entry);
            onComplete?.Invoke();
        }, () => onComplete?.Invoke());
    }

    private void DoGainRandomCard(Action onComplete)
    {
        CardData newCard = GetRandomCardFromPool();
        if (newCard == null) { onComplete?.Invoke(); return; }

        GameManager.Instance.WorldMapState.currentDeck.Add(new DeckCardEntry(newCard, false));

        var previewCards = new List<Card> { new Card(newCard) };
        deckViewer.ShowPreviewCards(previewCards, cardPreviewDuration, onComplete);
    }

    private void DoTransformCard(Action onComplete)
    {
        var deck = GameManager.Instance.WorldMapState.currentDeck;
        deckViewer.OpenForSelection(deck, (entry) =>
        {
            deck.Remove(entry);

            CardData newCard = GetRandomCardFromPool();
            if (newCard != null)
            {
                deck.Add(new DeckCardEntry(newCard, false));
                var previewCards = new List<Card> { new Card(newCard) };
                deckViewer.ShowPreviewCards(previewCards, cardPreviewDuration, onComplete);
            }
            else
            {
                onComplete?.Invoke();
            }
        }, () => onComplete?.Invoke());
    }

    private void DoUpgradeCards(int remaining, Action onComplete)
    {
        var deck = GameManager.Instance.WorldMapState.currentDeck;
        var upgradeable = deck.FindAll(e => !e.IsUpgraded && e.CardData != null && e.CardData.UpgradeGrade != null);

        if (remaining <= 0 || upgradeable.Count == 0)
        {
            onComplete?.Invoke();
            return;
        }

        deckViewer.OpenForSelection(upgradeable, (entry) =>
        {
            entry.IsUpgraded = true;
            DoUpgradeCards(remaining - 1, onComplete);
        }, () => onComplete?.Invoke(), upgradePreview: true);
    }

    private void DoDeleteAndGiveCards(Action onComplete)
    {
        var deck = GameManager.Instance.WorldMapState.currentDeck;
        deckViewer.OpenForSelection(deck, (entry) =>
        {
            deck.Remove(entry);

            CardData card1 = GetRandomCardFromPool();
            CardData card2 = GetRandomCardFromPool();

            var previewCards = new List<Card>();
            if (card1 != null)
            {
                deck.Add(new DeckCardEntry(card1, false));
                previewCards.Add(new Card(card1));
            }
            if (card2 != null)
            {
                deck.Add(new DeckCardEntry(card2, false));
                previewCards.Add(new Card(card2));
            }

            if (previewCards.Count > 0)
                deckViewer.ShowPreviewCards(previewCards, cardPreviewDuration, onComplete);
            else
                onComplete?.Invoke();
        }, () => onComplete?.Invoke());
    }

    private CardData GetRandomCardFromPool()
    {
        if (rewardConfig == null || rewardConfig.lootCardPool == null || rewardConfig.lootCardPool.Count == 0)
            return null;
        return rewardConfig.lootCardPool[UnityEngine.Random.Range(0, rewardConfig.lootCardPool.Count)];
    }

    // ========== 特殊交互 ==========

    private void ApplySpecialInteractions(StatueData statue)
    {
        var state = GameManager.Instance.WorldMapState;

        if (statue.statueName == "\u8D6B\u62C9" && HasBlessingFromStatue("\u963F\u4F5B\u6D1B\u72C4\u5FBD"))
            state.currentHealth -= 4;

        if (statue.statueName == "\u54C8\u8FEA\u65AF" && HasBlessingFromStatue("\u963F\u65AF\u514B\u52D2\u5E87\u4FC4\u65AF"))
            state.currentHealth -= 5;
    }

    // ========== 查询辅助 ==========

    private bool HasBlessing(string blessingId)
    {
        var blessings = GameManager.Instance.WorldMapState.activeBlessings;
        return blessings.Exists(ab => ab.blessingId == blessingId);
    }

    private bool HasBlessingFromStatue(string statueName)
    {
        var blessings = GameManager.Instance.WorldMapState.activeBlessings;
        return blessings.Exists(ab => ab.statueName == statueName);
    }

    // ========== 显示辅助 ==========

    private string FormatCost(BlessingEntry b)
    {
        return b.costType switch
        {
            BlessingCostType.None => "\u514D\u8D39",
            BlessingCostType.Health => $"\u732E\u796D: {b.costAmount} \u6EF4\u8840",
            BlessingCostType.Gold => $"\u6D88\u8017: {b.costAmount} \u91D1\u5E01",
            BlessingCostType.String => $"\u6D88\u8017: {b.costAmount} \u6839\u7EBF",
            BlessingCostType.MaxHealth => $"\u732E\u796D: {b.costAmount} \u8840\u91CF\u4E0A\u9650",
            _ => ""
        };
    }

    private void Shuffle<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}
