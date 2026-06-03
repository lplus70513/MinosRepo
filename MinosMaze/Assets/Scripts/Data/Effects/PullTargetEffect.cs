using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PullTargetEffect : Effect
{
    public override GameAction GetGameAction(List<CombatantView> targets, CombatantView caster)
    {
        if (targets == null || targets.Count == 0) return null;
        return new PullTargetGA(targets[0], HeroSystem.Instance.HeroView);
    }
}
