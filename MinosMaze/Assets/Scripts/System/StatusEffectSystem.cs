using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StatusEffectSystem : MonoBehaviour
{
    private void OnEnable()
    {
        ActionSystem.AttachPerformer<AddStatusEffectGA>(AddStatusEffectPerformer);
        ActionSystem.SubscribeReaction<EnemyTurnGA>(OnEnemyTurnPost, ReactionTiming.POST);
        ActionSystem.SubscribeReaction<DealDamageGA>(OnDealDamagePost, ReactionTiming.POST);
    }

    private void OnDisable()
    {
        ActionSystem.DetachPerformer<AddStatusEffectGA>();
        ActionSystem.UnsubscribeReaction<EnemyTurnGA>(OnEnemyTurnPost, ReactionTiming.POST);
        ActionSystem.UnsubscribeReaction<DealDamageGA>(OnDealDamagePost, ReactionTiming.POST);
    }

    private IEnumerator AddStatusEffectPerformer(AddStatusEffectGA addStatusEffectGA)
    {
        foreach (var target in addStatusEffectGA.Targets)
        {
            target.AddStatusEffect(addStatusEffectGA.StatusEffectType, addStatusEffectGA.StackCount);

            if (addStatusEffectGA.StatusEffectType == StatusEffectType.ROOT
                || addStatusEffectGA.StatusEffectType == StatusEffectType.STUN)
            {
                ActionSystem.Instance.CancelCurrentFlow();
            }

            yield return null;
        }
    }

    private void OnEnemyTurnPost(EnemyTurnGA enemyTurnGA)
    {
        var allCombatants = new List<CombatantView>();
        if (HeroSystem.Instance.HeroView != null)
            allCombatants.Add(HeroSystem.Instance.HeroView);
        if (EnemySystem.Instance.Enemies != null)
        {
            foreach (var enemy in EnemySystem.Instance.Enemies)
                if (enemy != null) allCombatants.Add(enemy);
        }

        foreach (var combatant in allCombatants)
        {
            int bleedStacks = combatant.GetStatusEffectStacks(StatusEffectType.BLEED);
            if (bleedStacks > 0)
            {
                DealDamageGA bleedGA = new(bleedStacks, new List<CombatantView> { combatant }, null);
                ActionSystem.Instance.AddReaction(bleedGA);
            }

            combatant.DecayTurnEndEffects();
        }
    }

    private void OnDealDamagePost(DealDamageGA dealDamageGA)
    {
        if (dealDamageGA.Caster == null) return;
        if (!dealDamageGA.Caster.HasStatusEffect(StatusEffectType.CHAIN_LIGHTNING)) return;

        var enemies = EnemySystem.Instance?.Enemies;
        if (enemies == null || enemies.Count == 0) return;

        var originalTargets = new HashSet<EnemyView>();
        foreach (var t in dealDamageGA.Targets)
        {
            if (t is EnemyView ev) originalTargets.Add(ev);
        }

        var chainTargets = new List<CombatantView>();

        foreach (var enemy in enemies)
        {
            if (enemy == null) continue;
            if (originalTargets.Contains(enemy)) continue;
            chainTargets.Add(enemy);
            if (chainTargets.Count >= 2) break;
        }

        if (chainTargets.Count < 2)
        {
            foreach (var enemy in enemies)
            {
                if (enemy == null) continue;
                if (chainTargets.Contains(enemy)) continue;
                chainTargets.Add(enemy);
                if (chainTargets.Count >= 2) break;
            }
        }

        foreach (var target in chainTargets)
        {
            DealDamageGA chainGA = new(4, new List<CombatantView> { target }, null);
            ActionSystem.Instance.AddReaction(chainGA);
        }
    }
}