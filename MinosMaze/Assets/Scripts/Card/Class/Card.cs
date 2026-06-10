using UnityEngine;
using System.Collections.Generic;

public class Card
{
    private readonly CardData data;

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

    public int ActionPointCost { get; private set; }

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
        ActionPointCost = Grade?.ActionPointCost ?? cardData.ActionPointCost;
    }

    public void Upgrade()
    {
        if (IsUpgraded) return;
        IsUpgraded = true;
        if (data.UpgradeGrade != null)
        {
            Cost = data.UpgradeGrade.Cost;
            ActionPointCost = data.UpgradeGrade.ActionPointCost;
        }
    }
}