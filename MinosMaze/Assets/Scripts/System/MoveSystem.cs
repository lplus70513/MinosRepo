using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class MoveSystem : Singleton<MoveSystem>
{
    private static readonly (int dx, int dz)[] hexNeighbors = { (1, 0), (1, -1), (0, -1), (-1, 0), (-1, 1), (0, 1) };

    void OnEnable()
    {
        ActionSystem.AttachPerformer<MoveGA>(MovePerformer);
        ActionSystem.AttachPerformer<PullTargetGA>(PullTargetPerformer);
        ActionSystem.AttachPerformer<StepBackGA>(StepBackPerformer);
        ActionSystem.AttachPerformer<ChargeGA>(ChargePerformer);
    }

    void OnDisable()
    {
        ActionSystem.DetachPerformer<MoveGA>();
        ActionSystem.DetachPerformer<PullTargetGA>();
        ActionSystem.DetachPerformer<StepBackGA>();
        ActionSystem.DetachPerformer<ChargeGA>();
    }

    private IEnumerator MovePerformer(MoveGA moveGA)
    {
        CombatantView mover = moveGA.Mover;

        HexCell targetCell = HexGrid.GetCell(moveGA.ToX, moveGA.ToZ);
        if (targetCell == null || !targetCell.IsWalkable)
        {
            Debug.LogWarning($"[MoveSystem] {mover.name} 目标格 ({moveGA.ToX}, {moveGA.ToZ}) 不可行走，跳过移动");
            yield break;
        }

        if (HexMove.IsCellOccupied(moveGA.ToX, moveGA.ToZ, mover))
        {
            Debug.LogWarning($"[MoveSystem] {mover.name} 目标格 ({moveGA.ToX}, {moveGA.ToZ}) 已被占据，跳过移动");
            yield break;
        }

        Vector3 targetPos = HexGrid.GetStandingPoint(moveGA.ToX, moveGA.ToZ);
        mover.SetFacing(moveGA.ToX, moveGA.ToZ);
        Tween tween = mover.transform.DOMove(targetPos, 0.2f);
        yield return tween.WaitForCompletion();
        mover.HexCoordX = moveGA.ToX;
        mover.HexCoordZ = moveGA.ToZ;
    }

    private IEnumerator PullTargetPerformer(PullTargetGA ga)
    {
        CombatantView target = ga.Target;
        CombatantView puller = ga.Puller;
        if (target == null || puller == null) yield break;

        int dist = HexGrid.HexDistance(puller.HexCoordX, puller.HexCoordZ, target.HexCoordX, target.HexCoordZ);
        if (dist <= 1)
        {
            Debug.Log($"[MoveSystem] {target.name} 已在身前，无需拖拽");
            yield break;
        }

        (int x, int z)? pullCell = FindPullCell(puller.HexCoordX, puller.HexCoordZ, target.HexCoordX, target.HexCoordZ);
        if (pullCell == null)
        {
            Debug.Log($"[MoveSystem] {target.name} 拖拽目标格全被占据，效果空过");
            yield break;
        }

        var (tx, tz) = pullCell.Value;
        MoveGA moveGA = new(target, tx, tz);
        ActionSystem.Instance.AddReaction(moveGA);
        Debug.Log($"[MoveSystem] 将 {target.name} 拖拽至 ({tx},{tz})");
        yield return null;
    }

    private IEnumerator StepBackPerformer(StepBackGA ga)
    {
        CombatantView mover = ga.Mover;
        if (mover == null) yield break;

        var enemies = EnemySystem.Instance?.Enemies;
        if (enemies == null || enemies.Count == 0)
        {
            Debug.Log("[MoveSystem] 无敌人，无法决定后退方向");
            yield break;
        }

        int bestDist = int.MinValue;
        (int x, int z)? bestCell = null;

        foreach (var (dx, dz) in hexNeighbors)
        {
            int nx = mover.HexCoordX + dx;
            int nz = mover.HexCoordZ + dz;
            if (!HexGrid.ContainsCell(nx, nz)) continue;
            if (HexMove.IsCellOccupied(nx, nz)) continue;
            HexCell cell = HexGrid.GetCell(nx, nz);
            if (cell == null || !cell.IsWalkable) continue;

            int minEnemyDist = int.MaxValue;
            foreach (var enemy in enemies)
            {
                if (enemy == null) continue;
                int ed = HexGrid.HexDistance(nx, nz, enemy.HexCoordX, enemy.HexCoordZ);
                if (ed < minEnemyDist) minEnemyDist = ed;
            }
            if (minEnemyDist > bestDist)
            {
                bestDist = minEnemyDist;
                bestCell = (nx, nz);
            }
        }

        if (bestCell == null)
        {
            Debug.Log("[MoveSystem] 后退无可用格子，效果空过");
            yield break;
        }

        var (bx, bz) = bestCell.Value;
        MoveGA moveGA = new(mover, bx, bz);
        ActionSystem.Instance.AddReaction(moveGA);
        Debug.Log($"[MoveSystem] 后退至 ({bx},{bz})");
        yield return null;
    }

    private (int x, int z)? FindPullCell(int px, int pz, int tx, int tz)
    {
        int dirDx = tx - px;
        int dirDz = tz - pz;

        int bestIdx = 0;
        float bestDot = float.MinValue;

        for (int i = 0; i < 6; i++)
        {
            var (dx, dz) = hexNeighbors[i];
            float dot = dx * dirDx + dz * dirDz;
            if (dot > bestDot)
            {
                bestDot = dot;
                bestIdx = i;
            }
        }

        int[] searchOrder = { bestIdx, (bestIdx + 1) % 6, (bestIdx + 5) % 6, (bestIdx + 2) % 6, (bestIdx + 4) % 6, (bestIdx + 3) % 6 };

        foreach (int idx in searchOrder)
        {
            var (dx, dz) = hexNeighbors[idx];
            int nx = px + dx;
            int nz = pz + dz;
            if (HexGrid.ContainsCell(nx, nz) && !HexMove.IsCellOccupied(nx, nz))
            {
                HexCell cell = HexGrid.GetCell(nx, nz);
                if (cell != null && cell.IsWalkable)
                    return (nx, nz);
            }
        }

        return null;
    }

    private IEnumerator ChargePerformer(ChargeGA ga)
    {
        CombatantView mover = ga.Caster;
        CombatantView target = ga.Target;
        if (mover == null || target == null) yield break;

        int cx = mover.HexCoordX;
        int cz = mover.HexCoordZ;
        int tx = target.HexCoordX;
        int tz = target.HexCoordZ;

        for (int step = 0; step < ga.Range; step++)
        {
            int dirDx = tx - cx;
            int dirDz = tz - cz;

            float bestDot = float.MinValue;
            int bestDx = 0, bestDz = 0;

            foreach (var (dx, dz) in hexNeighbors)
            {
                float dot = dx * dirDx + dz * dirDz;
                if (dot > bestDot)
                {
                    bestDot = dot;
                    bestDx = dx;
                    bestDz = dz;
                }
            }

            int nx = cx + bestDx;
            int nz = cz + bestDz;

            if (!HexGrid.ContainsCell(nx, nz)) break;
            HexCell cell = HexGrid.GetCell(nx, nz);
            if (cell == null || !cell.IsWalkable) break;

            int prevX = cx;
            int prevZ = cz;

            Vector3 targetPos = HexGrid.GetStandingPoint(nx, nz);
            mover.SetFacing(nx, nz);
            Tween tween = mover.transform.DOMove(targetPos, 0.12f);
            yield return tween.WaitForCompletion();
            mover.HexCoordX = nx;
            mover.HexCoordZ = nz;
            cx = nx;
            cz = nz;

            EnemyView enemyAt = EnemySystem.Instance.GetEnemyAt(nx, nz);
            if (enemyAt != null)
            {
                DealDamageGA damageGA = new(ga.Damage, 1, new List<CombatantView> { enemyAt }, ga.Caster);
                ActionSystem.Instance.AddReaction(damageGA);

                (int x, int z)? pushCell = FindPushCell(prevX, prevZ, enemyAt.HexCoordX, enemyAt.HexCoordZ);
                if (pushCell != null)
                {
                    MoveGA pushGA = new(enemyAt, pushCell.Value.x, pushCell.Value.z);
                    ActionSystem.Instance.AddReaction(pushGA);
                    Debug.Log($"[MoveSystem] 冲锋击退 {enemyAt.name} 至 ({pushCell.Value.x},{pushCell.Value.z})");
                }
                else
                {
                    Debug.Log($"[MoveSystem] {enemyAt.name} 背后无可用格子，击退失败");
                }

                yield break;
            }

            if (nx == tx && nz == tz)
                yield break;
        }
    }

    private (int x, int z)? FindPushCell(int hx, int hz, int ex, int ez)
    {
        int dirDx = ex - hx;
        int dirDz = ez - hz;

        int currentDist = HexGrid.HexDistance(hx, hz, ex, ez);
        float bestDot = float.MinValue;
        (int x, int z)? bestCell = null;

        foreach (var (dx, dz) in hexNeighbors)
        {
            int nx = ex + dx;
            int nz = ez + dz;

            if (!HexGrid.ContainsCell(nx, nz)) continue;
            HexCell cell = HexGrid.GetCell(nx, nz);
            if (cell == null || !cell.IsWalkable) continue;
            if (HexMove.IsCellOccupied(nx, nz)) continue;

            int dist = HexGrid.HexDistance(hx, hz, nx, nz);
            if (dist <= currentDist) continue;

            float dot = dx * dirDx + dz * dirDz;
            if (dot > bestDot)
            {
                bestDot = dot;
                bestCell = (nx, nz);
            }
        }

        return bestCell;
    }
}
