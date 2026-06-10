public class SpendActionPointsGA : GameAction
{
    public int Amount { get; private set; }

    public SpendActionPointsGA(int amount)
    {
        Amount = amount;
    }
}
