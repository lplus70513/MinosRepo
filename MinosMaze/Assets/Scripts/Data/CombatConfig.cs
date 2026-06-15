using System.Collections.Generic;
using UnityEngine;

// 单场遭遇的敌群配置，属于 EncounterRandomizer 的随机池中的一条。
// 配置名称、权重、适用层数范围、敌群数据、出生坐标、奖励
[System.Serializable]
public class CombatConfig
{
    public string configName;
    [Range(0, 100)] public int weight = 50;
    public int minFloor = 1;
    public int maxFloor = 999;
    public bool useCustomMap;
    public int mapRadius = 2;
    public List<SpecialCellConfig> specialCells;
    public List<EnemyData> enemyDatas;
    public List<Vector2Int> enemySpawnCoords;
    public Vector2Int heroSpawnCoord;
    public RewardConfig rewardConfig;
}
