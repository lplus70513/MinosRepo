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
        if (mover == null) yield break;

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
        if (mover is HeroView)
            AudioManager.Instance?.PlaySFX(AudioManager.Instance?.Config?.playerMoveSFX);
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
            yield break;
        }

        (int x, int z)? pullCell = FindPullCell(puller.HexCoordX, puller.HexCoordZ, target.HexCoordX, target.HexCoordZ);
        if (pullCell == null)
        {
            yield break;
        }

        var (tx, tz) = pullCell.Value;
        MoveGA moveGA = new(target, tx, tz);
        ActionSystem.Instance.AddReaction(moveGA);
        yield return null;
    }

    private IEnumerator StepBackPerformer(StepBackGA ga)
    {
        CombatantView mover = ga.Mover;
        if (mover == null) yield break;

        var enemies = EnemySystem.Instance?.Enemies;
        if (enemies == null || enemies.Count == 0)
        {
            yield break;
        }

        EnemyView nearest = null;
        int nearestDist = int.MaxValue;
        foreach (var enemy in enemies)
        {
            if (enemy == null) continue;
            int d = HexGrid.HexDistance(mover.HexCoordX, mover.HexCoordZ, enemy.HexCoordX, enemy.HexCoordZ);
            if (d < nearestDist)
            {
                nearestDist = d;
                nearest = enemy;
            }
        }

        if (nearest == null)
        {
            yield break;
        }

        int dirDx = mover.HexCoordX - nearest.HexCoordX;
        int dirDz = mover.HexCoordZ - nearest.HexCoordZ;

        int matchIdx = FindDirectionIndex(dirDx, dirDz);
        if (matchIdx >= 0)
        {
            var (ddx, ddz) = hexNeighbors[matchIdx];
            int nx = mover.HexCoordX + ddx;
            int nz = mover.HexCoordZ + ddz;
            if (HexGrid.ContainsCell(nx, nz) && !HexMove.IsCellOccupied(nx, nz))
            {
                HexCell cell = HexGrid.GetCell(nx, nz);
                if (cell != null && cell.IsWalkable)
                {
                    MoveGA moveGA = new(mover, nx, nz);
                    ActionSystem.Instance.AddReaction(moveGA);
                    yield break;
                }
            }
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
            yield break;
        }

        var (bx, bz) = bestCell.Value;
        MoveGA moveGA2 = new(mover, bx, bz);
        ActionSystem.Instance.AddReaction(moveGA2);
        yield return null;
    }

    private static int FindDirectionIndex(int dx, int dz)
    {
        if (dx == 0 && dz == 0) return -1;
        for (int i = 0; i < 6; i++)
        {
            var (ddx, ddz) = hexNeighbors[i];
            if (ddx == 0)
            {
                if (dx != 0) continue;
                if (ddz > 0 && dz > 0) return i;
                if (ddz < 0 && dz < 0) return i;
            }
            else if (ddz == 0)
            {
                if (dz != 0) continue;
                if (ddx > 0 && dx > 0) return i;
                if (ddx < 0 && dx < 0) return i;
            }
            else
            {
                if (dx % ddx != 0 || dz % ddz != 0) continue;
                if (dx / ddx != dz / ddz) continue;
                if (dx / ddx > 0) return i;
            }
        }
        return -1;
    }

    private (int x, int z)? FindPullCell(int px, int pz, int tx, int tz)
    {
        int dirDx = tx - px;
        int dirDz = tz - pz;

        int matchIdx = FindDirectionIndex(dirDx, dirDz);
        if (matchIdx >= 0)
        {
            var (ddx, ddz) = hexNeighbors[matchIdx];
            int nx = px + ddx;
            int nz = pz + ddz;
            if (HexGrid.ContainsCell(nx, nz) && !HexMove.IsCellOccupied(nx, nz))
            {
                HexCell cell = HexGrid.GetCell(nx, nz);
                if (cell != null && cell.IsWalkable)
                    return (nx, nz);
            }
        }

        var searchOrder = new System.Collections.Generic.List<int>();
        if (matchIdx >= 0) searchOrder.Add(matchIdx);
        for (int i = 0; i < 6; i++)
            if (i != matchIdx) searchOrder.Add(i);

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
            int bestDx = 0, bestDz = 0;
            int bestDist = HexGrid.HexDistance(cx, cz, tx, tz);

            foreach (var (dx, dz) in hexNeighbors)
            {
                int nx = cx + dx;
                int nz = cz + dz;
                if (!HexGrid.ContainsCell(nx, nz)) continue;
                int dist = HexGrid.HexDistance(nx, nz, tx, tz);
                if (dist < bestDist)
                {
                    bestDist = dist;
                    bestDx = dx;
                    bestDz = dz;
                }
            }

            if (bestDx == 0 && bestDz == 0) break;

            int nx2 = cx + bestDx;
            int nz2 = cz + bestDz;

            if (!HexGrid.ContainsCell(nx2, nz2)) break;
            HexCell cell = HexGrid.GetCell(nx2, nz2);
            if (cell == null || !cell.IsWalkable) break;

            int prevX = cx;
            int prevZ = cz;

            Vector3 targetPos = HexGrid.GetStandingPoint(nx2, nz2);
            mover.SetFacing(nx2, nz2);
            Tween tween = mover.transform.DOMove(targetPos, 0.12f);
            yield return tween.WaitForCompletion();
            mover.HexCoordX = nx2;
            mover.HexCoordZ = nz2;
            cx = nx2;
            cz = nz2;

            EnemyView enemyAt = EnemySystem.Instance.GetEnemyAt(nx2, nz2);
            if (enemyAt != null)
            {
                DealDamageGA damageGA = new(ga.Damage, 1, new List<CombatantView> { enemyAt }, ga.Caster);
                ActionSystem.Instance.AddReaction(damageGA);

                if (!enemyAt) yield break;

                (int x, int z)? pushCell = FindPushCell(prevX, prevZ, enemyAt.HexCoordX, enemyAt.HexCoordZ);
                if (pushCell != null)
                {
                    MoveGA pushGA = new(enemyAt, pushCell.Value.x, pushCell.Value.z);
                    ActionSystem.Instance.AddReaction(pushGA);
                }
                else
                {
                }

                yield break;
            }

            if (nx2 == tx && nz2 == tz)
                yield break;
        }
    }

    private (int x, int z)? FindPushCell(int hx, int hz, int ex, int ez)
    {
        int dirDx = ex - hx;
        int dirDz = ez - hz;
        int currentDist = HexGrid.HexDistance(hx, hz, ex, ez);

        int matchIdx = FindDirectionIndex(dirDx, dirDz);
        if (matchIdx >= 0)
        {
            var (ddx, ddz) = hexNeighbors[matchIdx];
            int nx = ex + ddx;
            int nz = ez + ddz;
            if (HexGrid.ContainsCell(nx, nz))
            {
                HexCell cell = HexGrid.GetCell(nx, nz);
                if (cell != null && cell.IsWalkable && !HexMove.IsCellOccupied(nx, nz))
                {
                    int dist = HexGrid.HexDistance(hx, hz, nx, nz);
                    if (dist > currentDist)
                        return (nx, nz);
                }
            }
        }

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
