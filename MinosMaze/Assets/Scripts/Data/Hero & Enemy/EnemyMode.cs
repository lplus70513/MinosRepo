using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class EnemyMode
{
    [field: SerializeField] public string ModeName { get; private set; }
    [field: SerializeField] public List<EnemyAction> Actions { get; private set; }
    [field: SerializeField] public int Weight { get; private set; } = 1;
    [field: SerializeField] public string CooldownTag { get; private set; }
    [field: SerializeField] public int CooldownTurns { get; private set; }
    [field: SerializeField] public int MinTurnToUse { get; private set; }
}
