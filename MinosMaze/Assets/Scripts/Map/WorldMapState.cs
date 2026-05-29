using System.Collections.Generic;
using UnityEngine;

// 大地图可序列化状态，由 GameManager 持有并跨场景保存/恢复
[System.Serializable]
public class WorldMapState
{
    public int playerPosX;
    public int playerPosZ;
    public int remainingMovePoints;
    public int maxHealth;
    public int currentHealth;
    public List<Vector2Int> clearedCells = new();
    public List<CardData> currentDeck = new();
    public bool isNewGame = true;
}
