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
    }

    void OnDisable()
    {
        ActionSystem.DetachPerformer<MoveGA>();
        ActionSystem.DetachPerformer<PullTargetGA>();
        ActionSystem.DetachPerformer<StepBackGA>();
    }

    private IEnumerator MovePerformer(MoveGA moveGA)
    {
        CombatantView mover = moveGA.Mover;

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
                return (nx, nz);
        }

        return null;
    }
}
