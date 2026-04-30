using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CostSystem : Singleton<CostSystem>
{
    [SerializeField] private CostUI costUI;

    private const int MAX_COST = 3;
    private int currentCost = MAX_COST;

    void OnEnable()
    {
        ActionSystem.AttachPerformer<SpendCostGA>(SpendCostPerformer);
        ActionSystem.AttachPerformer<RefillCostGA>(RefillCostPerformer);
        ActionSystem.SubscribeReaction<EnemyTurnGA>(EnemyTurnPostReaction, ReactionTiming.POST);
    }

    void OnDisable()
    {
        ActionSystem.DetachPerformer<SpendCostGA>();
        ActionSystem.DetachPerformer<RefillCostGA>();
        ActionSystem.UnsubscribeReaction<EnemyTurnGA>(EnemyTurnPostReaction, ReactionTiming.POST);
    }

    public bool HasEnoughCost(int cost)
    {
        return currentCost >= cost;
    }

    private IEnumerator SpendCostPerformer(SpendCostGA action)
    {
        currentCost -= action.Amount;
        costUI.UpdateCostText(currentCost);
        yield return null;
    }

    private IEnumerator RefillCostPerformer(RefillCostGA action)
    {
        currentCost = MAX_COST;
        costUI.UpdateCostText(currentCost);
        yield return null;
    }

    private void EnemyTurnPostReaction(EnemyTurnGA enemyTurnGA)
    {
        RefillCostGA refillCostGA = new();
        ActionSystem.Instance.AddReaction(refillCostGA);
    }
}