using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

[RequireComponent(typeof(Canvas))]
public class UIEffectView : MonoBehaviour
{
    [SerializeField] private UIEffectEntry entryPrefab;
    [SerializeField] private Transform leftColumn;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private float fadeDuration = 0.2f;
    [SerializeField] private Vector2 screenOffset = new Vector2(-20f, -20f);

    [Header("状态图标")]
    [SerializeField] private Sprite armorSprite;
    [SerializeField] private Sprite bleedSprite;
    [SerializeField] private Sprite strengthSprite;
    [SerializeField] private Sprite weaknessSprite;
    [SerializeField] private Sprite vulnerableSprite;
    [SerializeField] private Sprite fortifySprite;
    [SerializeField] private Sprite fragileSprite;
    [SerializeField] private Sprite agileSprite;
    [SerializeField] private Sprite slowSprite;
    [SerializeField] private Sprite chainLightningSprite;
    [SerializeField] private Sprite rootSprite;
    [SerializeField] private Sprite stunSprite;
    [SerializeField] private Sprite blockSprite;
    [SerializeField] private Sprite flyingSprite;
    [SerializeField] private Sprite defaultPerkSprite;

    private Canvas canvas;
    private bool isVisible;
    private readonly List<UIEffectEntry> activeEntries = new();

    void Awake()
    {
        canvas = GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        var rect = GetComponent<RectTransform>();
        rect.localScale = Vector3.one;
        rect.pivot = new Vector2(1f, 1f);
        rect.anchorMin = new Vector2(1f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.anchoredPosition = screenOffset;

        var scaler = GetComponent<CanvasScaler>();
        if (scaler == null)
            scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
        scaler.scaleFactor = 1f;

        if (canvasGroup != null)
            canvasGroup.alpha = 0f;

        var vlg = GetComponent<VerticalLayoutGroup>();
        if (vlg == null) vlg = gameObject.AddComponent<VerticalLayoutGroup>();
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;
        vlg.childForceExpandWidth = false;
        vlg.childForceExpandHeight = false;
        vlg.padding = new RectOffset(6, 6, 6, 6);
        vlg.spacing = 4f;

        var fitter = GetComponent<ContentSizeFitter>();
        if (fitter == null) fitter = gameObject.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        FixChildLayouts();
    }

    private void FixChildLayouts()
    {
        if (transform.childCount == 0) return;
        var panelBg = transform.GetChild(0);
        var bgRect = panelBg.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.anchoredPosition = Vector2.zero;
        bgRect.sizeDelta = Vector2.zero;
        var img = panelBg.GetComponent<Image>();
        if (img != null) img.enabled = false;
    }

    public void Populate(IReadOnlyDictionary<StatusEffectType, int> statuses, IReadOnlyList<Perk> perks)
    {
        ClearEntries();

        int totalEntries = (statuses?.Count ?? 0) + (perks?.Count ?? 0);
        Debug.Log($"[UIEffectView] Populate: statuses={statuses?.Count ?? 0}, perks={perks?.Count ?? 0}, total={totalEntries}");
        if (totalEntries == 0) return;

        if (statuses != null)
        {
            foreach (var kvp in statuses)
            {
                if (kvp.Value <= 0) continue;
                Sprite sprite = GetSpriteForStatus(kvp.Key);
                string name = StatusEffectData.GetName(kvp.Key);
                string desc = StatusEffectData.GetDescription(kvp.Key, kvp.Value);
                AddEntry(sprite, name, kvp.Value, desc);
            }
        }

        if (perks != null)
        {
            foreach (var perk in perks)
            {
                Sprite sprite = perk.Image != null ? perk.Image : defaultPerkSprite;
                string name = string.IsNullOrEmpty(perk.Name) ? "未知能力" : perk.Name;
                string desc = string.IsNullOrEmpty(perk.Description) ? "" : perk.Description;
                AddEntry(sprite, name, 0, desc);
            }
        }

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)transform);
    }

    private void AddEntry(Sprite icon, string name, int stacks, string desc)
    {
        if (leftColumn == null || entryPrefab == null) return;

        UIEffectEntry entry = Instantiate(entryPrefab, leftColumn);
        entry.Set(icon, name, stacks, desc);
        activeEntries.Add(entry);
    }

    public void Show()
    {
        if (isVisible) return;
        isVisible = true;
        Debug.Log($"[UIEffectView] Show() canvasGroup={canvasGroup != null}");
        if (canvasGroup != null)
        {
            canvasGroup.DOKill();
            canvasGroup.DOFade(1f, fadeDuration);
        }
    }

    public void Hide()
    {
        if (!isVisible) return;
        isVisible = false;
        if (canvasGroup != null)
        {
            canvasGroup.DOKill();
            canvasGroup.DOFade(0f, fadeDuration);
        }
    }

    private void ClearEntries()
    {
        foreach (var entry in activeEntries)
        {
            if (entry != null)
                Destroy(entry.gameObject);
        }
        activeEntries.Clear();

        if (leftColumn != null)
        {
            foreach (Transform child in leftColumn)
                Destroy(child.gameObject);
        }
    }

    private Sprite GetSpriteForStatus(StatusEffectType type)
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
            StatusEffectType.BLOCK => blockSprite,
            StatusEffectType.FLYING => flyingSprite,
            _ => null,
        };
    }
}
