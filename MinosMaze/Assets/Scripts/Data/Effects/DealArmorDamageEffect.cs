using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DealArmorDamageEffect : Effect
{
    public override GameAction GetGameAction(List<CombatantView> targets, CombatantView caster)
    {
        return new DealArmorDamageGA(targets, caster);
    }
}
