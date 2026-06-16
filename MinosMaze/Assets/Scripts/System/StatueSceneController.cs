using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class StatueSceneController : MonoBehaviour
{
    [Header("所有神像数据")]
    [SerializeField] private List<StatueData> allStatues;

    [Header("UI - 3个雕像槽位")]
    [SerializeField] private List<StatueSlotUI> slots;

    [Header("离开按钮")]
    [SerializeField] private Button leaveButton;

    [Header("配置")]
    [SerializeField] private float exitDelay = 1f;

    private StatueData[] assignedStatues = new StatueData[3];
    private BlessingEntry[] assignedBlessings = new BlessingEntry[3];
    private bool optionChosen;

    void Start()
    {
        SelectStatues();
        for (int i = 0; i < 3; i++)
            SetupSlot(i);

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
        if (Random.value < 0.5f)
            selected = new List<StatueData> { males[0], males[1], females[0] };
        else
            selected = new List<StatueData> { females[0], females[1], males[0] };
        Shuffle(selected);

        for (int i = 0; i < 3; i++)
        {
            assignedStatues[i] = selected[i];
            var blessings = selected[i].blessings;
            assignedBlessings[i] = blessings[Random.Range(0, blessings.Count)];
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
        if (slot.costText != null)
            slot.costText.text = FormatCost(blessing);

        bool alreadyOwned = !blessing.repeatable && HasBlessing(blessing.blessingId);
        bool canAfford = CanAfford(blessing);
        bool available = !alreadyOwned && canAfford;

        if (slot.selectButton != null)
        {
            slot.selectButton.interactable = available;
            int idx = index;
            slot.selectButton.onClick.AddListener(() => OnSelectBlessing(idx));
        }
    }

    private void OnSelectBlessing(int index)
    {
        if (optionChosen) return;
        optionChosen = true;

        StatueData statue = assignedStatues[index];
        BlessingEntry blessing = assignedBlessings[index];

        DeductCost(blessing);
        GrantBlessing(statue, blessing);
        ExecuteImmediateEffect(blessing);
        ApplySpecialInteractions(statue);

        for (int i = 0; i < slots.Count; i++)
        {
            if (slots[i].selectButton != null)
                slots[i].selectButton.interactable = false;
        }

        StartCoroutine(DelayedExit());
    }

    private void OnLeave()
    {
        GameManager.Instance.ExitEncounter();
    }

    private IEnumerator DelayedExit()
    {
        yield return new WaitForSeconds(exitDelay);
        GameManager.Instance.ExitEncounter();
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

    // ========== 即时效果 ==========

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
            case BlessingEffectType.DeleteCard:
            case BlessingEffectType.GainRandomCard:
            case BlessingEffectType.TransformCard:
            case BlessingEffectType.UpgradeCards:
            case BlessingEffectType.DeleteAndGiveCards:
                Debug.Log($"[StatueScene] 卡牌操作效果 {b.effectType} 将在子任务3中实现");
                break;
        }
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
            int j = Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}
