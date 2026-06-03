using System;
using UnityEngine;

public class OnUnblockedDamageCondition : PerkCondition
{
    public override bool SubConditionIsMet(GameAction gameAction)
    {
        if (gameAction is DealDamageGA dmg)
        {
            if (dmg.Caster == HeroSystem.Instance?.HeroView)
                return dmg.UnblockedAmount > 0;
        }
        return false;
    }

    public override void SubscribeCondition(Action<GameAction> reaction)
    {
        ActionSystem.SubscribeReaction<DealDamageGA>(reaction, reactionTiming);
    }

    public override void UnsubscribeCondition(Action<GameAction> reaction)
    {
        ActionSystem.UnsubscribeReaction<DealDamageGA>(reaction, reactionTiming);
    }
}
