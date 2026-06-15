using UnityEngine;
using DG.Tweening;

public class HoverEffect3D : MonoBehaviour
{
    [SerializeField] private float hoverScale = 1.08f;
    [SerializeField] private float hoverDuration = 0.1f;
    [SerializeField] private Color outlineColor = Color.white;
    [SerializeField] private float outlineWidth = 3f;

    private Transform scaleTarget;
    private Vector3 originalScale;
    private SpriteRenderer[] targetRenderers;
    private Material[] originalMaterials;
    private Material[] outlineMaterials;
    private Tween hoverTween;
    private bool initialized;

    public void Init(Transform scaleTarget, SpriteRenderer[] renderers)
    {
        this.scaleTarget = scaleTarget;
        originalScale = scaleTarget.localScale;
        targetRenderers = renderers;

        Shader outlineShader = Shader.Find("Custom/SpriteOutline");
        if (outlineShader == null)
        {
            Debug.LogError("[HoverEffect3D] 未找到 Custom/SpriteOutline Shader");
            return;
        }

        originalMaterials = new Material[renderers.Length];
        outlineMaterials = new Material[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
        {
            originalMaterials[i] = renderers[i].sharedMaterial;
            Material mat = new Material(outlineShader);
            mat.SetColor("_Color", originalMaterials[i].GetColor("_Color"));
            mat.SetColor("_OutlineColor", outlineColor);
            mat.SetFloat("_OutlineWidth", outlineWidth);
            mat.SetFloat("_EnableOutline", 0f);
            outlineMaterials[i] = mat;
        }

        initialized = true;
    }

    void OnMouseEnter()
    {
        if (!initialized) return;

        for (int i = 0; i < targetRenderers.Length; i++)
        {
            if (targetRenderers[i] == null) continue;
            outlineMaterials[i].SetFloat("_EnableOutline", 1f);
            targetRenderers[i].material = outlineMaterials[i];
        }

        hoverTween?.Kill();
        hoverTween = scaleTarget.DOScale(originalScale * hoverScale, hoverDuration)
            .SetEase(Ease.OutQuad)
            .SetUpdate(true);
    }

    void OnMouseExit()
    {
        if (!initialized) return;

        for (int i = 0; i < targetRenderers.Length; i++)
        {
            if (targetRenderers[i] == null) continue;
            targetRenderers[i].sharedMaterial = originalMaterials[i];
        }

        hoverTween?.Kill();
        hoverTween = scaleTarget.DOScale(originalScale, hoverDuration)
            .SetEase(Ease.OutQuad)
            .SetUpdate(true);
    }

    void OnDestroy()
    {
        hoverTween?.Kill();
        if (outlineMaterials != null)
        {
            for (int i = 0; i < outlineMaterials.Length; i++)
            {
                if (outlineMaterials[i] != null)
                    Destroy(outlineMaterials[i]);
            }
        }
    }
}
