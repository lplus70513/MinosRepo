using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlessingSystem : Singleton<BlessingSystem>
{
    void OnEnable()
    {
        ActionSystem.SubscribeReaction<EnemyTurnGA>(OnEnemyTurnPost, ReactionTiming.POST);
        ActionSystem.SubscribeReaction<RefillCostGA>(OnRefillCostPost, ReactionTiming.POST);
    }

    void OnDisable()
    {
        ActionSystem.UnsubscribeReaction<EnemyTurnGA>(OnEnemyTurnPost, ReactionTiming.POST);
        ActionSystem.UnsubscribeReaction<RefillCostGA>(OnRefillCostPost, ReactionTiming.POST);
    }

    public IEnumerator ApplyBattleStartBlessingsCoroutine()
    {
        var blessings = GameManager.Instance?.WorldMapState?.activeBlessings;
        if (blessings == null || blessings.Count == 0) yield break;

        var hero = HeroSystem.Instance?.HeroView;
        if (hero == null) yield break;

        int totalMovePoints = 0;
        int totalActionPoints = 0;

        foreach (var b in blessings)
        {
            int stacks = b.effectValue * b.count;
            switch (b.effectType)
            {
                case BlessingEffectType.GainStrengthOnBattleStart:
                    hero.AddStatusEffect(StatusEffectType.STRENGTH, stacks);
                    break;
                case BlessingEffectType.DamageAllEnemiesOnBattleStart:
                    yield return DamageAllEnemiesCoroutine(b.effectValue * b.count);
                    break;
                case BlessingEffectType.GainStrengthLoseFortify:
                    hero.AddStatusEffect(StatusEffectType.STRENGTH, b.effectValue * b.count);
                    hero.AddStatusEffect(StatusEffectType.FORTIFY, -(1 * b.count));
                    break;
                case BlessingEffectType.GainFortifyOnBattleStart:
                    hero.AddStatusEffect(StatusEffectType.FORTIFY, stacks);
                    break;
                case BlessingEffectType.GainLightningChain:
                    hero.AddStatusEffect(StatusEffectType.CHAIN_LIGHTNING, stacks);
                    break;
                case BlessingEffectType.GainBlockPerTurn:
                    hero.AddStatusEffect(StatusEffectType.ARMOR, stacks);
                    break;
                case BlessingEffectType.GainMovePointPerTurn:
                    totalMovePoints += stacks;
                    break;
                case BlessingEffectType.GainActionPointPerTurn:
                    totalActionPoints += stacks;
                    break;
            }
        }

        if (totalMovePoints > 0 && PlayerMovementSystem.Instance != null)
            PlayerMovementSystem.Instance.AddMovementPoints(totalMovePoints);
        if (totalActionPoints > 0 && CostSystem.Instance != null)
            CostSystem.Instance.AddCost(totalActionPoints);
    }

    private IEnumerator DamageAllEnemiesCoroutine(int amount)
    {
        var enemies = EnemySystem.Instance?.Enemies;
        if (enemies == null || enemies.Count == 0) yield break;

        var hero = HeroSystem.Instance?.HeroView;
        var targets = new List<CombatantView>(enemies);
        DealDamageGA damageGA = new(amount, 1, targets, hero);

        bool finished = false;
        ActionSystem.Instance.Perform(damageGA, () => finished = true);
        yield return new WaitUntil(() => finished);
    }

    private void OnEnemyTurnPost(EnemyTurnGA ga)
    {
        var blessings = GameManager.Instance?.WorldMapState?.activeBlessings;
        if (blessings == null || blessings.Count == 0) return;

        var hero = HeroSystem.Instance?.HeroView;
        if (hero == null) return;

        int totalMovePoints = 0;

        foreach (var b in blessings)
        {
            switch (b.effectType)
            {
                case BlessingEffectType.GainBlockPerTurn:
                    hero.AddStatusEffect(StatusEffectType.ARMOR, b.effectValue * b.count);
                    break;
                case BlessingEffectType.GainMovePointPerTurn:
                    totalMovePoints += b.effectValue * b.count;
                    break;
            }
        }

        if (totalMovePoints > 0)
            ActionSystem.Instance.AddReaction(new AddMovePointsGA(totalMovePoints));
    }

    private void OnRefillCostPost(RefillCostGA ga)
    {
        var blessings = GameManager.Instance?.WorldMapState?.activeBlessings;
        if (blessings == null || blessings.Count == 0) return;

        int totalActionPoints = 0;

        foreach (var b in blessings)
        {
            if (b.effectType == BlessingEffectType.GainActionPointPerTurn)
                totalActionPoints += b.effectValue * b.count;
        }

        if (totalActionPoints > 0)
            ActionSystem.Instance.AddReaction(new GainCostGA(totalActionPoints));
    }

    private void DamageAllEnemies(int amount)
    {
        var enemies = EnemySystem.Instance?.Enemies;
        if (enemies == null || enemies.Count == 0) return;

        var hero = HeroSystem.Instance?.HeroView;
        var targets = new List<CombatantView>(enemies);
        DealDamageGA damageGA = new(amount, 1, targets, hero);
        ActionSystem.Instance.Perform(damageGA);
    }

    public bool TryResurrect(HeroView hero, System.Action onResurrect = null)
    {
        var blessings = GameManager.Instance?.WorldMapState?.activeBlessings;
        if (blessings == null) return false;

        var resurrect = blessings.Find(b => b.effectType == BlessingEffectType.Resurrection);
        if (resurrect == null) return false;

        int reviveHP = Mathf.CeilToInt(hero.MaxHealth * 0.3f);
        hero.SetCurrentHealth(reviveHP);
        blessings.Remove(resurrect);

        onResurrect?.Invoke();

        return true;
    }
}
