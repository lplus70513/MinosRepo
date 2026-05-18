using System.Collections;
using System.Collections.Generic;
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
        foreach(var enemy in enemyBoardView.EnemyViews)
        {
            switch (enemy.EnemyType)
            {
                case EnemyType.Normal:
                    QueueNormalAttack(enemy, enemyTurnGA);
                    break;
            }
        }
        yield return null;
    }

    private void EnemyTurnPreReaction(EnemyTurnGA enemyTurnGA)
    {
        HashSet<(int, int)> reservedCells = new();
        foreach(var enemy in enemyBoardView.EnemyViews)
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

    private void QueueNormalAttack(EnemyView enemy, EnemyTurnGA ga)
    {
        HeroView hero = HeroSystem.Instance.HeroView;
        int dist = HexGrid.HexDistance(enemy.HexCoordX, enemy.HexCoordZ, hero.HexCoordX, hero.HexCoordZ);
        if (dist <= 1)
        {
            ga.PerformReactions.Add(new AttackHeroGA(enemy));
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
        // Deal Damage
        DealDamageGA dealDamageGA = new(attacker.AttackPower, new() { heroView }, attackHeroGA.Caster);
        ActionSystem.Instance.AddReaction(dealDamageGA);
    }

    private IEnumerator KillEnemyPerformer(KillEnemyGA killEnemyGA)
    {
        yield return enemyBoardView.RemoveEnemy(killEnemyGA.EnemyView);
    }

}
