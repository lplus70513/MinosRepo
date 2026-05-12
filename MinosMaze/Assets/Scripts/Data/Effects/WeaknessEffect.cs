using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaknessEffect : Effect
{
    [SerializeField] private int weaknessAmount;

    public override GameAction GetGameAction(List<CombatantView> targets)
    {
        return null;
    }
}
