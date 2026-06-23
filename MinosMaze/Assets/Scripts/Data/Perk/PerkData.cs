using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SerializeReferenceEditor;

[CreateAssetMenu(menuName = "Data/Perk")]

public class PerkData : ScriptableObject
{
    [field: SerializeField] public string Name { get; private set; }

    [field: SerializeField, TextArea(2, 4)] public string Description { get; private set; }

    [field: SerializeField] public Sprite Image { get; private set; }

    [field: SerializeField, SerializeReference, SR] public PerkCondition PerkCondition { get; private set; }

    [field: SerializeField, SerializeReference, SR] public AutoTargetEffect AutoTargetEffect { get; private set; }

    [field: SerializeField] public bool UseAutoTarget { get; private set; } = true;

    [field: SerializeField] public bool UseActionAsTarget { get; private set; } = false;

    [field: SerializeField] public bool PersistArmor { get; private set; } = false;
}
