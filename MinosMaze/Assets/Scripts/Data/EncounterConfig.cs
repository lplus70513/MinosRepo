using UnityEngine;

// 遭遇标识数据包，由 WorldMap 在跳转战斗场景前写入 GameManager，
// 仅携带格子类型和当前层数，战斗场景的 EncounterRandomizer 据此从池中随机选配
[System.Serializable]
public class EncounterConfig
{
    public MapCellType cellType;
    public int floorLevel;
}
