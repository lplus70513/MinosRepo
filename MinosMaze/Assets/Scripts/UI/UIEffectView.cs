using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class UIEffectView : MonoBehaviour
{
    [SerializeField] private UIEffectEntry entryPrefab;
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

    private static UIEffectView instance;
    private bool isVisible;
    private readonly List<UIEffectEntry> activeEntries = new();

    public static UIEffectView Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<UIEffectView>();
                if (instance == null)
                {
                    Debug.Log("[UIEffectView] 场景中未找到面板，自动创建");
                    var go = new GameObject("UIEffectView");
                    go.AddComponent<RectTransform>();
                    var canvas = go.AddComponent<Canvas>();
                    canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                    canvas.sortingOrder = 100;
                    go.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
                    var cg = go.AddComponent<CanvasGroup>();
                    cg.alpha = 0f;
                    instance = go.AddComponent<UIEffectView>();
                    instance.canvasGroup = cg;
                    DontDestroyOnLoad(go);
                }
            }
            return instance;
        }
    }

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;

        var rect = GetComponent<RectTransform>();
        rect.pivot = new Vector2(1f, 1f);
        rect.anchorMin = new Vector2(1f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.anchoredPosition = screenOffset;
        rect.localScale = Vector3.one;

        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        var vlg = GetComponent<VerticalLayoutGroup>();
        if (vlg == null)
        {
            vlg = gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = false;
            vlg.childForceExpandHeight = false;
            vlg.padding = new RectOffset(8, 8, 8, 8);
            vlg.spacing = 4f;
        }

        var fitter = GetComponent<ContentSizeFitter>();
        if (fitter == null)
        {
            fitter = gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }

        if (canvasGroup != null)
            canvasGroup.alpha = 0f;
    }

    public static void Populate(IReadOnlyDictionary<StatusEffectType, int> statuses, IReadOnlyList<Perk> perks)
    {
        var self = Instance;
        if (self == null) { Debug.LogError("[UIEffectView] Populate: Instance 为空！"); return; }
        self.ClearEntries();

        int count = (statuses?.Count ?? 0) + (perks?.Count ?? 0);
        Debug.Log($"[UIEffectView] Populate: statuses={statuses?.Count ?? 0}, perks={perks?.Count ?? 0}, total={count}, entryPrefab={self.entryPrefab != null}");
        if (count == 0) return;

        if (statuses != null)
        {
            foreach (var kvp in statuses)
            {
                if (kvp.Value <= 0) continue;
                var sprite = self.GetSpriteForStatus(kvp.Key);
                var name = StatusEffectData.GetName(kvp.Key);
                var desc = StatusEffectData.GetDescription(kvp.Key, kvp.Value);
                self.AddEntry(sprite, name, kvp.Value, desc);
            }
        }

        if (perks != null)
        {
            foreach (var perk in perks)
            {
                var sprite = perk.Image != null ? perk.Image : self.defaultPerkSprite;
                var name = string.IsNullOrEmpty(perk.Name) ? "未知能力" : perk.Name;
                var desc = string.IsNullOrEmpty(perk.Description) ? "" : perk.Description;
                self.AddEntry(sprite, name, 0, desc);
            }
        }

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)self.transform);
    }

    private void AddEntry(Sprite icon, string name, int stacks, string desc)
    {
        if (entryPrefab == null) return;
        UIEffectEntry entry = Instantiate(entryPrefab, transform);
        entry.Set(icon, name, stacks, desc);
        activeEntries.Add(entry);
    }

    public static void Show()
    {
        var self = Instance;
        if (self == null) return;
        if (self.isVisible) return;
        self.isVisible = true;
        Debug.Log($"[UIEffectView] Show: entryCount={self.activeEntries.Count}, canvasGroup={self.canvasGroup != null}, alpha={self.canvasGroup?.alpha}");
        if (self.canvasGroup != null)
        {
            self.canvasGroup.DOKill();
            self.canvasGroup.DOFade(1f, self.fadeDuration);
        }
    }

    public static void Hide()
    {
        var self = Instance;
        if (!self.isVisible) return;
        self.isVisible = false;
        if (self.canvasGroup != null)
        {
            self.canvasGroup.DOKill();
            self.canvasGroup.DOFade(0f, self.fadeDuration);
        }
    }

    private void ClearEntries()
    {
        foreach (var entry in activeEntries)
        {
            if (entry != null) Destroy(entry.gameObject);
        }
        activeEntries.Clear();

        foreach (Transform child in transform)
        {
            if (child != null) Destroy(child.gameObject);
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
