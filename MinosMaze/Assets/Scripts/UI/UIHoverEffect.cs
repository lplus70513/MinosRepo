using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using DG.Tweening;

public class UIHoverEffect : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ICanvasRaycastFilter
{
    [SerializeField] private float hoverScale = 1.08f;
    [SerializeField] private float hoverDuration = 0.1f;
    [SerializeField] private Color outlineColor = Color.white;
    [SerializeField] private float outlineWidth = 3f;

    [Header("射线检测扩展")]
    [SerializeField] private float hitExpandRadius = 10f;
    [SerializeField] private float hitAlphaThreshold = 0.1f;

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

        targetImage.alphaHitTestMinimumThreshold = 0f;
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

    public bool IsRaycastLocationValid(Vector2 screenPoint, Camera eventCamera)
    {
        if (targetImage == null || targetImage.sprite == null)
            return true;

        Texture2D tex = targetImage.sprite.texture;
        if (tex == null || !tex.isReadable)
            return true;

        RectTransform rt = transform as RectTransform;
        if (rt == null) return true;

        Vector2 localPoint;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rt, screenPoint, eventCamera, out localPoint))
            return false;

        Rect rect = rt.rect;
        float normalizedX = (localPoint.x - rect.x) / rect.width;
        float normalizedY = (localPoint.y - rect.y) / rect.height;

        if (normalizedX < 0f || normalizedX > 1f || normalizedY < 0f || normalizedY > 1f)
            return false;

        Rect texRect = targetImage.sprite.textureRect;
        int centerX = Mathf.RoundToInt(texRect.x + normalizedX * texRect.width);
        int centerY = Mathf.RoundToInt(texRect.y + normalizedY * texRect.height);
        int expand = Mathf.Max(0, Mathf.RoundToInt(hitExpandRadius));

        for (int dx = -expand; dx <= expand; dx++)
        {
            for (int dy = -expand; dy <= expand; dy++)
            {
                int px = centerX + dx;
                int py = centerY + dy;
                if (px < 0 || px >= tex.width || py < 0 || py >= tex.height)
                    continue;
                if (tex.GetPixel(px, py).a >= hitAlphaThreshold)
                    return true;
            }
        }

        return false;
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
