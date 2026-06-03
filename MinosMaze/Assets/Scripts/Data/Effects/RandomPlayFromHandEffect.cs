using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RandomPlayFromHandEffect : Effect
{
    [SerializeField] private int playCount = 1;

    public override GameAction GetGameAction(List<CombatantView> targets, CombatantView caster)
    {
        return new RandomPlayFromHandGA(playCount);
    }
}
