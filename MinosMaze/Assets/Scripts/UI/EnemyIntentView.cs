using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

[RequireComponent(typeof(Canvas))]
public class EnemyIntentView : MonoBehaviour
{
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private GameObject intentUIPrefab;
    [SerializeField] private float fadeDuration = 0.5f;

    [Header("意图图标")]
    [SerializeField] private Sprite attackSprite;
    [SerializeField] private Sprite moveSprite;
    [SerializeField] private Sprite defenseSprite;
    [SerializeField] private Sprite buffSprite;
    [SerializeField] private Sprite debuffSprite;

    private Canvas canvas;

    private void Awake()
    {
        canvas = GetComponent<Canvas>();
        if (canvas == null)
        {
            Debug.LogError($"[EnemyIntentView] {name} 缺少 Canvas 组件！UI 将无法渲染。");
            return;
        }

        if (canvas.renderMode != RenderMode.WorldSpace)
        {
            Debug.LogWarning($"[EnemyIntentView] {name} Canvas 渲染模式自动修正: {canvas.renderMode} → WorldSpace");
            canvas.renderMode = RenderMode.WorldSpace;
        }

        canvas.sortingOrder = 100;

        var rect = GetComponent<RectTransform>();
        if (rect != null && (rect.sizeDelta.x <= 1f || rect.sizeDelta.y <= 1f))
        {
            rect.sizeDelta = new Vector2(400f, 80f);
        }

        var layout = GetComponent<HorizontalLayoutGroup>();
        if (layout == null)
            layout = gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.spacing = 3f;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        var fitter = GetComponent<ContentSizeFitter>();
        if (fitter == null)
            fitter = gameObject.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        Debug.Log($"[EnemyIntentView] {name} 初始化, renderMode={canvas.renderMode}, sortingOrder={canvas.sortingOrder}, canvasGroup={(canvasGroup != null ? "已绑定" : "NULL")}, intentUIPrefab={(intentUIPrefab != null ? intentUIPrefab.name : "NULL")}");

        if (canvasGroup != null)
            canvasGroup.alpha = 0f;
        else
            Debug.LogError($"[EnemyIntentView] {name} 缺少 CanvasGroup 引用！");
    }

    public void Show(List<EnemyIntentData> intents)
    {
        Debug.Log($"[EnemyIntentView] {name} Show() 意图数量={intents?.Count ?? 0}");
        if (canvasGroup == null) return;
        canvasGroup.DOKill();
        if (intents == null || intents.Count == 0)
        {
            canvasGroup.DOFade(0f, fadeDuration);
            return;
        }
        RebuildChildren(intents);
        canvasGroup.DOFade(1f, fadeDuration);
    }

    public void Hide()
    {
        Debug.Log($"[EnemyIntentView] {name} Hide()");
        if (canvasGroup == null) return;
        canvasGroup.DOKill();
        canvasGroup.DOFade(0f, fadeDuration);
    }

    public void TransitionTo(List<EnemyIntentData> intents)
    {
        Debug.Log($"[EnemyIntentView] {name} TransitionTo() 意图数量={intents?.Count ?? 0}");
        if (canvasGroup == null) return;
        canvasGroup.DOKill();
        if (intents == null || intents.Count == 0)
        {
            canvasGroup.DOFade(0f, fadeDuration);
            return;
        }
        canvasGroup.DOFade(0f, fadeDuration).OnComplete(() =>
        {
            Debug.Log($"[EnemyIntentView] {name} 淡出完成，开始重建子元素");
            RebuildChildren(intents);
            canvasGroup.DOFade(1f, fadeDuration);
        });
    }

    private void RebuildChildren(List<EnemyIntentData> intents)
    {
        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }

        if (intents == null || intents.Count == 0) return;
        if (intentUIPrefab == null)
        {
            Debug.LogError($"[EnemyIntentView] {name} intentUIPrefab 未绑定！");
            return;
        }

        foreach (var intent in intents)
        {
            var obj = Instantiate(intentUIPrefab, transform);
            if (obj.GetComponent<RectTransform>() == null)
            {
                Debug.LogError($"[EnemyIntentView] {name} 实例化的预制体 {intentUIPrefab.name} 缺少 RectTransform！请确保 EnemyIntentUI 预制体是在 Canvas 下创建的（UI → Image）");
                Destroy(obj);
                continue;
            }
            var ui = obj.GetComponent<EnemyIntentUI>();
            if (ui == null)
            {
                Debug.LogError($"[EnemyIntentView] {name} 实例化的预制体 {intentUIPrefab.name} 缺少 EnemyIntentUI 脚本！");
                Destroy(obj);
                continue;
            }
            var sprite = GetSpriteForType(intent.IntentType);
            Debug.Log($"[EnemyIntentView] {name} 创建意图: type={intent.IntentType}, sprite={(sprite != null ? sprite.name : "NULL")}, hit={intent.HitCount}, value={intent.ValuePerHit}");
            ui.SetData(intent, sprite);
        }
    }

    private Sprite GetSpriteForType(EnemyActionType type)
    {
        switch (type)
        {
            case EnemyActionType.Attack: return attackSprite;
            case EnemyActionType.Move: return moveSprite;
            case EnemyActionType.Defense: return defenseSprite;
            case EnemyActionType.Buff: return buffSprite;
            case EnemyActionType.Debuff: return debuffSprite;
            default: return null;
        }
    }
}
