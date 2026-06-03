using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ReturnToDrawPileEffect : Effect
{
    public override GameAction GetGameAction(List<CombatantView> targets, CombatantView caster)
    {
        return new SelectCardFromHandGA(new ReturnToDrawPileGA(null));
    }
}
