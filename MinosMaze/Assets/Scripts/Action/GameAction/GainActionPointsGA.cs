public class GainActionPointsGA : GameAction
{
    public int Amount { get; private set; }

    public GainActionPointsGA(int amount)
    {
        Amount = amount;
    }
}
