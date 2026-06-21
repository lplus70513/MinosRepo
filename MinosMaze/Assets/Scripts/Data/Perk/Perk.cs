using UnityEngine;
using System.Collections.Generic;

public class Perk 
{
    public Sprite Image => data != null ? data.Image : null;

    private readonly PerkData data;

    private readonly PerkCondition condition;

    private readonly AutoTargetEffect effect;

    public Perk(PerkData perkData)
    {
        data = perkData;
        if (data != null)
        {
            condition = data.PerkCondition;
            effect = data.AutoTargetEffect;
        }
    }

    public void OnAdd()
    {
        if (condition == null)
        {
            Debug.LogWarning("[Perk] PerkData 为空或 PerkCondition 为空，跳过 OnAdd");
            return;
        }
        condition.SubscribeCondition(Reaction);
        SetupPassive();
    }

    public void OnRemove()
    {
        if (condition == null) return;
        condition.UnsubscribeCondition(Reaction);
        RemovePassive();
    }

    private void SetupPassive()
    {
        if (data.PersistArmor)
        {
            var hero = HeroSystem.Instance?.HeroView;
            if (hero != null) hero.PersistArmor = true;
        }
    }

    private void RemovePassive()
    {
        if (data.PersistArmor)
        {
            var hero = HeroSystem.Instance?.HeroView;
            if (hero != null) hero.PersistArmor = false;
        }
    }

    private void Reaction(GameAction gameAction)
    {
        if (condition == null || effect == null) return;
        if(condition.SubConditionIsMet(gameAction))
        {
            IHaveCaster haveCaster = gameAction as IHaveCaster;
            List<CombatantView> targets = new();
            if(data.UseActionAsTarget && haveCaster != null)
            {
                targets.Add(haveCaster.Caster);
            }
            if(data.UseAutoTarget)
            {
                targets.AddRange(effect.TargetMode.GetTargets());
            }
            GameAction perkEffectAction = effect.Effect.GetGameAction(targets, HeroSystem.Instance.HeroView);
            ActionSystem.Instance.AddReaction(perkEffectAction);
        }
    }
}
