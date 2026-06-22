using UnityEngine;
using UnityEngine.UI;

public enum SmokeRenderLayer
{
    背景层,
    UI叠加层
}

[RequireComponent(typeof(RawImage))]
public class ProceduralSmokeEffect : MonoBehaviour
{
    [Header("烟雾设置")]
    [SerializeField] private Color smokeColor = Color.black;
    [SerializeField] [Range(0f, 1f)] private float opacity = 0.6f;
    [SerializeField] [Range(0f, 1f)] private float flowSpeed = 0.15f;
    [SerializeField] [Range(0f, 360f)] private float flowAngle = 45f;
    [SerializeField] [Range(0f, 0.5f)] private float distortion = 0.1f;
    [SerializeField] [Range(1f, 20f)] private float noiseScale = 5f;
    [SerializeField] [Range(0f, 1f)] private float edgeSoftness = 0.4f;
    [SerializeField] [Range(0f, 1f)] private float vignettePower = 0.5f;

    [Header("渲染设置")]
    [SerializeField] private SmokeRenderLayer renderLayer = SmokeRenderLayer.UI叠加层;

    private RawImage rawImage;
    private Material smokeMaterialOverlay;
    private Material smokeMaterialBackground;
    private Material smokeMaterial;
    private SmokeRenderLayer currentRenderLayer;
    private Texture2D dummyTexture;

    private void Awake()
    {
        rawImage = GetComponent<RawImage>();
        rawImage.raycastTarget = false;

        Shader shaderOverlay = Shader.Find("Custom/ProceduralSmoke");
        Shader shaderBackground = Shader.Find("Custom/ProceduralSmokeBackground");

        if (shaderOverlay == null && shaderBackground == null)
        {
            Debug.LogError("找不到 ProceduralSmoke Shader，请确认 Shader 文件存在于 Assets/Shaders/ 目录下");
            enabled = false;
            return;
        }

        if (shaderOverlay != null)
            smokeMaterialOverlay = new Material(shaderOverlay);
        if (shaderBackground != null)
            smokeMaterialBackground = new Material(shaderBackground);

        dummyTexture = new Texture2D(1, 1);
        dummyTexture.SetPixel(0, 0, Color.white);
        dummyTexture.Apply();

        rawImage.texture = dummyTexture;

        currentRenderLayer = renderLayer;
        ApplyRenderLayer();
    }

    private void Update()
    {
        if (renderLayer != currentRenderLayer)
        {
            currentRenderLayer = renderLayer;
            ApplyRenderLayer();
        }

        if (smokeMaterial == null) return;

        ApplyMaterialProperties();
    }

    private void ApplyRenderLayer()
    {
        if (rawImage == null) return;

        switch (renderLayer)
        {
            case SmokeRenderLayer.UI叠加层:
                smokeMaterial = smokeMaterialOverlay;
                break;
            case SmokeRenderLayer.背景层:
                smokeMaterial = smokeMaterialBackground;
                break;
        }

        rawImage.material = smokeMaterial;
    }

    private void ApplyMaterialProperties()
    {
        smokeMaterial.SetColor("_Color", smokeColor);
        smokeMaterial.SetFloat("_Opacity", opacity);
        smokeMaterial.SetFloat("_FlowSpeed", flowSpeed);
        smokeMaterial.SetFloat("_FlowAngle", flowAngle * Mathf.Deg2Rad);
        smokeMaterial.SetFloat("_Distortion", distortion);
        smokeMaterial.SetFloat("_NoiseScale", noiseScale);
        smokeMaterial.SetFloat("_EdgeSoftness", edgeSoftness);
        smokeMaterial.SetFloat("_VignettePower", vignettePower);
    }

    private void OnDestroy()
    {
        if (smokeMaterialOverlay != null)
        {
            if (Application.isPlaying)
                Destroy(smokeMaterialOverlay);
            else
                DestroyImmediate(smokeMaterialOverlay);
        }
        if (smokeMaterialBackground != null)
        {
            if (Application.isPlaying)
                Destroy(smokeMaterialBackground);
            else
                DestroyImmediate(smokeMaterialBackground);
        }
        if (dummyTexture != null)
        {
            if (Application.isPlaying)
                Destroy(dummyTexture);
            else
                DestroyImmediate(dummyTexture);
        }
    }
}
