using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class DrawCardEffects : Effect
{
    [SerializeField] private int drawAmount;

    public override GameAction GetGameAction(List<CombatantView> targets)
    {
        DrawCardsGA drawCardsGA = new(drawAmount);
        return drawCardsGA;
    }
}
