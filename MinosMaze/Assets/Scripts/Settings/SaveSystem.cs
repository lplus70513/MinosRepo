using System.Collections.Generic;
using System.IO;
using UnityEngine;

[System.Serializable]
public class SavedDeckEntry
{
    public string cardName;
    public bool isUpgraded;
}

[System.Serializable]
public class SaveData
{
    public int playerPosX;
    public int playerPosZ;
    public int maxHealth;
    public int currentHealth;
    public int gold;
    public int stringCount;
    public int floorLevel;
    public List<Vector2Int> clearedCells = new();
    public List<SavedDeckEntry> deck = new();
    public List<ActiveBlessing> activeBlessings = new();
    public List<CellLayoutEntry> cellLayout = new();
}

public static class SaveSystem
{
    private static string SavePath => Path.Combine(Application.persistentDataPath, "save.json");

    public static void Save(WorldMapState state)
    {
        SaveData data = new SaveData
        {
            playerPosX = state.playerPosX,
            playerPosZ = state.playerPosZ,
            maxHealth = state.maxHealth,
            currentHealth = state.currentHealth,
            gold = state.gold,
            stringCount = state.stringCount,
            floorLevel = state.floorLevel,
            clearedCells = new List<Vector2Int>(state.clearedCells),
            activeBlessings = new List<ActiveBlessing>(state.activeBlessings),
            cellLayout = new List<CellLayoutEntry>(state.cellLayout)
        };

        foreach (var entry in state.currentDeck)
        {
            if (entry == null || entry.CardData == null) continue;
            data.deck.Add(new SavedDeckEntry
            {
                cardName = entry.CardData.name,
                isUpgraded = entry.IsUpgraded
            });
        }

        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(SavePath, json);
        Debug.Log($"[SaveSystem] 存档已保存到: {SavePath}");
    }

    public static WorldMapState Load(CardDatabase cardDatabase)
    {
        if (!HasSave()) return null;

        string json = File.ReadAllText(SavePath);
        SaveData data = JsonUtility.FromJson<SaveData>(json);

        WorldMapState state = new WorldMapState
        {
            playerPosX = data.playerPosX,
            playerPosZ = data.playerPosZ,
            maxHealth = data.maxHealth,
            currentHealth = data.currentHealth,
            gold = data.gold,
            stringCount = data.stringCount,
            floorLevel = data.floorLevel,
            isNewGame = false,
            clearedCells = new List<Vector2Int>(data.clearedCells),
            activeBlessings = new List<ActiveBlessing>(data.activeBlessings),
            cellLayout = new List<CellLayoutEntry>(data.cellLayout)
        };

        state.currentDeck = new List<DeckCardEntry>();
        if (cardDatabase != null)
        {
            foreach (var saved in data.deck)
            {
                CardData card = cardDatabase.GetCardByName(saved.cardName);
                if (card != null)
                {
                    state.currentDeck.Add(new DeckCardEntry(card, saved.isUpgraded));
                }
                else
                {
                    Debug.LogWarning($"[SaveSystem] 读档时找不到卡牌: {saved.cardName}");
                }
            }
        }

        Debug.Log("[SaveSystem] 存档已加载");
        return state;
    }

    public static bool HasSave()
    {
        return File.Exists(SavePath);
    }

    public static void DeleteSave()
    {
        if (HasSave())
        {
            File.Delete(SavePath);
            Debug.Log("[SaveSystem] 存档已删除");
        }
    }
}
