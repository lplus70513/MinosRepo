using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using DG.Tweening;

public class EnemySystem : Singleton<EnemySystem>
{
    [SerializeField] private EnemyBoardView enemyBoardView;

    public List<EnemyView> Enemies => enemyBoardView.EnemyViews;

    public EnemyView GetEnemyAt(int x, int z)
    {
        return Enemies.Find(e => e.HexCoordX == x && e.HexCoordZ == z);
    }

    void OnEnable()
    {
        Debug.Log($"[EnemySystem] OnEnable — 注册 AttachPerformer/SubscribeReaction, scene={gameObject.scene.name}");
        ActionSystem.AttachPerformer<EnemyTurnGA>(EnemyTurnPerformer);
        ActionSystem.AttachPerformer<AttackHeroGA>(AttackHeroPerformer);
        ActionSystem.AttachPerformer<KillEnemyGA>(KillEnemyPerformer);
        ActionSystem.SubscribeReaction<EnemyTurnGA>(EnemyTurnPreReaction, ReactionTiming.PRE);
        ActionSystem.SubscribeReaction<EnemyTurnGA>(EnemyTurnPostReaction, ReactionTiming.POST);
        ActionSystem.SubscribeReaction<DealDamageGA>(OnEnemyDealDamage, ReactionTiming.POST);
    }

    void OnDisable()
    {
        Debug.Log($"[EnemySystem] OnDisable — 注销 AttachPerformer/SubscribeReaction, scene={gameObject.scene.name}");
        ActionSystem.DetachPerformer<EnemyTurnGA>();
        ActionSystem.DetachPerformer<AttackHeroGA>();
        ActionSystem.DetachPerformer<KillEnemyGA>();
        ActionSystem.UnsubscribeReaction<EnemyTurnGA>(EnemyTurnPreReaction, ReactionTiming.PRE);
        ActionSystem.UnsubscribeReaction<EnemyTurnGA>(EnemyTurnPostReaction, ReactionTiming.POST);
        ActionSystem.UnsubscribeReaction<DealDamageGA>(OnEnemyDealDamage, ReactionTiming.POST);
    }

    public void Setup(List<EnemyData> enemyDatas, List<Vector2Int> spawnCoords)
    {
        for (int i = 0; i < enemyDatas.Count; i++)
        {
            if (enemyDatas[i] == null)
            {
                Debug.LogError($"[EnemySystem] enemyDatas[{i}] 为 null，跳过");
                continue;
            }
            Vector3 pos = Vector3.zero;
            Quaternion rot = Quaternion.identity;
            int hexX = 0, hexZ = 0;
            if (i < spawnCoords.Count)
            {
                Vector2Int coord = spawnCoords[i];
                hexX = coord.x;
                hexZ = coord.y;
                pos = HexGrid.GetStandingPoint(coord.x, coord.y);
            }
            enemyBoardView.AddEnemy(enemyDatas[i], pos, rot, hexX, hexZ);
        }
    }

    private IEnumerator EnemyTurnPerformer(EnemyTurnGA enemyTurnGA)
    {
        yield return null;
    }

    private void EnemyTurnPreReaction(EnemyTurnGA enemyTurnGA)
    {
        HashSet<(int, int)> reservedCells = new();
        foreach (var enemy in enemyBoardView.EnemyViews)
        {
            enemy.TurnCount++;

            // 复活检查
            if (enemy.CurrentHealth <= 0 && enemy.WillRevive)
            {
                enemy.SetCurrentHealth(enemy.SourceData.ReviveHealth);
                enemy.HasRevived = true;
                enemy.WillRevive = false;
            }

            if (enemy.CurrentHealth <= 0) continue;

            // 回合开始自动效果
            if (enemy.SourceData != null && enemy.SourceData.StartOfTurnActions != null && enemy.SourceData.StartOfTurnActions.Count > 0)
            {
                foreach (var action in enemy.SourceData.StartOfTurnActions)
                {
                    enemyTurnGA.PerformReactions.Add(new AttackHeroGA(enemy, action));
                }
            }

            var actions = enemy.selectedActions;
            if (actions == null || actions.Count == 0)
            {
                enemy.DecrementCooldowns();
                actions = SelectActions(enemy);
            }
            enemy.selectedActions = new();

            foreach (var action in actions)
            {
                if (action.ActionType == EnemyActionType.Move)
                {
                    QueueMoveAction(enemy, action, enemyTurnGA, reservedCells);
                }
                else
                {
                    enemyTurnGA.PerformReactions.Add(new AttackHeroGA(enemy, action));
                }
            }
        }
    }

