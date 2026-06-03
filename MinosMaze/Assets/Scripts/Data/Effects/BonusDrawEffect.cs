using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BonusDrawEffect : Effect
{
    [SerializeField] private int drawAmount = 1;

    public override GameAction GetGameAction(List<CombatantView> targets, CombatantView caster)
    {
        return new BonusDrawGA(drawAmount);
    }
}
