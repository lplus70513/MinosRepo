using UnityEngine;

[System.Serializable]
public class DeckCardEntry
{
    [field: SerializeField] public CardData CardData { get; private set; }

    [field: SerializeField] public bool IsUpgraded { get; set; }

    public DeckCardEntry(CardData cardData, bool isUpgraded = false)
    {
        CardData = cardData;
        IsUpgraded = isUpgraded;
    }
}
