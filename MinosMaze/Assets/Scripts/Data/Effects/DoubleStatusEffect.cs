using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DoubleStatusEffect : Effect
{
    [SerializeField] private StatusEffectType statusEffectType;

    public override GameAction GetGameAction(List<CombatantView> targets, CombatantView caster)
    {
        return new DoubleStatusGA(statusEffectType, targets);
    }
}
