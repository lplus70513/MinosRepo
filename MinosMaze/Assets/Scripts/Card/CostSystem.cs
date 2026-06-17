using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CostSystem : Singleton<CostSystem>
{
    [SerializeField] private CostUI costUI;

    private const int MAX_COST = 3;
    private int currentCost = MAX_COST;
    private int bonusCostNextTurn = 0;

    void OnEnable()
    {
        ActionSystem.AttachPerformer<SpendCostGA>(SpendCostPerformer);
        ActionSystem.AttachPerformer<RefillCostGA>(RefillCostPerformer);
        ActionSystem.AttachPerformer<GainCostGA>(GainCostPerformer);
        ActionSystem.AttachPerformer<BonusCostGA>(BonusCostPerformer);
        ActionSystem.SubscribeReaction<EnemyTurnGA>(EnemyTurnPostReaction, ReactionTiming.POST);
    }

    void OnDisable()
    {
        ActionSystem.DetachPerformer<SpendCostGA>();
        ActionSystem.DetachPerformer<RefillCostGA>();
        ActionSystem.DetachPerformer<GainCostGA>();
        ActionSystem.DetachPerformer<BonusCostGA>();
        ActionSystem.UnsubscribeReaction<EnemyTurnGA>(EnemyTurnPostReaction, ReactionTiming.POST);
    }

    public bool HasEnoughCost(int cost)
    {
        return currentCost >= cost;
    }

    public void AddCost(int amount)
    {
        currentCost += amount;
        if (costUI != null) costUI.UpdateCostText(currentCost);
    }

    private IEnumerator SpendCostPerformer(SpendCostGA action)
    {
        if (CardSystem.Instance.FreePlayRemaining > 0)
        {
            CardSystem.Instance.ConsumeFreePlay();
            Debug.Log($"[CostSystem] 免费出牌，跳过消耗 {action.Amount} 点行动力");
            yield return null;
        }
        else
        {
            currentCost -= action.Amount;
            costUI.UpdateCostText(currentCost);
            yield return null;
        }
    }

    private IEnumerator RefillCostPerformer(RefillCostGA action)
    {
        currentCost = MAX_COST;

        var hero = HeroSystem.Instance?.HeroView;
        if (hero != null)
        {
            currentCost += hero.GetStatusEffectStacks(StatusEffectType.AGILE);
            if (hero.HasStatusEffect(StatusEffectType.SLOW))
                currentCost -= 1;
        }

        currentCost += bonusCostNextTurn;
        Debug.Log($"[CostSystem] 下回合额外行动力 +{bonusCostNextTurn}");
        bonusCostNextTurn = 0;

        if (currentCost < 0) currentCost = 0;
        costUI.UpdateCostText(currentCost);
        yield return null;
    }

    private void EnemyTurnPostReaction(EnemyTurnGA enemyTurnGA)
    {
        RefillCostGA refillCostGA = new();
        ActionSystem.Instance.AddReaction(refillCostGA);
    }

    private IEnumerator GainCostPerformer(GainCostGA action)
    {
        currentCost += action.Amount;
        costUI.UpdateCostText(currentCost);
        Debug.Log($"[CostSystem] 获得行动力 +{action.Amount}，当前: {currentCost}");
        yield return null;
    }

    private IEnumerator BonusCostPerformer(BonusCostGA ga)
    {
        bonusCostNextTurn += ga.Amount;
        Debug.Log($"[CostSystem] 下回合额外行动力 +{ga.Amount}，累计: {bonusCostNextTurn}");
        yield return null;
    }
}