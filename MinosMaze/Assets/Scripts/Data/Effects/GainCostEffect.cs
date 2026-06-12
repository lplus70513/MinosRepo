using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GainCostEffect : Effect
{
    [SerializeField] private int costAmount = 1;

    public override GameAction GetGameAction(List<CombatantView> targets, CombatantView caster)
    {
        return new GainCostGA(costAmount);
    }
}
