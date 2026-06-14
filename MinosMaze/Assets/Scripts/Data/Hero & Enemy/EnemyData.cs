using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Data/Enemy")]

public class EnemyData : ScriptableObject
{
    [field: SerializeField] public Sprite Image { get; private set; }
    [field: SerializeField] public string DisplayName { get; private set; }
    [field: SerializeField] public IntRange HealthRange { get; private set; } = new(10, 10);
    [field: SerializeField] public EnemyType Type { get; private set; } = EnemyType.Normal;
    [field: SerializeField] public List<EnemyAction> ActionPool { get; private set; }
    [field: SerializeField] public int ActionsPerTurn { get; private set; } = 2;
    [field: SerializeField] public bool UseModeSelection { get; private set; }
    [field: SerializeField] public List<EnemyMode> Modes { get; private set; }
    [field: SerializeField] public List<EnemyAction> StartOfTurnActions { get; private set; }
    [field: SerializeField] public bool CanRevive { get; private set; }
    [field: SerializeField] public int ReviveHealth { get; private set; } = 4;
    [field: SerializeField] public bool PersistArmor { get; private set; }
    [field: SerializeField] public List<StatusEffectInfliction> OnDealDamageEffects { get; private set; }
}
