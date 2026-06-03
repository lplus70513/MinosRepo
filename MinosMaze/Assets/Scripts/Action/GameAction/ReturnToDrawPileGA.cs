public class ReturnToDrawPileGA : GameAction
{
    public Card Card { get; private set; }

    public ReturnToDrawPileGA(Card card)
    {
        Card = card;
    }
}
