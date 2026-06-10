using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GainActionPointsEffect : Effect
{
    [SerializeField] private int pointAmount = 1;

    public override GameAction GetGameAction(List<CombatantView> targets, CombatantView caster)
    {
        return new GainActionPointsGA(pointAmount);
    }
}
