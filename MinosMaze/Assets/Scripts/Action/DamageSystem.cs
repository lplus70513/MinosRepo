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
        foreach (var target in dealDamageGA.Targets)
        {
            int modifiedDamage = CalculateModifiedDamage(dealDamageGA.Amount, dealDamageGA.Caster, target);
            target.Damage(modifiedDamage);
            SpawnDamageVFX(target);
            yield return new WaitForSeconds(0.15f);
            if (target.CurrentHealth == 0 && target is EnemyView)
            {
                KillEnemyGA killEnemyGA = new((EnemyView)target);
                ActionSystem.Instance.AddReaction(killEnemyGA);
            }
        }
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
            DealDamageGA innerGA = new(modifiedDamage, new List<CombatantView> { target }, ga.Caster);
            ActionSystem.Instance.AddReaction(innerGA);
            Debug.Log($"[DamageSystem] {target.name} 护甲 {armorStacks} → 伤害 {modifiedDamage}");
            yield return null;
        }
    }

    private int CalculateModifiedDamage(int baseDamage, CombatantView caster, CombatantView target)
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

        return Mathf.FloorToInt(damage);
    }

    private void SpawnDamageVFX(CombatantView target)
    {
        if (damageVFX != null)
        {
            Vector3 pos = new Vector3(target.transform.position.x, target.transform.position.y + 1, target.transform.position.z);
            GameObject vfxInstance = Instantiate(damageVFX, pos, Quaternion.identity);
        }
    }
}
