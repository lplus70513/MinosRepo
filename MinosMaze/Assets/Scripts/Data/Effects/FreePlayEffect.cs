using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FreePlayEffect : Effect
{
    [SerializeField] private int freeCount = 1;

    public override GameAction GetGameAction(List<CombatantView> targets, CombatantView caster)
    {
        return new FreePlayGA(freeCount);
    }
}
