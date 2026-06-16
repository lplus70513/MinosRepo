public class BonusCostGA : GameAction
{
    public int Amount { get; private set; }

    public BonusCostGA(int amount)
    {
        Amount = amount;
    }
}
