using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyView : CombatantView
{
    [SerializeField] private EnemyIntentView intentView;

    public string DisplayName { get; private set; }

    public EnemyType EnemyType { get; private set; }

    public EnemyData SourceData { get; private set; }

    public Dictionary<string, int> ActionCooldowns { get; private set; } = new();

    public List<EnemyAction> selectedActions = new();

    public List<EnemyIntentData> currentIntents = new();

    public int TurnCount { get; set; }

    public bool HasRevived { get; set; }

    public bool WillRevive { get; set; }

    public void Setup(EnemyData enemyData, int hexX, int hexZ)
    {
        HexCoordX = hexX;
        HexCoordZ = hexZ;
        SourceData = enemyData;
        DisplayName = enemyData.DisplayName;
        EnemyType = enemyData.Type;
        PersistArmor = enemyData.PersistArmor;
        int actualHealth = enemyData.HealthRange.RandomValue;
        SetupBase(actualHealth, enemyData.Image);
    }

    public void ShowIntents()
    {
        if (intentView != null && currentIntents.Count > 0)
            intentView.Show(currentIntents);
    }

    public void TransitionIntents(List<EnemyIntentData> intents)
    {
        currentIntents = intents;
        if (intentView != null)
        {
            intentView.TransitionTo(intents);
        }
        else
        {
            Debug.LogWarning($"[EnemyView] {name} intentView 未绑定，无法显示意图！");
        }
    }

    public void HideIntents()
    {
        if (intentView != null)
            intentView.Hide();
    }

    protected override void BillboardUI()
    {
        base.BillboardUI();
        if (intentView != null && camera3D != null)
            intentView.transform.rotation = camera3D.transform.rotation;
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

    public void SetCooldown(EnemyAction action)
    {
        if (action == null || action.CooldownTurns <= 0) return;
        string key = GetCooldownKeyForAction(action);
        if (string.IsNullOrEmpty(key)) return;
        ActionCooldowns[key] = action.CooldownTurns;
    }

    public void SetModeCooldown(EnemyMode mode)
    {
        if (mode == null || mode.CooldownTurns <= 0) return;
        string key = !string.IsNullOrEmpty(mode.CooldownTag) ? mode.CooldownTag : mode.ModeName;
        if (string.IsNullOrEmpty(key)) return;
        ActionCooldowns[key] = mode.CooldownTurns;
    }

    public bool IsOnCooldown(EnemyAction action)
    {
        if (action == null) return false;
        string key = GetCooldownKeyForAction(action);
        if (string.IsNullOrEmpty(key)) return false;
        return ActionCooldowns.ContainsKey(key);
    }

    public bool IsOnCooldown(EnemyMode mode)
    {
        if (mode == null) return false;
        string key = !string.IsNullOrEmpty(mode.CooldownTag) ? mode.CooldownTag : mode.ModeName;
        if (string.IsNullOrEmpty(key)) return false;
        return ActionCooldowns.ContainsKey(key);
    }

    private static string GetCooldownKeyForAction(EnemyAction action)
    {
        if (!string.IsNullOrEmpty(action.Tag)) return action.Tag;
        if (action.CooldownTurns > 0) return action.ActionName ?? action.GetHashCode().ToString();
        return null;
    }

    public int GetMaxMoveRange()
    {
        if (SourceData == null || SourceData.ActionPool == null)
            return 0;

        int maxMove = 0;
        foreach (var action in SourceData.ActionPool)
        {
            if (action.ActionType == EnemyActionType.Move && !IsOnCooldown(action))
            {
                if (action.MoveRange > maxMove)
                    maxMove = action.MoveRange;
            }
        }
        return maxMove;
    }
}
