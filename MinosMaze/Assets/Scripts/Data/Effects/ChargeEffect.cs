using System.Collections.Generic;
using UnityEngine;

public class ChargeEffect : Effect
{
    [SerializeField] private int range = 3;
    [SerializeField] private int damageAmount = 8;

    public override GameAction GetGameAction(List<CombatantView> targets, CombatantView caster)
    {
        if (targets == null || targets.Count == 0) return null;
        return new ChargeGA(targets[0], caster, range, damageAmount);
    }
}