    private List<EnemyAction> SelectActions(EnemyView enemy)
    {
        if (enemy.SourceData.UseModeSelection)
            return SelectMode(enemy);

        var actionPool = enemy.SourceData.ActionPool;
        if (actionPool == null || actionPool.Count == 0)
            return new List<EnemyAction>();

        var available = actionPool
            .Where(a => !enemy.IsOnCooldown(a))
            .Where(a => enemy.TurnCount >= a.MinTurnToUse)
            .ToList();

        return WeightedSelectWithoutReplacement(available, enemy.SourceData.ActionsPerTurn);
    }

    private List<EnemyAction> SelectMode(EnemyView enemy)
    {
        var modes = enemy.SourceData.Modes;
        if (modes == null || modes.Count == 0)
            return new List<EnemyAction>();

        var availableModes = modes
            .Where(m => !enemy.IsOnCooldown(m))
            .Where(m => enemy.TurnCount >= m.MinTurnToUse)
            .ToList();

        if (availableModes.Count == 0) return new List<EnemyAction>();

        int totalWeight = availableModes.Sum(m => m.Weight);
        int roll = Random.Range(0, totalWeight);
        int cumulative = 0;
        EnemyMode selected = null;
        foreach (var mode in availableModes)
        {
            cumulative += mode.Weight;
            if (roll < cumulative)
            {
                selected = mode;
                break;
            }
        }

        if (selected == null) return new List<EnemyAction>();

        enemy.SetModeCooldown(selected);

        return new List<EnemyAction>(selected.Actions);
    }

    private List<EnemyAction> WeightedSelectWithoutReplacement(List<EnemyAction> pool, int count)
    {
        var result = new List<EnemyAction>();
        var remaining = new List<EnemyAction>(pool);

        for (int i = 0; i < count && remaining.Count > 0; i++)
        {
            int totalWeight = remaining.Sum(a => a.Weight);
            if (totalWeight <= 0) break;

            int roll = Random.Range(0, totalWeight);
            int cumulative = 0;
            EnemyAction selected = null;
            for (int j = 0; j < remaining.Count; j++)
            {
                cumulative += remaining[j].Weight;
                if (roll < cumulative)
                {
                    selected = remaining[j];
                    break;
                }
            }

            if (selected == null) break;

            result.Add(selected);
            remaining.Remove(selected);

            if (selected.IsExclusive) break;
        }

        return result;
    }

    private void QueueMoveAction(EnemyView enemy, EnemyAction action, EnemyTurnGA ga, HashSet<(int, int)> reservedCells)
    {
        HeroView hero = HeroSystem.Instance.HeroView;
        int dist = HexGrid.HexDistance(enemy.HexCoordX, enemy.HexCoordZ, hero.HexCoordX, hero.HexCoordZ);
        if (dist <= 1) return;

        var extraExcluded = new HashSet<(int, int)>(reservedCells);
        extraExcluded.Remove((enemy.HexCoordX, enemy.HexCoordZ));
        var path = HexPathfinder.FindPath(enemy.HexCoordX, enemy.HexCoordZ, hero.HexCoordX, hero.HexCoordZ, enemy, extraExcluded);
        if (path != null && path.Count >= 2)
        {
            int stepIndex = Mathf.Min(action.MoveRange, path.Count - 1);
            var (x, z) = path[stepIndex];
            if (!reservedCells.Contains((x, z)))
            {
                reservedCells.Add((x, z));
                ga.PreReactions.Add(new MoveGA(enemy, x, z));
            }
        }
    }

