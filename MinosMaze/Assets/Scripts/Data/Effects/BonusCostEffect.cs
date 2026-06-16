using System.Collections.Generic;
using UnityEngine;

public class BonusCostEffect : Effect
{
    [SerializeField] private int amount = 1;

    public override GameAction GetGameAction(List<CombatantView> targets, CombatantView caster)
    {
        return new BonusCostGA(amount);
    }
}
