using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DealDamageGA : GameAction, IHaveCaster
{
    public int Amount { get; set; }

    public int HitCount { get; set; }

    public List<CombatantView> Targets { get; set; }

    public CombatantView Caster { get; private set; }

    public int UnblockedAmount { get; set; }

    public DealDamageGA(int amount, int hitCount, List<CombatantView> targets, CombatantView caster)
    {
        Amount = amount;
        HitCount = hitCount;
        Targets = new(targets); 
        Caster = caster;
    }
}
