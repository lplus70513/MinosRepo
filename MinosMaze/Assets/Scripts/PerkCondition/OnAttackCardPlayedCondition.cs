using System;
using UnityEngine;

public class OnAttackCardPlayedCondition : PerkCondition
{
    [SerializeField] private int thresholdCount = 1;

    private int attackCardCounter = 0;

    public override bool SubConditionIsMet(GameAction gameAction)
    {
        if (gameAction is not PlayCardGA playGA)
            return false;

        if (!playGA.Card.IsAttackCard)
            return false;

        attackCardCounter++;
        if (attackCardCounter >= thresholdCount)
        {
            attackCardCounter = 0;
            return true;
        }
        return false;
    }

    public override void SubscribeCondition(Action<GameAction> reaction)
    {
        ActionSystem.SubscribeReaction<PlayCardGA>(reaction, reactionTiming);
    }

    public override void UnsubscribeCondition(Action<GameAction> reaction)
    {
        ActionSystem.UnsubscribeReaction<PlayCardGA>(reaction, reactionTiming);
    }
}
