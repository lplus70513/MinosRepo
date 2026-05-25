using UnityEngine;

public class AttackHeroGA : GameAction, IHaveCaster
{
    public EnemyView Attacker { get; private set; }

    public EnemyAction Action { get; private set; }

    public CombatantView Caster { get; private set; }
    
    public AttackHeroGA(EnemyView attacker, EnemyAction action = null)
    {
        Attacker = attacker;
        Action = action;
        Caster = Attacker;
    }
}
