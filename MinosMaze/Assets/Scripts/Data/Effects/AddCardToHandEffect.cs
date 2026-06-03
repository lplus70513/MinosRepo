using System.Collections;
using System.Collections.Generic;
using SerializeReferenceEditor;
using UnityEngine;

public class AddCardToHandEffect : Effect
{
    [field: SerializeReference, SR] public CardData CardData { get; private set; }

    public override GameAction GetGameAction(List<CombatantView> targets, CombatantView caster)
    {
        return new AddCardToHandGA(CardData);
    }
}
