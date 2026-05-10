using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DamageSystem : MonoBehaviour
{
    [SerializeField] private GameObject damageVFX;

    void OnEnable()
    {
        ActionSystem.AttachPerformer<DealDamageGA>(DealDamagePerformer);
    }

    void OnDisable()
    {
        ActionSystem.DetachPerformer<DealDamageGA>();
    }

    private IEnumerator DealDamagePerformer(DealDamageGA dealDamageGA)
    {
        foreach (var target in dealDamageGA.Targets)
        {
            // 1. 先造成伤害
            target.Damage(dealDamageGA.Amount);

            // 2. 只有当特效预设体不为空时，才生成它
            if (damageVFX != null)
            {
                Instantiate(damageVFX, target.transform.position, Quaternion.identity);
            }
            // 3. 如果 damageVFX 为空，代码会直接跳过上面的 Instantiate，继续向下执行
            yield return new WaitForSeconds(0.15f);

            if(target.CurrentHealth == 0)
            {
                if(target is EnemyView enemyView)
                {
                    KillEnemyGA killEnemyGA = new(enemyView);
                    ActionSystem.Instance.AddReaction(killEnemyGA);
                }
                else
                {

                }
            }
        }
    }
}
