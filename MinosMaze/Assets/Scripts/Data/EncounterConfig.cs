using UnityEngine;

// 遭遇标识数据包，由 WorldMap 在跳转战斗场景前写入 GameManager，
// 携带格子类型、当前层数、距中心难度等级，战斗场景据此从池中随机选配
[System.Serializable]
public class EncounterConfig
{
    public MapCellType cellType;
    public int floorLevel;
    /// <summary>距大地图中心的难度等级: 1=弱(外层) 2=中(中层) 3=强(内层)，0=未计算(向后兼容)</summary>
    public int difficultyLevel;
}
