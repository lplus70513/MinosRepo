using System.Collections;
using UnityEngine;

public class ActionPointSystem : Singleton<ActionPointSystem>
{
    private const int MAX_ACTION_POINTS = 1;
    private int currentActionPoints = MAX_ACTION_POINTS;

    void OnEnable()
    {
        ActionSystem.AttachPerformer<SpendActionPointsGA>(SpendActionPointsPerformer);
        ActionSystem.AttachPerformer<GainActionPointsGA>(GainActionPointsPerformer);
        ActionSystem.SubscribeReaction<EnemyTurnGA>(EnemyTurnPostReaction, ReactionTiming.POST);
    }

    void OnDisable()
    {
        ActionSystem.DetachPerformer<SpendActionPointsGA>();
        ActionSystem.DetachPerformer<GainActionPointsGA>();
        ActionSystem.UnsubscribeReaction<EnemyTurnGA>(EnemyTurnPostReaction, ReactionTiming.POST);
    }

    void Start()
    {
        currentActionPoints = MAX_ACTION_POINTS;
    }

    public bool HasEnoughActionPoints(int cost)
    {
        return currentActionPoints >= cost;
    }

    private IEnumerator SpendActionPointsPerformer(SpendActionPointsGA action)
    {
        if (CardSystem.Instance.FreePlayRemaining > 0)
        {
            yield return null;
        }
        else
        {
            currentActionPoints -= action.Amount;
            Debug.Log($"[ActionPointSystem] 消耗行动点 -{action.Amount}，剩余: {currentActionPoints}");
            yield return null;
        }
    }

    private IEnumerator GainActionPointsPerformer(GainActionPointsGA action)
    {
        currentActionPoints += action.Amount;
        Debug.Log($"[ActionPointSystem] 获得行动点 +{action.Amount}，当前: {currentActionPoints}");
        yield return null;
    }

    private void EnemyTurnPostReaction(EnemyTurnGA enemyTurnGA)
    {
        currentActionPoints = MAX_ACTION_POINTS;
        Debug.Log($"[ActionPointSystem] 回合开始，行动点重置为 {MAX_ACTION_POINTS}");
    }
}
