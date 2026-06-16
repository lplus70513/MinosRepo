public class ChargeGA : GameAction
{
    public CombatantView Target { get; private set; }
    public CombatantView Caster { get; private set; }
    public int Range { get; private set; }
    public int Damage { get; private set; }

    public ChargeGA(CombatantView target, CombatantView caster, int range, int damage)
    {
        Target = target;
        Caster = caster;
        Range = range;
        Damage = damage;
    }
}
