using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Data/BattleMap")]
public class BattleMapDataSO : ScriptableObject
{
    [field: SerializeField] public int MapRadius { get; private set; } = 2;
    [field: SerializeField] public List<SpecialCellConfig> SpecialCells { get; private set; }
}
