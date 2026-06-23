using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct CellLayoutEntry
{
    public int x;
    public int z;
    public MapCellType type;

    public CellLayoutEntry(int x, int z, MapCellType type)
    {
        this.x = x;
        this.z = z;
        this.type = type;
    }
}

// 大地图可序列化状态，由 GameManager 持有并跨场景保存/恢复
[System.Serializable]
public class WorldMapState
{
    public int playerPosX;
    public int playerPosZ;
    public int maxHealth;
    public int currentHealth;
    public int gold;
    public int stringCount;
    public List<Vector2Int> clearedCells = new();
    public List<DeckCardEntry> currentDeck = new();
    public List<ActiveBlessing> activeBlessings = new();
    public List<CellLayoutEntry> cellLayout = new();
    public bool isNewGame = true;
    public int floorLevel = 1;
    public int stepDifficulty = 0;

    // 浅拷贝：值字段直接复制，List 复制独立容器（元素引用共享）。
    // 遭遇场景对列表均为 Add 新元素，不会原地修改已有元素，故浅拷贝足以隔离后续追加。
    public WorldMapState Clone()
    {
        return new WorldMapState
        {
            playerPosX = playerPosX,
            playerPosZ = playerPosZ,
            maxHealth = maxHealth,
            currentHealth = currentHealth,
            gold = gold,
            stringCount = stringCount,
            clearedCells = new List<Vector2Int>(clearedCells),
            currentDeck = new List<DeckCardEntry>(currentDeck),
            activeBlessings = new List<ActiveBlessing>(activeBlessings),
            cellLayout = new List<CellLayoutEntry>(cellLayout),
            isNewGame = isNewGame,
            floorLevel = floorLevel,
            stepDifficulty = stepDifficulty
        };
    }
}
