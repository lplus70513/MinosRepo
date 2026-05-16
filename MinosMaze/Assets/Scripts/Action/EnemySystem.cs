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
    }

    void OnDisable()
    {
        ActionSystem.DetachPerformer<EnemyTurnGA>();
        ActionSystem.DetachPerformer<AttackHeroGA>();
        ActionSystem.DetachPerformer<KillEnemyGA>();
    }

    public void Setup(List<EnemyData> enemyDatas, List<Vector2Int> spawnCoords)
    {
        for (int i = 0; i < enemyDatas.Count; i++)
        {
            Vector3 pos = Vector3.zero;
            Quaternion rot = Quaternion.identity;
            if (i < spawnCoords.Count)
            {
                Vector2Int coord = spawnCoords[i];
                pos = HexGrid.GetStandingPoint(coord.x, coord.y);
            }
            enemyBoardView.AddEnemy(enemyDatas[i], pos, rot);
        }
    }

    private IEnumerator EnemyTurnPerformer(EnemyTurnGA enemyTurnGA)
    {
        foreach(var enemy in enemyBoardView.EnemyViews)
        {
            AttackHeroGA attackHeroGA = new(enemy);
            ActionSystem.Instance.AddReaction(attackHeroGA);
        }
        yield return null;
    }

    private IEnumerator AttackHeroPerformer(AttackHeroGA attackHeroGA)
    {
        EnemyView attacker = attackHeroGA.Attacker;
        HeroView heroView = HeroSystem.Instance.HeroView;
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
