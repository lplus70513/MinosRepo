using System.Collections.Generic;

public class MultiGA : GameAction
{
    public List<AutoTargetEffect> Effects { get; private set; }
    public List<CombatantView> Targets { get; private set; }
    public CombatantView Caster { get; private set; }

    public MultiGA(List<AutoTargetEffect> effects, List<CombatantView> targets, CombatantView caster)
    {
        Effects = effects;
        Targets = targets;
        Caster = caster;
    }
}
