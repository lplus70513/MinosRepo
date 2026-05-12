using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BleedEffect : Effect
{
    [SerializeField] private int bleedAmount;

    public override GameAction GetGameAction(List<CombatantView> targets)
    {
        return null;
    }
}
