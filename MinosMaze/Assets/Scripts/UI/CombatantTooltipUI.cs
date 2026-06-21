using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(Canvas))]
[RequireComponent(typeof(CanvasScaler))]
public class CombatantTooltipUI : MonoBehaviour
{
    [SerializeField] private StatusEffectTooltipEntry entryPrefab;
    [SerializeField] private Image background;

    private Canvas canvas;
    private readonly List<StatusEffectTooltipEntry> entries = new();
    private static StatusEffectTooltipEntry defaultEntryPrefab;

    void Awake()
    {
        if (entryPrefab != null)
            defaultEntryPrefab = entryPrefab;
        else if (defaultEntryPrefab != null)
            entryPrefab = defaultEntryPrefab;

        canvas = GetComponent<Canvas>();
        if (canvas.renderMode != RenderMode.WorldSpace)
        {
            Debug.LogWarning($"[CombatantTooltipUI] Canvas 渲染模式自动修正: {canvas.renderMode} → WorldSpace");
            canvas.renderMode = RenderMode.WorldSpace;
        }
        canvas.overrideSorting = true;
        canvas.sortingOrder = 101;

        var scaler = GetComponent<CanvasScaler>();
        scaler.dynamicPixelsPerUnit = 100f;

        var rect = GetComponent<RectTransform>();
        if (rect != null && (rect.sizeDelta.x <= 1f || rect.sizeDelta.y <= 1f))
            rect.sizeDelta = new Vector2(3f, 2f);

        var layout = GetComponent<VerticalLayoutGroup>();
        if (layout == null)
            layout = gameObject.AddComponent<VerticalLayoutGroup>();
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.spacing = 4f;
        layout.padding = new RectOffset(8, 8, 8, 8);
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        var fitter = GetComponent<ContentSizeFitter>();
        if (fitter == null)
            fitter = gameObject.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        if (background != null)
        {
            var bgLE = background.GetComponent<LayoutElement>();
            if (bgLE == null)
                bgLE = background.gameObject.AddComponent<LayoutElement>();
            bgLE.ignoreLayout = true;

            var bgRect = background.rectTransform;
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;
            background.transform.SetAsFirstSibling();
        }

        Debug.Log($"[CombatantTooltipUI] Awake 完成, renderMode={canvas.renderMode}, sortingOrder={canvas.sortingOrder}, dppu={scaler.dynamicPixelsPerUnit}, sizeDelta={rect.sizeDelta}");
    }

    public void Populate(CombatantView combatant)
    {
        if (entryPrefab == null && defaultEntryPrefab != null)
            entryPrefab = defaultEntryPrefab;

        if (entryPrefab == null)
        {
            Debug.LogWarning($"[CombatantTooltipUI] entryPrefab 未设置且无默认值，无法生成条目。请在预制体中为 {name} 的 entryPrefab 字段赋值");
            return;
        }

        foreach (Transform child in transform)
        {
            if (background != null && child == background.transform) continue;
            Destroy(child.gameObject);
        }
        entries.Clear();

        var effects = combatant.GetStatusEffects();
        if (effects.Count == 0)
        {
            if (background != null) background.enabled = false;
            return;
        }

        if (background != null)
            background.enabled = true;

        foreach (var kvp in effects)
        {
            GameObject obj = Instantiate(entryPrefab.gameObject, transform);
            StatusEffectTooltipEntry entry = obj.GetComponent<StatusEffectTooltipEntry>();
            if (entry == null)
            {
                Debug.LogError($"[CombatantTooltipUI] entryPrefab 实例缺少 StatusEffectTooltipEntry 组件");
                Destroy(obj);
                continue;
            }

            var entryRect = entry.GetComponent<RectTransform>();
            if (entryRect != null)
            {
                if (entryRect.sizeDelta.x > 10f || entryRect.sizeDelta.y > 10f)
                    entryRect.localScale = new Vector3(0.01f, 0.01f, 1f);
                else
                    entryRect.localScale = Vector3.one;
            }

            Sprite sprite = combatant.GetStatusEffectSprite(kvp.Key);
            string name = StatusEffectData.GetName(kvp.Key);
            string desc = StatusEffectData.GetDescription(kvp.Key, kvp.Value);

            entry.Set(sprite, name, desc);
            entries.Add(entry);
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(GetComponent<RectTransform>());

        Debug.Log($"[CombatantTooltipUI] Populate: {combatant.name}, 状态效果 {effects.Count} 个, entryCount={entries.Count}, canvasSize={GetComponent<RectTransform>().sizeDelta}");
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
