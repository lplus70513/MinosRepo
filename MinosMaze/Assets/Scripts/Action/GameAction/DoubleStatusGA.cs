using System.Collections.Generic;

public class DoubleStatusGA : GameAction
{
    public StatusEffectType StatusEffectType { get; private set; }
    public List<CombatantView> Targets { get; private set; }

    public DoubleStatusGA(StatusEffectType statusEffectType, List<CombatantView> targets)
    {
        StatusEffectType = statusEffectType;
        Targets = targets;
    }
}
