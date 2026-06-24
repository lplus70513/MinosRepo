using System.Collections.Generic;
using UnityEngine;

public class CombatantTooltipSystem : Singleton<CombatantTooltipSystem>
{
    [SerializeField] private StatusEffectSlotUI[] slots = new StatusEffectSlotUI[6];
    [SerializeField] private float hoverScreenDistance = 120f;

    [Header("状态图标")]
    [SerializeField] private Sprite armorSprite, bleedSprite;
    [SerializeField] private Sprite strengthSprite, weaknessSprite, vulnerableSprite;
    [SerializeField] private Sprite fortifySprite, fragileSprite, agileSprite, slowSprite;
    [SerializeField] private Sprite chainLightningSprite, rootSprite, stunSprite;

    private Camera camera3D;
    private CombatantView lastHovered;

    void Start()
    {
        var camObj = GameObject.FindGameObjectWithTag("3D Camera");
        if (camObj != null)
        {
            camera3D = camObj.GetComponent<Camera>();
            Debug.Log("[CombatantTooltipSystem] 已找到 3D Camera");
        }
        else
        {
            Debug.LogError("[CombatantTooltipSystem] 未找到标记为 '3D Camera' 的对象！");
        }
    }

    void Update()
    {
        if (camera3D == null)
            return;

        if (!Interactions.Instance.PlayerCanHover())
        {
            ClearAllSlots();
            lastHovered = null;
            return;
        }

        CombatantView hovered = GetHoveredCombatant();

        if (hovered != null && hovered.GetStatusEffects().Count > 0)
        {
            if (hovered != lastHovered)
            {
                PopulateSlots(hovered);
                lastHovered = hovered;
            }
        }
        else
        {
            ClearAllSlots();
            lastHovered = null;
        }
    }

    private void PopulateSlots(CombatantView combatant)
    {
        var effects = combatant.GetStatusEffects();

        int slotIndex = 0;
        foreach (var kvp in effects)
        {
            if (slotIndex >= slots.Length)
                break;

            var slot = slots[slotIndex];
            slotIndex++;

            if (slot == null)
                continue;

            Sprite sprite = GetSpriteByType(kvp.Key);
            string name = StatusEffectData.GetName(kvp.Key);
            string desc = StatusEffectData.GetDescription(kvp.Key, kvp.Value);

            slot.Populate(sprite, name, desc);
        }

        for (int i = slotIndex; i < slots.Length; i++)
        {
            if (slots[i] != null)
                slots[i].Clear();
        }
    }

    private void ClearAllSlots()
    {
        foreach (var slot in slots)
        {
            if (slot != null)
                slot.Clear();
        }
    }

    private Sprite GetSpriteByType(StatusEffectType type)
    {
        return type switch
        {
            StatusEffectType.ARMOR => armorSprite,
            StatusEffectType.BLEED => bleedSprite,
            StatusEffectType.STRENGTH => strengthSprite,
            StatusEffectType.WEAKNESS => weaknessSprite,
            StatusEffectType.VULNERABLE => vulnerableSprite,
            StatusEffectType.FORTIFY => fortifySprite,
            StatusEffectType.FRAGILE => fragileSprite,
            StatusEffectType.AGILE => agileSprite,
            StatusEffectType.SLOW => slowSprite,
            StatusEffectType.CHAIN_LIGHTNING => chainLightningSprite,
            StatusEffectType.ROOT => rootSprite,
            StatusEffectType.STUN => stunSprite,
            _ => null,
        };
    }

    private CombatantView GetHoveredCombatant()
    {
        CombatantView best = null;
        float bestDist = hoverScreenDistance;
        Vector2 mouseScreen = Input.mousePosition;

        var enemies = EnemySystem.Instance?.Enemies;
        if (enemies != null)
        {
            foreach (var enemy in enemies)
            {
                if (enemy == null) continue;
                Vector3 screenPos = camera3D.WorldToScreenPoint(enemy.transform.position);
                float dist = Vector2.Distance(mouseScreen, new Vector2(screenPos.x, screenPos.y));
                if (dist < bestDist)
                {
                    bestDist = dist;
                    best = enemy;
                }
            }
        }

        var hero = HeroSystem.Instance?.HeroView;
        if (hero != null)
        {
            Vector3 screenPos = camera3D.WorldToScreenPoint(hero.transform.position);
            float dist = Vector2.Distance(mouseScreen, new Vector2(screenPos.x, screenPos.y));
            if (dist < bestDist)
            {
                bestDist = dist;
                best = hero;
            }
        }

        return best;
    }
}
