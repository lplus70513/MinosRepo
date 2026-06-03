using System.Collections;

public class SingleDrawGA : GameAction
{
    public Card DrawnCard { get; private set; }

    public SingleDrawGA(Card card)
    {
        DrawnCard = card;
    }
}
