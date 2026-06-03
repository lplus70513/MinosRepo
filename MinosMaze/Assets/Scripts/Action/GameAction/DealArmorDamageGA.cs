using System.Collections.Generic;

public class DealArmorDamageGA : GameAction
{
    public List<CombatantView> Targets { get; private set; }
    public CombatantView Caster { get; private set; }

    public DealArmorDamageGA(List<CombatantView> targets, CombatantView caster)
    {
        Targets = targets;
        Caster = caster;
    }
}
