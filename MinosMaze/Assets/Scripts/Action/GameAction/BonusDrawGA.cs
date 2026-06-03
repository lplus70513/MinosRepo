public class BonusDrawGA : GameAction
{
    public int Amount { get; private set; }

    public BonusDrawGA(int amount)
    {
        Amount = amount;
    }
}
