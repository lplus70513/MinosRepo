using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class StatusEffectVFXEntry
{
    public StatusEffectType type;
    public GameObject vfx;
}

public class StatusEffectSystem : MonoBehaviour
{
    [SerializeField] private List<StatusEffectVFXEntry> statusEffectVFXList = new();

    private static Camera camera3D;
    private Dictionary<StatusEffectType, GameObject> vfxLookup;

    private void OnEnable()
    {
        BuildVFXLookup();
        ActionSystem.AttachPerformer<AddStatusEffectGA>(AddStatusEffectPerformer);
        ActionSystem.AttachPerformer<DoubleStatusGA>(DoubleStatusPerformer);
        ActionSystem.AttachPerformer<ApplySpikedShieldGA>(ApplySpikedShieldPerformer);
        ActionSystem.SubscribeReaction<DealDamageGA>(OnDealDamagePost, ReactionTiming.POST);
        ActionSystem.SubscribeReaction<DealDamageGA>(OnThornsDamage, ReactionTiming.POST);
    }

    private void OnDisable()
    {
        ActionSystem.DetachPerformer<AddStatusEffectGA>();
        ActionSystem.DetachPerformer<DoubleStatusGA>();
        ActionSystem.DetachPerformer<ApplySpikedShieldGA>();
        ActionSystem.UnsubscribeReaction<DealDamageGA>(OnDealDamagePost, ReactionTiming.POST);
        ActionSystem.UnsubscribeReaction<DealDamageGA>(OnThornsDamage, ReactionTiming.POST);
    }

    private IEnumerator AddStatusEffectPerformer(AddStatusEffectGA addStatusEffectGA)
    {
        foreach (var target in addStatusEffectGA.Targets)
        {
            target.AddStatusEffect(addStatusEffectGA.StatusEffectType, addStatusEffectGA.StackCount);

            SpawnStatusEffectVFX(target, addStatusEffectGA.StatusEffectType);

            if (addStatusEffectGA.StatusEffectType == StatusEffectType.ROOT
                || addStatusEffectGA.StatusEffectType == StatusEffectType.STUN)
            {
                ActionSystem.Instance.CancelCurrentFlow();
            }

            yield return null;
        }
    }

    private IEnumerator DoubleStatusPerformer(DoubleStatusGA doubleStatusGA)
    {
        foreach (var target in doubleStatusGA.Targets)
        {
            int currentStacks = target.GetStatusEffectStacks(doubleStatusGA.StatusEffectType);
            if (currentStacks > 0)
            {
                int doubled = currentStacks * 2;
                target.SetStatusEffectStacks(doubleStatusGA.StatusEffectType, doubled);
            }
            yield return null;
        }
    }

    private IEnumerator ApplySpikedShieldPerformer(ApplySpikedShieldGA ga)
    {
        HeroView hero = HeroSystem.Instance?.HeroView;
        if (hero != null)
        {
            hero.ThornsDamage = ga.DamageAmount;
        }
        yield return null;
    }

    private void OnThornsDamage(DealDamageGA dealDamageGA)
    {
        if (dealDamageGA.Caster is not EnemyView attacker) return;

        HeroView hero = HeroSystem.Instance?.HeroView;
        if (hero == null || hero.ThornsDamage <= 0) return;
        if (!dealDamageGA.Targets.Contains(hero)) return;

        DealDamageGA thornsGA = new(hero.ThornsDamage, 1, new List<CombatantView> { attacker }, hero);
        ActionSystem.Instance.AddReaction(thornsGA);
    }

    private void OnDealDamagePost(DealDamageGA dealDamageGA)
    {
        if (dealDamageGA.Caster == null) return;
        if (!dealDamageGA.Caster.HasStatusEffect(StatusEffectType.CHAIN_LIGHTNING)) return;

        var enemies = EnemySystem.Instance?.Enemies;
        if (enemies == null || enemies.Count == 0) return;

        var originalTargets = new HashSet<EnemyView>();
        foreach (var t in dealDamageGA.Targets)
        {
            if (t is EnemyView ev) originalTargets.Add(ev);
        }

        var chainTargets = new List<CombatantView>();

        foreach (var enemy in enemies)
        {
            if (enemy == null) continue;
            if (originalTargets.Contains(enemy)) continue;
            chainTargets.Add(enemy);
            if (chainTargets.Count >= 2) break;
        }

        if (chainTargets.Count < 2)
        {
            foreach (var enemy in enemies)
            {
                if (enemy == null) continue;
                if (chainTargets.Contains(enemy)) continue;
                chainTargets.Add(enemy);
                if (chainTargets.Count >= 2) break;
            }
        }

        foreach (var target in chainTargets)
        {
            DealDamageGA chainGA = new(4, 1, new List<CombatantView> { target }, null);
            ActionSystem.Instance.AddReaction(chainGA);
        }
    }

    private void BuildVFXLookup()
    {
        vfxLookup = new Dictionary<StatusEffectType, GameObject>();
        foreach (var entry in statusEffectVFXList)
        {
            if (entry.vfx != null && !vfxLookup.ContainsKey(entry.type))
                vfxLookup[entry.type] = entry.vfx;
        }
    }

    private void SpawnStatusEffectVFX(CombatantView target, StatusEffectType type)
    {
        if (target == null) return;
        if (vfxLookup == null || !vfxLookup.TryGetValue(type, out var vfxPrefab) || vfxPrefab == null) return;

        if (camera3D == null)
        {
            var camObj = GameObject.FindGameObjectWithTag("3D Camera");
            if (camObj != null)
                camera3D = camObj.GetComponent<Camera>();
        }

        Vector3 pos = new Vector3(target.transform.position.x, target.transform.position.y + 1, target.transform.position.z);
        Quaternion rot = camera3D != null ? camera3D.transform.rotation : Quaternion.identity;
        GameObject vfx = Instantiate(vfxPrefab, pos, rot);

        vfx.layer = LayerMask.NameToLayer("Combatant");
        var renderers = vfx.GetComponentsInChildren<Renderer>();
        foreach (var r in renderers)
        {
            r.sortingOrder = 32767;
            r.material.renderQueue = 5000;
        }
    }
}