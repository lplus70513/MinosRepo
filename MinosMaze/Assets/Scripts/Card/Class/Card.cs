using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

public class Card
{
    private readonly CardData data;

    public CardData CardData => data;
    public bool IsUpgraded { get; private set; }

    private CardGradeData Grade => IsUpgraded ? data.UpgradeGrade : data.BaseGrade;

    public string Name => Grade != null && !string.IsNullOrEmpty(Grade.Name)
        ? Grade.Name
        : data.name;

    public string Description => Grade != null && !string.IsNullOrEmpty(Grade.Description)
        ? Grade.Description
        : data.Description;

    public Sprite Image => data.Image;

    public Sprite Background => data.Background;

    public bool HasAttackRange => Grade != null ? Grade.HasAttackRange : data.HasAttackRange;

    public int AttackRange => Grade != null ? Grade.AttackRange : data.AttackRange;

    public Effect ManualTargetEffect => Grade != null ? Grade.ManualTargetEffect : data.ManualTargetEffect;

    public List<AutoTargetEffect> OtherEffects => Grade != null ? Grade.OtherEffects : data.OtherEffects;

    public int Cost { get; private set; }

    public bool IsInnate => Grade != null ? Grade.IsInnate : data.IsInnate;

    public bool IsExhaust => Grade != null ? Grade.IsExhaust : data.IsExhaust;

    public bool IsRetain => Grade != null ? Grade.IsRetain : data.IsRetain;

    public bool CanHitFlying => Grade != null ? Grade.CanHitFlying : data.CanHitFlying;

    public bool IsAttackCard => Grade != null ? Grade.IsAttackCard : data.IsAttackCard;

    public HexRangePattern AttackRangePattern => Grade?.AttackRangePattern ?? data.AttackRangePattern;

    public Card(CardData cardData, bool isUpgraded = false)
    {
        data = cardData;
        IsUpgraded = isUpgraded;
        Cost = Grade?.Cost ?? cardData.Cost;
    }

    public int GetEffectiveCost(CombatantView target)
    {
        if (target != null && target.HasStatusEffect(StatusEffectType.WEAKNESS) && OtherEffects != null)
        {
            foreach (var wrapper in OtherEffects)
            {
                if (wrapper.Effect is WeaknessCostEffect)
                    return 0;
            }
        }
        return Cost;
    }

    public void Upgrade()
    {
        if (IsUpgraded) return;
        IsUpgraded = true;
        if (data.UpgradeGrade != null)
            Cost = data.UpgradeGrade.Cost;
    }

    private List<DealDamageEffect> CollectDealDamageEffects()
    {
        var result = new List<DealDamageEffect>();
        if (ManualTargetEffect is DealDamageEffect dde)
            result.Add(dde);
        if (OtherEffects != null)
        {
            foreach (var wrapper in OtherEffects)
            {
                if (wrapper.Effect is DealDamageEffect otherDde)
                    result.Add(otherDde);
            }
        }
        return result;
    }

    public string GetLiveDescription(CombatantView caster, CombatantView target)
    {
        var damageEffects = CollectDealDamageEffects();
        if (damageEffects.Count == 0)
            return Description;

        string desc = Description;
        foreach (var effect in damageEffects)
        {
            int modified = DamageSystem.CalculateModifiedDamage(effect.DamageAmount, caster, target);
            if (modified != effect.DamageAmount)
            {
                string pattern = $@"(?<!\d){effect.DamageAmount}(?!\d)";
                desc = Regex.Replace(desc, pattern, modified.ToString());
            }
        }
        return desc;
    }
}