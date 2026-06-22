using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RawImage))]
public class ProceduralParticleEffect : MonoBehaviour
{
    [Header("粒子设置")]
    [SerializeField] private Color particleColor = new Color(0.7f, 0.7f, 0.8f, 1f);
    [SerializeField] [Range(0f, 1f)] private float opacity = 0.8f;
    [SerializeField] [Range(0.001f, 0.04f)] private float particleSize = 0.006f;
    [SerializeField] [Range(3f, 15f)] private float cellDensity = 7f;
    [SerializeField] [Range(0f, 0.5f)] private float flowSpeed = 0.08f;
    [SerializeField] [Range(0f, 360f)] private float flowAngle = 135f;
    [SerializeField] [Range(0.1f, 1f)] private float flowDistance = 0.5f;
    [SerializeField] [Range(3f, 10f)] private float lifetimeMin = 5f;
    [SerializeField] [Range(5f, 15f)] private float lifetimeMax = 10f;
    [SerializeField] [Range(0f, 0.3f)] private float perpSpread = 0.08f;
    [SerializeField] [Range(0.1f, 1.5f)] private float edgeSoftness = 0.4f;
    [SerializeField] [Range(0f, 1f)] private float vignettePower = 0.3f;

    private RawImage rawImage;
    private Material particleMaterial;
    private Texture2D dummyTexture;
    private RectTransform rectTransform;

    private void Awake()
    {
        rawImage = GetComponent<RawImage>();
        rectTransform = GetComponent<RectTransform>();
        rawImage.raycastTarget = false;

        Shader shader = Shader.Find("Custom/ProceduralParticle");
        if (shader == null)
        {
            Debug.LogError("找不到 Custom/ProceduralParticle Shader，请确认 Shader 文件存在于 Assets/Shaders/ 目录下");
            enabled = false;
            return;
        }

        particleMaterial = new Material(shader);

        dummyTexture = new Texture2D(1, 1);
        dummyTexture.SetPixel(0, 0, Color.white);
        dummyTexture.Apply();

        rawImage.texture = dummyTexture;
        rawImage.material = particleMaterial;
    }

    private void Update()
    {
        if (particleMaterial == null) return;

        particleMaterial.SetColor("_Color", particleColor);
        particleMaterial.SetFloat("_Opacity", opacity);
        particleMaterial.SetFloat("_ParticleSize", particleSize);
        particleMaterial.SetFloat("_CellDensity", cellDensity);
        particleMaterial.SetFloat("_FlowSpeed", flowSpeed);
        particleMaterial.SetFloat("_FlowAngle", flowAngle * Mathf.Deg2Rad);
        particleMaterial.SetFloat("_FlowDistance", flowDistance);
        particleMaterial.SetFloat("_LifetimeMin", lifetimeMin);
        particleMaterial.SetFloat("_LifetimeMax", lifetimeMax);
        particleMaterial.SetFloat("_PerpSpread", perpSpread);
        particleMaterial.SetFloat("_EdgeSoftness", edgeSoftness);
        particleMaterial.SetFloat("_VignettePower", vignettePower);
        particleMaterial.SetFloat("_AspectRatio", rectTransform.rect.width / rectTransform.rect.height);
    }

    private void OnDestroy()
    {
        if (particleMaterial != null)
        {
            if (Application.isPlaying)
                Destroy(particleMaterial);
            else
                DestroyImmediate(particleMaterial);
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
