using UnityEngine;

public class PlayCardGA : GameAction
{
    public EnemyView ManualTarget { get; private set; }

    public Card Card { get; set; }

    public bool IsFreePlay { get; private set; }

    public PlayCardGA(Card card, bool isFreePlay = false)
    {
        Card = card;
        ManualTarget = null;
        IsFreePlay = isFreePlay;
    }

    public PlayCardGA(Card card, EnemyView target, bool isFreePlay = false)
    {
        Card = card;
        ManualTarget = target;
        IsFreePlay = isFreePlay;
    }
}
