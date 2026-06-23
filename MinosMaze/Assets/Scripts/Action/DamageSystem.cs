using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DamageSystem : MonoBehaviour
{
    [SerializeField] private GameObject damageVFX;

    void OnEnable()
    {
        ActionSystem.AttachPerformer<DealDamageGA>(DealDamagePerformer);
        ActionSystem.AttachPerformer<DealArmorDamageGA>(DealArmorDamagePerformer);
    }

    void OnDisable()
    {
        ActionSystem.DetachPerformer<DealDamageGA>();
        ActionSystem.DetachPerformer<DealArmorDamageGA>();
    }

    private IEnumerator DealDamagePerformer(DealDamageGA dealDamageGA)
    {
        int totalUnblocked = 0;
        foreach (var target in dealDamageGA.Targets)
        {
            if (target == null) continue;
            for (int h = 0; h < dealDamageGA.HitCount; h++)
            {
                if (target == null) break;
                int modifiedDamage = CalculateModifiedDamage(dealDamageGA.Amount, dealDamageGA.Caster, target);
                target.Damage(modifiedDamage);
                totalUnblocked += target.LastUnblockedDamage;
                SpawnDamageVFX(target);
                if (target is EnemyView enemyView && enemyView.SourceData != null && enemyView.SourceData.HitSFX != null)
                    AudioManager.Instance?.PlaySFX(enemyView.SourceData.HitSFX);
                else if (target is HeroView)
                    AudioManager.Instance?.PlaySFX(AudioManager.Instance?.Config?.playerHitSFX);
                yield return new WaitForSeconds(0.1f);
                if (target == null) break;
                if (target.CurrentHealth == 0 && target is EnemyView)
                {
                    KillEnemyGA killEnemyGA = new((EnemyView)target);
                    ActionSystem.Instance.AddReaction(killEnemyGA);
                    break;
                }
            }
        }
        dealDamageGA.UnblockedAmount = totalUnblocked;
    }

    private IEnumerator DealArmorDamagePerformer(DealArmorDamageGA ga)
    {
        foreach (var target in ga.Targets)
        {
            int armorStacks = target.GetStatusEffectStacks(StatusEffectType.ARMOR);
            if (armorStacks <= 0)
            {
                Debug.Log($"[DamageSystem] {target.name} 无护甲，跳过护甲伤害");
                continue;
            }
            int modifiedDamage = CalculateModifiedDamage(armorStacks, ga.Caster, target);
            DealDamageGA innerGA = new(modifiedDamage, 1, new List<CombatantView> { target }, ga.Caster);
            ActionSystem.Instance.AddReaction(innerGA);
            Debug.Log($"[DamageSystem] {target.name} 护甲 {armorStacks} → 伤害 {modifiedDamage}");
            yield return null;
        }
    }

    public static int CalculateModifiedDamage(int baseDamage, CombatantView caster, CombatantView target)
    {
        float damage = baseDamage;

        if (caster != null)
        {
            damage += caster.GetStatusEffectStacks(StatusEffectType.STRENGTH);
            if (caster.HasStatusEffect(StatusEffectType.WEAKNESS))
                damage *= 0.5f;
        }

        if (target != null && target.HasStatusEffect(StatusEffectType.VULNERABLE))
            damage *= 1.5f;

        if (target != null && target.HasStatusEffect(StatusEffectType.FLYING))
            damage *= 0.5f;

        return Mathf.FloorToInt(damage);
    }

    private void SpawnDamageVFX(CombatantView target)
    {
        if (damageVFX != null && target != null)
        {
            Vector3 pos = new Vector3(target.transform.position.x, target.transform.position.y + 1, target.transform.position.z);
            GameObject vfxInstance = Instantiate(damageVFX, pos, Quaternion.identity);
        }
    }
}
