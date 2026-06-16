using System.Collections.Generic;
using UnityEngine;

public class SpikedShieldEffect : Effect
{
    [SerializeField] private int damageAmount = 2;

    public override GameAction GetGameAction(List<CombatantView> targets, CombatantView caster)
    {
        return new ApplySpikedShieldGA(damageAmount);
    }
}
