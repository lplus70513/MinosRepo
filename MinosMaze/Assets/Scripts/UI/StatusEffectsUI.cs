using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

[RequireComponent(typeof(CanvasGroup))]
public class StatusEffectsUI : MonoBehaviour
{
    [SerializeField] private StatusEffectUI statusEffectUIPrefab;

    [SerializeField] private Sprite armorSprite, bleedSprite;
    [SerializeField] private Sprite strengthSprite, weaknessSprite, vulnerableSprite;
    [SerializeField] private Sprite fortifySprite, fragileSprite, agileSprite, slowSprite;
    [SerializeField] private Sprite chainLightningSprite, rootSprite, stunSprite;

    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private float fadeDuration = 0.3f;

    private Dictionary<StatusEffectType, StatusEffectUI> statusEffectUIs = new();

    private static Material uiAlwaysVisibleMaterial;
    private static Shader tmpAlwaysVisibleShader;

    private void Awake()
    {
        Canvas parentCanvas = GetComponentInParent<Canvas>();
        if (parentCanvas != null)
        {
            parentCanvas.overrideSorting = true;
            parentCanvas.sortingOrder = 100;
        }

        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
        canvasGroup.alpha = 0f;

        if (uiAlwaysVisibleMaterial == null)
        {
            Shader shader = Shader.Find("Custom/UIAlwaysVisible");
            if (shader != null)
                uiAlwaysVisibleMaterial = new Material(shader);
        }

        if (tmpAlwaysVisibleShader == null)
            tmpAlwaysVisibleShader = Shader.Find("Custom/TMPAlwaysVisible");
    }

    private void ApplyAlwaysVisibleMaterial(StatusEffectUI ui)
    {
        if (uiAlwaysVisibleMaterial != null)
        {
            var images = ui.GetComponentsInChildren<Image>();
            foreach (var img in images)
                img.material = uiAlwaysVisibleMaterial;
        }

        if (tmpAlwaysVisibleShader != null)
        {
            var texts = ui.GetComponentsInChildren<TMP_Text>();
            foreach (var tmp in texts)
            {
                var fontMat = new Material(tmp.fontMaterial);
                fontMat.shader = tmpAlwaysVisibleShader;
                tmp.fontMaterial = fontMat;
            }
        }
    }

    public void UpdateStatusEffectUI(StatusEffectType statusEffectType, int stackCount)
    {
        bool wasEmpty = statusEffectUIs.Count == 0;

        if (stackCount == 0)
        {
            if (statusEffectUIs.ContainsKey(statusEffectType))
            {
                StatusEffectUI statusEffectUI = statusEffectUIs[statusEffectType];
                statusEffectUIs.Remove(statusEffectType);

                if (statusEffectUIs.Count == 0)
                {
                    canvasGroup.DOKill();
                    canvasGroup.DOFade(0f, fadeDuration).OnComplete(() =>
                    {
                        if (statusEffectUI != null)
                            Destroy(statusEffectUI.gameObject);
                    });
                }
                else
                {
                    Destroy(statusEffectUI.gameObject);
                }
            }
        }
        else
        {
            if (!statusEffectUIs.ContainsKey(statusEffectType))
            {
                StatusEffectUI statusEffectUI = Instantiate(statusEffectUIPrefab, transform);
                ApplyAlwaysVisibleMaterial(statusEffectUI);
                statusEffectUIs.Add(statusEffectType, statusEffectUI);
            }
            Sprite sprite = GetSpriteByType(statusEffectType);
            statusEffectUIs[statusEffectType].Set(sprite, stackCount);

            if (wasEmpty)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.DOKill();
                canvasGroup.DOFade(1f, fadeDuration);
            }
        }
    }

    public Sprite GetSpriteByType(StatusEffectType statusEffectType)
    {
        return statusEffectType switch
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
}
