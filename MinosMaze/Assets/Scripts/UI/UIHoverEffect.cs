using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using DG.Tweening;

public class UIHoverEffect : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private float hoverScale = 1.08f;
    [SerializeField] private float hoverDuration = 0.1f;
    [SerializeField] private Color outlineColor = Color.white;
    [SerializeField] private float outlineWidth = 3f;
    [SerializeField, Range(0f, 1f)] private float alphaHitThreshold = 0.5f;

    private Vector3 originalScale;
    private Tween hoverTween;
    private Image targetImage;
    private Material originalMaterial;
    private Material outlineMaterial;

    void Awake()
    {
        originalScale = transform.localScale;

        targetImage = GetComponent<Image>();
        if (targetImage == null) return;

        targetImage.alphaHitTestMinimumThreshold = alphaHitThreshold;
        originalMaterial = targetImage.material;

        Shader shader = Shader.Find("Custom/UIOutline");
        if (shader == null)
        {
            Debug.LogError("[UIHoverEffect] 未找到 Custom/UIOutline Shader");
            return;
        }

        outlineMaterial = new Material(shader);
        outlineMaterial.SetColor("_OutlineColor", outlineColor);
        outlineMaterial.SetFloat("_OutlineWidth", outlineWidth);
        outlineMaterial.SetFloat("_EnableOutline", 0f);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        hoverTween?.Kill();
        hoverTween = transform.DOScale(originalScale * hoverScale, hoverDuration)
            .SetEase(Ease.OutQuad)
            .SetUpdate(true);

        if (targetImage != null && outlineMaterial != null)
        {
            outlineMaterial.SetFloat("_EnableOutline", 1f);
            targetImage.material = outlineMaterial;
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        hoverTween?.Kill();
        hoverTween = transform.DOScale(originalScale, hoverDuration)
            .SetEase(Ease.OutQuad)
            .SetUpdate(true);

        if (targetImage != null)
            targetImage.material = originalMaterial;
    }

    void OnDestroy()
    {
        hoverTween?.Kill();
        if (outlineMaterial != null)
            Destroy(outlineMaterial);
    }
}
