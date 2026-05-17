using UnityEngine;
using System.Collections.Generic;

public class Card
{
    private readonly CardData data;

    public string Name => data.name;

    public string Description => data.Description;

    public Sprite Image => data.Image;

    public Sprite Background => data.Background;

    public bool HasAttackRange => data.HasAttackRange;

    public int AttackRange => data.AttackRange;

    public Effect ManualTargetEffect => data.ManualTargetEffect;

    public List<AutoTargetEffect> OtherEffects => data.OtherEffects;

    public int Cost { get; private set; }

    public Card(CardData cardData)
    {
        data = cardData;
        Cost = cardData.Cost;
    }
}