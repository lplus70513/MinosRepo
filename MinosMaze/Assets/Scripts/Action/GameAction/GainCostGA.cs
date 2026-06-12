public class GainCostGA : GameAction
{
    public int Amount { get; private set; }

    public GainCostGA(int amount)
    {
        Amount = amount;
    }
}
