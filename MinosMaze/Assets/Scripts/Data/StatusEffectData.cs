public static class StatusEffectData
{
    public static string GetName(StatusEffectType type)
    {
        return type switch
        {
            StatusEffectType.ARMOR => "护甲",
            StatusEffectType.BLEED => "流血",
            StatusEffectType.STRENGTH => "力量",
            StatusEffectType.WEAKNESS => "虚弱",
            StatusEffectType.VULNERABLE => "易伤",
            StatusEffectType.FORTIFY => "稳固",
            StatusEffectType.FRAGILE => "脆弱",
            StatusEffectType.AGILE => "敏捷",
            StatusEffectType.SLOW => "迟缓",
            StatusEffectType.CHAIN_LIGHTNING => "连锁闪电",
            StatusEffectType.ROOT => "定身",
            StatusEffectType.STUN => "眩晕",
            StatusEffectType.BLOCK => "格挡",
            StatusEffectType.FLYING => "飞行",
            _ => type.ToString(),
        };
    }

    public static string GetDescription(StatusEffectType type, int stackCount)
    {
        return type switch
        {
            StatusEffectType.ARMOR => $"吸收 {stackCount} 点伤害",
            StatusEffectType.BLEED => $"每回合结束时受到 {stackCount} 点伤害",
            StatusEffectType.STRENGTH => $"造成的伤害 +{stackCount}",
            StatusEffectType.WEAKNESS => "造成的伤害降低 50%",
            StatusEffectType.VULNERABLE => "受到的伤害增加 50%",
            StatusEffectType.FORTIFY => $"获得护甲时额外 +{stackCount} 层",
            StatusEffectType.FRAGILE => "获得的护甲减半",
            StatusEffectType.AGILE => $"每回合 +{stackCount} 行动点",
            StatusEffectType.SLOW => "每回合 -1 行动点",
            StatusEffectType.CHAIN_LIGHTNING => "造成伤害时对至多 2 个其他敌人造成 4 点伤害",
            StatusEffectType.ROOT => "无法移动，只能使用攻击卡牌",
            StatusEffectType.STUN => "无法进行任何操作",
            StatusEffectType.BLOCK => $"格挡 {stackCount} 次攻击伤害",
            StatusEffectType.FLYING => "受到的伤害降低 50%",
            _ => "",
        };
    }
}
