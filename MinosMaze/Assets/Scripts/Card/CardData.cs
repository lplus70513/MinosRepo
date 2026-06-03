using SerializeReferenceEditor;
using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "Data/Card")]

public class CardData : ScriptableObject
{
    [field: SerializeField] public string Description { get; private set; }

    [field: SerializeField] public int Cost { get; private set; }

    [field: SerializeField] public Sprite Image { get; private set; }

    [field: SerializeField] public Sprite Background { get; private set; }

    [field: SerializeField] public bool HasAttackRange { get; private set; } = false;

    [field: SerializeField] public int AttackRange { get; private set; } = 1;

    [field: SerializeReference, SR] public Effect ManualTargetEffect { get; private set; } = null;

    [field: SerializeField] public List<AutoTargetEffect> OtherEffects { get; private set; }

    [field: SerializeField] public bool IsInnate { get; private set; } = false;

    [field: SerializeField] public bool IsExhaust { get; private set; } = false;

    [field: SerializeField] public bool IsRetain { get; private set; } = false;

    [field: SerializeField] public bool CanHitFlying { get; private set; } = false;

    [field: SerializeField] public bool IsAttackCard { get; private set; } = false;
}