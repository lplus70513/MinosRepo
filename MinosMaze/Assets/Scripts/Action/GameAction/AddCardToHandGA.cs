public class AddCardToHandGA : GameAction
{
    public CardData CardData { get; private set; }

    public AddCardToHandGA(CardData cardData)
    {
        CardData = cardData;
    }
}
