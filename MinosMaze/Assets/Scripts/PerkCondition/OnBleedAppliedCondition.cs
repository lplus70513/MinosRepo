using System;
using UnityEngine;

public class OnBleedAppliedCondition : PerkCondition
{
    [SerializeField] private int thresholdStacks = 1;

    public override bool SubConditionIsMet(GameAction gameAction)
    {
        return gameAction is AddStatusEffectGA ga
            && ga.StatusEffectType == StatusEffectType.BLEED
            && ga.StackCount >= thresholdStacks;
    }

    public override void SubscribeCondition(Action<GameAction> reaction)
    {
        ActionSystem.SubscribeReaction<AddStatusEffectGA>(reaction, reactionTiming);
    }

    public override void UnsubscribeCondition(Action<GameAction> reaction)
    {
        ActionSystem.UnsubscribeReaction<AddStatusEffectGA>(reaction, reactionTiming);
    }
}
