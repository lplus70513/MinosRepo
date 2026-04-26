using UnityEngine;

public class Card
{
    private readonly CardData data;

    public string Name => data.name;
    public string Description => data.Description;
    public Sprite Image => data.Image;
    public int Cost { get; private set; }

    public Card(CardData cardData)
    {
        data = cardData;
        Cost = cardData.Cost;
    }
}