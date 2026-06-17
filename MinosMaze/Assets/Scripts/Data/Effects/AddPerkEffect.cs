using System.Collections.Generic;
using UnityEngine;

public class AddPerkEffect : Effect
{
    [field: SerializeField] public PerkData PerkData { get; private set; }

    public override GameAction GetGameAction(List<CombatantView> targets, CombatantView caster)
    {
        return new AddPerkGA(PerkData);
    }
}
