public class ApplySpikedShieldGA : GameAction
{
    public int DamageAmount { get; private set; }

    public ApplySpikedShieldGA(int damageAmount)
    {
        DamageAmount = damageAmount;
    }
}
