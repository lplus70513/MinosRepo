using System.Collections.Generic;
using UnityEngine;

public class WeaknessCostEffect : Effect
{
    [SerializeField] private int costAmount = 1;

    public override GameAction GetGameAction(List<CombatantView> targets, CombatantView caster)
    {
        if (targets == null || targets.Count == 0) return null;

        bool targetHasWeakness = targets[0].HasStatusEffect(StatusEffectType.WEAKNESS);
        if (targetHasWeakness) return new GainCostGA(costAmount);

        return null;
    }
}
