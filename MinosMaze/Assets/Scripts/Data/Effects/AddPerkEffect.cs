using System.Collections;
using System.Collections.Generic;
using SerializeReferenceEditor;
using UnityEngine;

public class AddPerkEffect : Effect
{
    [field: SerializeReference, SR] public PerkData PerkData { get; private set; }

    public override GameAction GetGameAction(List<CombatantView> targets, CombatantView caster)
    {
        return new AddPerkGA(PerkData);
    }
}
