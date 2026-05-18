using System.Collections;
using UnityEngine;
using DG.Tweening;

public class MoveSystem : Singleton<MoveSystem>
{
    void OnEnable()
    {
        ActionSystem.AttachPerformer<MoveGA>(MovePerformer);
    }

    void OnDisable()
    {
        ActionSystem.DetachPerformer<MoveGA>();
    }

    private IEnumerator MovePerformer(MoveGA moveGA)
    {
        CombatantView mover = moveGA.Mover;

        if (HexGrid.IsCellOccupied(moveGA.ToX, moveGA.ToZ, mover))
        {
            Debug.LogWarning($"[MoveSystem] {mover.name} 目标格 ({moveGA.ToX}, {moveGA.ToZ}) 已被占据，跳过移动");
            yield break;
        }

        Vector3 targetPos = HexGrid.GetStandingPoint(moveGA.ToX, moveGA.ToZ);
        Tween tween = mover.transform.DOMove(targetPos, 0.2f);
        yield return tween.WaitForCompletion();
        mover.HexCoordX = moveGA.ToX;
        mover.HexCoordZ = moveGA.ToZ;
    }
}