    private IEnumerator AttackHeroPerformer(AttackHeroGA attackHeroGA)
    {
        EnemyView attacker = attackHeroGA.Attacker;
        HeroView heroView = HeroSystem.Instance.HeroView;
        EnemyAction action = attackHeroGA.Action;

        if (action.ActionType == EnemyActionType.Attack)
        {
            int dist = HexGrid.HexDistance(attacker.HexCoordX, attacker.HexCoordZ, heroView.HexCoordX, heroView.HexCoordZ);
            if (dist > 1)
            {
                Debug.LogWarning($"[EnemySystem] {attacker.name} 攻击距离不足 (距离={dist})，跳过攻击");
                yield break;
            }

            Vector3 direction = (heroView.transform.position - attacker.transform.position).normalized;
            Vector3 startPos = attacker.transform.position;
            Vector3 targetPos = startPos + direction * 1f;
            Tween tween = attacker.transform.DOMove(targetPos, 0.15f);
            yield return tween.WaitForCompletion();
            attacker.transform.DOMove(startPos, 0.25f);

            int damage = action.BaseDamage;
            int hitCount = action.HitCount;

            for (int i = 0; i < hitCount; i++)
            {
                DealDamageGA dealDamageGA = new(damage, 1, new() { heroView }, attackHeroGA.Caster);
                ActionSystem.Instance.AddReaction(dealDamageGA);
            }
        }

        if (action.StatusEffects != null)
        {
            foreach (var se in action.StatusEffects)
            {
                if (se.Target == TargetType.AllOtherEnemies)
                {
                    foreach (var otherEnemy in EnemySystem.Instance.Enemies)
                    {
                        if (otherEnemy != null && otherEnemy != attacker && otherEnemy.CurrentHealth > 0)
                            ApplyStatusEffectWithReplace(otherEnemy, se);
                    }
                }
                else
                {
                    CombatantView target = se.Target == TargetType.Self ? attacker : heroView;
                    ApplyStatusEffectWithReplace(target, se);
                }
            }
        }

        if (action.CooldownTurns > 0)
        {
            attacker.SetCooldown(action);
        }
    }

    private static void ApplyStatusEffectWithReplace(CombatantView target, StatusEffectInfliction se)
    {
        if (target == null) return;
        if (se.ReplaceExisting)
        {
            int existing = target.GetStatusEffectStacks(se.EffectType);
            if (existing > 0)
                target.RemoveStatusEffect(se.EffectType, existing);
        }
        target.AddStatusEffect(se.EffectType, se.StackCount);
    }

    private IEnumerator KillEnemyPerformer(KillEnemyGA killEnemyGA)
    {
        var enemy = killEnemyGA.EnemyView;

        if (enemy.SourceData != null && enemy.SourceData.CanRevive && !enemy.HasRevived)
        {
            enemy.HideIntents();
            enemy.WillRevive = true;
            yield break;
        }

        killEnemyGA.EnemyView.HideIntents();
        yield return enemyBoardView.RemoveEnemy(killEnemyGA.EnemyView);
    }

    private void EnemyTurnPostReaction(EnemyTurnGA enemyTurnGA)
    {
        ComputeAndStoreNextTurnIntents();
        foreach (var enemy in enemyBoardView.EnemyViews)
        {
            if (enemy.CurrentHealth <= 0) continue;
            enemy.TransitionIntents(enemy.currentIntents);
        }
    }

    public void ComputeAndStoreNextTurnIntents()
    {
        Debug.Log($"[EnemySystem] ComputeAndStoreNextTurnIntents, 敌人数量={enemyBoardView.EnemyViews.Count}");
        HeroView heroView = HeroSystem.Instance?.HeroView;
        foreach (var enemy in enemyBoardView.EnemyViews)
        {
            if (enemy.CurrentHealth <= 0) continue;

            enemy.DecrementCooldowns();
            var actions = SelectActions(enemy);
            enemy.selectedActions = actions;

            Debug.Log($"[EnemySystem] {enemy.name} 选中 {actions.Count} 个动作: {string.Join(", ", actions.ConvertAll(a => a.ActionType.ToString()))}");

            enemy.currentIntents = actions.ConvertAll(a => new EnemyIntentData
            {
                IntentType = a.ActionType,
                HitCount = a.HitCount,
                ValuePerHit = a.ActionType == EnemyActionType.Attack
                    ? DamageSystem.CalculateModifiedDamage(a.BaseDamage, enemy, heroView)
                    : a.BaseDamage,
            });
        }
    }

    private void OnEnemyDealDamage(DealDamageGA dealDamageGA)
    {
        if (dealDamageGA.UnblockedAmount <= 0) return;
        if (dealDamageGA.Caster is not EnemyView attacker) return;
        if (attacker.SourceData == null || attacker.SourceData.OnDealDamageEffects == null || attacker.SourceData.OnDealDamageEffects.Count == 0) return;

        bool hitHero = dealDamageGA.Targets.Any(t => t is HeroView);
        if (!hitHero) return;

        foreach (var se in attacker.SourceData.OnDealDamageEffects)
        {
            ApplyStatusEffectWithReplace(attacker, se);
        }
    }
}
