using System.Collections.Generic;
using UnityEngine;

public enum EnemyActionType
{
    Attack,
    Move,
}

public enum TargetType
{
    Hero,
    Self,
}

[System.Serializable]
public class StatusEffectInfliction
{
    [field: SerializeField] public StatusEffectType EffectType { get; private set; }
    [field: SerializeField] public int StackCount { get; private set; } = 1;
    [field: SerializeField] public TargetType Target { get; private set; } = TargetType.Hero;
}

[System.Serializable]
public class EnemyAction
{
    [field: SerializeField] public EnemyActionType ActionType { get; private set; } = EnemyActionType.Attack;
    [field: SerializeField] public string ActionName { get; private set; }
    [field: SerializeField] public int BaseDamage { get; private set; }
    [field: SerializeField] public int HitCount { get; private set; } = 1;
    [field: SerializeField] public int MoveRange { get; private set; } = 1;
    [field: SerializeField] public List<StatusEffectInfliction> StatusEffects { get; private set; }
    [field: SerializeField] public int Weight { get; private set; } = 1;
    [field: SerializeField] public string Tag { get; private set; }
    [field: SerializeField] public int CooldownTurns { get; private set; }
}
