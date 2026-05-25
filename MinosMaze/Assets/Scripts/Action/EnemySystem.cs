using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using DG.Tweening;

public class EnemySystem : Singleton<EnemySystem>
{
    [SerializeField] private EnemyBoardView enemyBoardView;

    public List<EnemyView> Enemies => enemyBoardView.EnemyViews;

    void OnEnable()
    {
        ActionSystem.AttachPerformer<EnemyTurnGA>(EnemyTurnPerformer);
        ActionSystem.AttachPerformer<AttackHeroGA>(AttackHeroPerformer);
        ActionSystem.AttachPerformer<KillEnemyGA>(KillEnemyPerformer);
        ActionSystem.SubscribeReaction<EnemyTurnGA>(EnemyTurnPreReaction, ReactionTiming.PRE);
    }

    void OnDisable()
    {
        ActionSystem.DetachPerformer<EnemyTurnGA>();
        ActionSystem.DetachPerformer<AttackHeroGA>();
        ActionSystem.DetachPerformer<KillEnemyGA>();
        ActionSystem.UnsubscribeReaction<EnemyTurnGA>(EnemyTurnPreReaction, ReactionTiming.PRE);
    }

    public void Setup(List<EnemyData> enemyDatas, List<Vector2Int> spawnCoords)
    {
        for (int i = 0; i < enemyDatas.Count; i++)
        {
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
        foreach (var enemy in enemyBoardView.EnemyViews)
        {
            enemy.DecrementCooldowns();
            SelectAndQueueActions(enemy, enemyTurnGA);
        }
        yield return null;
    }

    private void SelectAndQueueActions(EnemyView enemy, EnemyTurnGA ga)
    {
        HeroView hero = HeroSystem.Instance.HeroView;
        int dist = HexGrid.HexDistance(enemy.HexCoordX, enemy.HexCoordZ, hero.HexCoordX, hero.HexCoordZ);

        var actionPool = enemy.SourceData.ActionPool;
        if (actionPool == null || actionPool.Count == 0)
        {
            if (dist <= 1)
            {
                ga.PerformReactions.Add(new AttackHeroGA(enemy));
            }
            return;
        }

        var available = actionPool.Where(a => !enemy.IsOnCooldown(a.Tag)).ToList();
        var selected = WeightedSelectWithoutReplacement(available, enemy.SourceData.ActionsPerTurn);

        foreach (var action in selected)
        {
            ga.PerformReactions.Add(new AttackHeroGA(enemy, action));
        }
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
            for (int j = 0; j < remaining.Count; j++)
            {
                cumulative += remaining[j].Weight;
                if (roll < cumulative)
                {
                    result.Add(remaining[j]);
                    remaining.RemoveAt(j);
                    break;
                }
            }
        }

        return result;
    }

    private void EnemyTurnPreReaction(EnemyTurnGA enemyTurnGA)
    {
        HashSet<(int, int)> reservedCells = new();
        foreach (var enemy in enemyBoardView.EnemyViews)
        {
            switch (enemy.EnemyType)
            {
                case EnemyType.Normal:
                    QueueNormalMove(enemy, enemyTurnGA, reservedCells);
                    break;
            }
        }
    }

    private void QueueNormalMove(EnemyView enemy, EnemyTurnGA ga, HashSet<(int, int)> reservedCells)
    {
        HeroView hero = HeroSystem.Instance.HeroView;
        int dist = HexGrid.HexDistance(enemy.HexCoordX, enemy.HexCoordZ, hero.HexCoordX, hero.HexCoordZ);
        if (dist <= 1) return;

        var extraExcluded = new HashSet<(int, int)>(reservedCells);
        extraExcluded.Remove((enemy.HexCoordX, enemy.HexCoordZ));
        var path = HexPathfinder.FindPath(enemy.HexCoordX, enemy.HexCoordZ, hero.HexCoordX, hero.HexCoordZ, enemy, extraExcluded);
        if (path != null && path.Count >= 2)
        {
            var (x, z) = path[1];
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

        EnemyAction action = attackHeroGA.Action;
        int damage = action != null ? action.BaseDamage : attacker.AttackPower;
        int hitCount = action != null ? action.HitCount : 1;

        for (int i = 0; i < hitCount; i++)
        {
            DealDamageGA dealDamageGA = new(damage, new() { heroView }, attackHeroGA.Caster);
            ActionSystem.Instance.AddReaction(dealDamageGA);
        }

        if (action?.StatusEffects != null)
        {
            foreach (var se in action.StatusEffects)
            {
                CombatantView target = se.Target == TargetType.Self ? attacker : heroView;
                target.AddStatusEffect(se.EffectType, se.StackCount);
            }
        }

        if (action != null && !string.IsNullOrEmpty(action.Tag) && action.CooldownTurns > 0)
        {
            attacker.SetCooldown(action.Tag, action.CooldownTurns);
        }
    }

    private IEnumerator KillEnemyPerformer(KillEnemyGA killEnemyGA)
    {
        yield return enemyBoardView.RemoveEnemy(killEnemyGA.EnemyView);
    }

}
