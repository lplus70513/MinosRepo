using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PerkSystem : Singleton<PerkSystem>
{
    [SerializeField] private PerksUI perksUI;

    private readonly List<Perk> perks = new();

    void OnEnable()
    {
        ActionSystem.AttachPerformer<AddPerkGA>(AddPerkPerformer);
        ActionSystem.AttachPerformer<MultiGA>(MultiPerformer);
    }

    void OnDisable()
    {
        Debug.Log($"[PerkSystem] OnDisable — 清理 {perks.Count} 个 Perk 订阅, scene={gameObject.scene.name}");
        foreach (var perk in perks)
            perk.OnRemove();
        perks.Clear();
        ActionSystem.DetachPerformer<AddPerkGA>();
        ActionSystem.DetachPerformer<MultiGA>();
    }

    public void AddPerk(Perk perk)
    {
        perks.Add(perk);
        perksUI.AddPerkUI(perk);
        perk.OnAdd();
    }

    public void RemovePerk(Perk perk)
    {
        perks.Remove(perk);
        perksUI.RemovePerkUI(perk);
        perk.OnRemove();
    }

    private IEnumerator AddPerkPerformer(AddPerkGA ga)
    {
        if (ga.PerkData == null)
        {
            Debug.LogWarning("[PerkSystem] AddPerkGA 的 PerkData 为空");
            yield break;
        }
        Perk perk = new(ga.PerkData);
        AddPerk(perk);
        Debug.Log($"[PerkSystem] 添加 Perk: {ga.PerkData.name}");
        yield return null;
    }

    private IEnumerator MultiPerformer(MultiGA ga)
    {
        foreach (var wrapper in ga.Effects)
        {
            List<CombatantView> targets = wrapper.TargetMode?.GetTargets() ?? ga.Targets;
            GameAction effectAction = wrapper.Effect.GetGameAction(targets, ga.Caster);
            if (effectAction != null)
                ActionSystem.Instance.AddReaction(effectAction);
            yield return null;
        }
    }
}
