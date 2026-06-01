using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyView : CombatantView
{
    public string DisplayName { get; private set; }

    public EnemyType EnemyType { get; private set; }

    public EnemyData SourceData { get; private set; }

    public Dictionary<string, int> ActionCooldowns { get; private set; } = new();

    public void Setup(EnemyData enemyData, int hexX, int hexZ)
    {
        HexCoordX = hexX;
        HexCoordZ = hexZ;
        SourceData = enemyData;
        DisplayName = enemyData.DisplayName;
        EnemyType = enemyData.Type;
        int actualHealth = enemyData.HealthRange.RandomValue;
        SetupBase(actualHealth, enemyData.Image);
    }

    public void DecrementCooldowns()
    {
        var tags = new List<string>(ActionCooldowns.Keys);
        foreach (var tag in tags)
        {
            ActionCooldowns[tag]--;
            if (ActionCooldowns[tag] <= 0)
                ActionCooldowns.Remove(tag);
        }
    }

    public void SetCooldown(string tag, int turns)
    {
        if (string.IsNullOrEmpty(tag) || turns <= 0) return;
        ActionCooldowns[tag] = turns;
    }

    public bool IsOnCooldown(string tag)
    {
        if (string.IsNullOrEmpty(tag)) return false;
        return ActionCooldowns.ContainsKey(tag);
    }

    public int GetMaxMoveRange()
    {
        if (SourceData == null || SourceData.ActionPool == null)
            return 0;

        int maxMove = 0;
        foreach (var action in SourceData.ActionPool)
        {
            if (action.ActionType == EnemyActionType.Move && !IsOnCooldown(action.Tag))
            {
                if (action.MoveRange > maxMove)
                    maxMove = action.MoveRange;
            }
        }
        return maxMove;
    }
}
