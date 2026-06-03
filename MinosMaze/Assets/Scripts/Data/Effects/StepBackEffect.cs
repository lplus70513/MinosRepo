using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StepBackEffect : Effect
{
    public override GameAction GetGameAction(List<CombatantView> targets, CombatantView caster)
    {
        return new StepBackGA(HeroSystem.Instance.HeroView);
    }
}
