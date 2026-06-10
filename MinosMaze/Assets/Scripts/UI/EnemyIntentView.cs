using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

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

    private void Awake()
    {
        if (canvasGroup != null)
            canvasGroup.alpha = 0f;
    }

    public void Show(List<EnemyIntentData> intents)
    {
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
        canvasGroup.DOKill();
        canvasGroup.DOFade(0f, fadeDuration);
    }

    public void TransitionTo(List<EnemyIntentData> intents)
    {
        canvasGroup.DOKill();
        if (intents == null || intents.Count == 0)
        {
            canvasGroup.DOFade(0f, fadeDuration);
            return;
        }
        canvasGroup.DOFade(0f, fadeDuration).OnComplete(() =>
        {
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

        foreach (var intent in intents)
        {
            var obj = Instantiate(intentUIPrefab, transform);
            var ui = obj.GetComponent<EnemyIntentUI>();
            ui.SetData(intent, GetSpriteForType(intent.IntentType));
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
