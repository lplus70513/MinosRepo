using System.Collections;
using System.Collections.Generic;
using SerializeReferenceEditor;
using UnityEngine;

public class IfAttackedThisTurnEffect : Effect
{
    [field: SerializeReference, SR] public Effect InnerEffect { get; private set; }

    public override GameAction GetGameAction(List<CombatantView> targets, CombatantView caster)
    {
        if (targets == null || targets.Count == 0) return null;
        var hero = HeroSystem.Instance?.HeroView;
        if (hero == null) return null;

        foreach (var target in targets)
        {
            if (target is EnemyView ev && hero.AttackedThisTurn.Contains(ev))
                return InnerEffect?.GetGameAction(targets, caster);
        }
        return null;
    }
}
