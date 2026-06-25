using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

[System.Serializable]
public class LeaderboardEntry
{
    public string playerName;
    public string timestamp;
    public int score;
    public int floorLevel;
    public int enemiesKilled;
    public int goldCollected;
    public int turnsTaken;
    public float elapsedSeconds;
    public bool isWin;
}

[System.Serializable]
public class LeaderboardData
{
    public List<LeaderboardEntry> entries = new();
}

public class LeaderboardSystem : Singleton<LeaderboardSystem>
{
    public List<LeaderboardEntry> Entries { get; private set; } = new();
    private const int MaxEntries = 100;

    private static string SavePath => Path.Combine(Application.persistentDataPath, "leaderboard.json");

    protected override void Awake()
    {
        base.Awake();
        DontDestroyOnLoad(gameObject);
        LoadLeaderboard();
    }

    public void AddEntry(RunStatistics stats)
    {
        var entry = new LeaderboardEntry
        {
            playerName = stats.PlayerName,
            timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm"),
            score = stats.CalculateScore(),
            floorLevel = stats.MaxFloorReached,
            enemiesKilled = stats.EnemiesKilled,
            goldCollected = stats.GoldCollected,
            turnsTaken = stats.TurnsTaken,
            elapsedSeconds = stats.ElapsedTime,
            isWin = stats.IsWin
        };

        Entries.Add(entry);
        Entries.Sort((a, b) => b.score.CompareTo(a.score));

        if (Entries.Count > MaxEntries)
            Entries.RemoveRange(MaxEntries, Entries.Count - MaxEntries);

        SaveLeaderboard();
    }

    public void LoadLeaderboard()
    {
        if (!File.Exists(SavePath))
        {
            Entries = new List<LeaderboardEntry>();
            return;
        }

        try
        {
            string json = File.ReadAllText(SavePath);
            LeaderboardData data = JsonUtility.FromJson<LeaderboardData>(json);
            Entries = data?.entries ?? new List<LeaderboardEntry>();
        }
        catch (Exception e)
        {
            Debug.LogWarning("[LeaderboardSystem] 读取排行榜文件失败: " + e.Message);
            Entries = new List<LeaderboardEntry>();
        }
    }

    public void SaveLeaderboard()
    {
        LeaderboardData data = new LeaderboardData { entries = Entries };
        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(SavePath, json);
    }

    public void ClearLeaderboard()
    {
        Entries.Clear();
        if (File.Exists(SavePath))
            File.Delete(SavePath);
    }
}
