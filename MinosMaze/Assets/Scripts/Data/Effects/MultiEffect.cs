using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MultiEffect : Effect
{
    [field: SerializeField] public List<AutoTargetEffect> Effects { get; private set; }

    public override GameAction GetGameAction(List<CombatantView> targets, CombatantView caster)
    {
        return new MultiGA(Effects, targets, caster);
    }
}
