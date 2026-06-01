using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AddMovePointsEffect : Effect
{
    [SerializeField] private int movePointAmount = 1;

    public override GameAction GetGameAction(List<CombatantView> targets, CombatantView caster)
    {
        return new AddMovePointsGA(movePointAmount);
    }
}
