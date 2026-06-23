using UnityEngine;

/// <summary>
/// 难度系统（纯静态工具类）。
/// 敌人 HP / 伤害随玩家在世界地图上的累计步数指数增长。
/// </summary>
public static class DifficultySystem
{
    /// <summary>每步增长倍率：1.018（+1.8%）</summary>
    public const float ScalePerStep = 0.018f;

    /// <summary>获取当前难度步数（从 WorldMapState 读取）</summary>
    public static int CurrentDifficulty
    {
        get
        {
            var gm = GameManager.Instance;
            return gm != null ? gm.WorldMapState.stepDifficulty : 0;
        }
    }

    /// <summary>获取当前难度倍率：pow(1.018, stepDifficulty)</summary>
    public static float GetMultiplier()
    {
        int steps = CurrentDifficulty;
        if (steps <= 0) return 1f;
        return Mathf.Pow(1f + ScalePerStep, steps);
    }

    /// <summary>将基础生命值按难度缩放，四舍五入</summary>
    public static int ScaleHP(int baseHP)
    {
        return Mathf.RoundToInt(baseHP * GetMultiplier());
    }

    /// <summary>将基础伤害值按难度缩放，四舍五入</summary>
    public static int ScaleDamage(int baseDamage)
    {
        return Mathf.RoundToInt(baseDamage * GetMultiplier());
    }

    /// <summary>获取显示用字符串，例如 "0 (x100%)"、"20 (x143%)"</summary>
    public static string GetDisplayString()
    {
        int steps = CurrentDifficulty;
        float pct = GetMultiplier() * 100f;
        return $"{steps} (x{pct:F0}%)";
    }
}
