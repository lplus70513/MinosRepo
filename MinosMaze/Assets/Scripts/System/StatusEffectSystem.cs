using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StatusEffectSystem : MonoBehaviour
{
    private void OnEnable()
    {
        ActionSystem.AttachPerformer<AddStatusEffectGA>(AddStatusEffectPerformer);
        ActionSystem.AttachPerformer<DoubleStatusGA>(DoubleStatusPerformer);
        ActionSystem.AttachPerformer<ApplySpikedShieldGA>(ApplySpikedShieldPerformer);
        ActionSystem.SubscribeReaction<DealDamageGA>(OnDealDamagePost, ReactionTiming.POST);
        ActionSystem.SubscribeReaction<DealDamageGA>(OnThornsDamage, ReactionTiming.POST);
    }

    private void OnDisable()
    {
        ActionSystem.DetachPerformer<AddStatusEffectGA>();
        ActionSystem.DetachPerformer<DoubleStatusGA>();
        ActionSystem.DetachPerformer<ApplySpikedShieldGA>();
        ActionSystem.UnsubscribeReaction<DealDamageGA>(OnDealDamagePost, ReactionTiming.POST);
        ActionSystem.UnsubscribeReaction<DealDamageGA>(OnThornsDamage, ReactionTiming.POST);
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

    private IEnumerator DoubleStatusPerformer(DoubleStatusGA doubleStatusGA)
    {
        foreach (var target in doubleStatusGA.Targets)
        {
            int currentStacks = target.GetStatusEffectStacks(doubleStatusGA.StatusEffectType);
            if (currentStacks > 0)
            {
                int doubled = currentStacks * 2;
                target.SetStatusEffectStacks(doubleStatusGA.StatusEffectType, doubled);
                Debug.Log($"[StatusEffectSystem] {target.name} 的 {doubleStatusGA.StatusEffectType} 翻倍: {currentStacks} → {doubled}");
            }
            yield return null;
        }
    }

    private IEnumerator ApplySpikedShieldPerformer(ApplySpikedShieldGA ga)
    {
        HeroView hero = HeroSystem.Instance?.HeroView;
        if (hero != null)
        {
            hero.ThornsDamage = ga.DamageAmount;
            Debug.Log($"[StatusEffectSystem] 荆棘护盾激活，反伤 {ga.DamageAmount} 点");
        }
        yield return null;
    }

    private void OnThornsDamage(DealDamageGA dealDamageGA)
    {
        if (dealDamageGA.Caster is not EnemyView attacker) return;

        HeroView hero = HeroSystem.Instance?.HeroView;
        if (hero == null || hero.ThornsDamage <= 0) return;
        if (!dealDamageGA.Targets.Contains(hero)) return;

        DealDamageGA thornsGA = new(hero.ThornsDamage, 1, new List<CombatantView> { attacker }, hero);
        ActionSystem.Instance.AddReaction(thornsGA);
        Debug.Log($"[StatusEffectSystem] 荆棘反伤 {hero.ThornsDamage} 点给 {attacker.name}");
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
            DealDamageGA chainGA = new(4, 1, new List<CombatantView> { target }, null);
            ActionSystem.Instance.AddReaction(chainGA);
        }
    }
}