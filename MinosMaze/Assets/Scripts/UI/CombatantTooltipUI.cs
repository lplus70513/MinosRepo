using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Canvas))]
public class CombatantTooltipUI : MonoBehaviour
{
    [SerializeField] private StatusEffectTooltipEntry entryPrefab;
    [SerializeField] private float margin = 20f;

    [Header("状态图标")]
    [SerializeField] private Sprite armorSprite, bleedSprite;
    [SerializeField] private Sprite strengthSprite, weaknessSprite, vulnerableSprite;
    [SerializeField] private Sprite fortifySprite, fragileSprite, agileSprite, slowSprite;
    [SerializeField] private Sprite chainLightningSprite, rootSprite, stunSprite;

    private readonly List<StatusEffectTooltipEntry> entries = new();

    void Awake()
    {
        Canvas canvas = GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 101;

        var scaler = GetComponent<CanvasScaler>();
        if (scaler == null)
            scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;

        var layout = GetComponent<VerticalLayoutGroup>();
        if (layout == null)
            layout = gameObject.AddComponent<VerticalLayoutGroup>();
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.spacing = 4f;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        var fitter = GetComponent<ContentSizeFitter>();
        if (fitter == null)
            fitter = gameObject.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
    }

    public void Populate(CombatantView combatant)
    {
        foreach (Transform child in transform)
            Destroy(child.gameObject);
        entries.Clear();

        var effects = combatant.GetStatusEffects();
        if (effects.Count == 0) return;

        foreach (var kvp in effects)
        {
            StatusEffectTooltipEntry entry = Instantiate(entryPrefab, transform);
            if (entry == null) continue;

            Sprite sprite = GetSpriteByType(kvp.Key);
            string name = StatusEffectData.GetName(kvp.Key);
            string desc = StatusEffectData.GetDescription(kvp.Key, kvp.Value);

            entry.Set(null, sprite, name, desc);
            entries.Add(entry);
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(GetComponent<RectTransform>());

        Debug.Log($"[CombatantTooltipUI] Populate: {combatant.name}, 状态效果 {effects.Count} 个, entryCount={entries.Count}");
    }

    public void UpdatePosition(Vector2 mouseScreenPos)
    {
        var rect = GetComponent<RectTransform>();
        float panelWidth = rect.rect.width;
        float panelHeight = rect.rect.height;

        Vector2 pos = mouseScreenPos;

        if (mouseScreenPos.x < Screen.width * 0.5f)
            pos.x += margin;
        else
            pos.x -= panelWidth + margin;

        pos.y -= margin;

        pos.x = Mathf.Max(0f, pos.x);
        if (pos.x + panelWidth > Screen.width)
            pos.x = Screen.width - panelWidth;

        float maxY = Screen.height - panelHeight;
        pos.y = Mathf.Clamp(pos.y, 0f, maxY);

        rect.position = Vector2.Lerp(rect.position, pos, 30f * Time.deltaTime);
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

    public void Show()
    {
        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true);
            Debug.Log("[CombatantTooltipUI] Show");
        }
    }

    public void Hide()
    {
        if (gameObject.activeSelf)
        {
            gameObject.SetActive(false);
            Debug.Log("[CombatantTooltipUI] Hide");
        }
    }
}
