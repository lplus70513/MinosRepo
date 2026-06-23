using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 纸牌燃烧消融控制器。
/// 挂载到需要燃烧的卡牌 GameObject 上，驱动 Shader 的 _BurnAmount 从 0 → 1，
/// 同时联动粒子系统模拟火焰与烟雾效果。
/// </summary>
public class BurnController : MonoBehaviour
{
    [Header("燃烧配置")]
    [SerializeField] private Material burnMaterial;
    [SerializeField] private float burnDuration = 1.5f;
    [SerializeField] private bool destroyOnComplete = true;

    [Header("粒子系统")]
    [SerializeField] private GameObject flameParticlePrefab;
    [SerializeField] private GameObject smokeParticlePrefab;

    [Header("粒子联动参数")]
    [SerializeField] private float maxFlameRate = 30f;
    [SerializeField] private float maxSmokeRate = 10f;
    [SerializeField] private Vector3 particleOffset = new Vector3(0f, -2f, 0f);

    /// <summary>当前燃烧进度（0~1），只读，由 Update 驱动</summary>
    public float BurnAmount { get; private set; }

    /// <summary>燃烧完成后的回调</summary>
    public event Action OnBurnComplete;

    private SpriteRenderer[] spriteRenderers;
    private Material[] burnMaterialInstances;
    private Dictionary<SpriteRenderer, Material[]> originalMaterials;

    private ParticleSystem flameParticle;
    private ParticleSystem smokeParticle;
    private ParticleSystem.EmissionModule flameEmission;
    private ParticleSystem.EmissionModule smokeEmission;

    private float burnTimer;
    private bool isBurning;
    private bool isComplete;

    // ── 以下是 Inspector 参数设置指南中引用的可选配置 ──

    /// <summary>
    /// [可选] 运行时自动创建火焰/烟雾粒子系统（无需在 Prefab 上手动配置）。
    /// 传入 null 则不创建对应粒子。
    /// </summary>
    public static (ParticleSystem flame, ParticleSystem smoke) CreateDefaultParticles(
        Transform parent, Vector3 offset, Material flameMat, Material smokeMat)
    {
        ParticleSystem flame = null;
        ParticleSystem smoke = null;

        if (flameMat != null)
        {
            GameObject flameGo = new GameObject("FlameParticles");
            flameGo.transform.SetParent(parent, false);
            flameGo.transform.localPosition = offset;
            flame = flameGo.AddComponent<ParticleSystem>();
            ConfigureFlameParticleSystem(flame, flameMat);
        }

        if (smokeMat != null)
        {
            GameObject smokeGo = new GameObject("SmokeParticles");
            smokeGo.transform.SetParent(parent, false);
            smokeGo.transform.localPosition = offset + Vector3.up * 1.5f;
            smoke = smokeGo.AddComponent<ParticleSystem>();
            ConfigureSmokeParticleSystem(smoke, smokeMat);
        }

        return (flame, smoke);
    }

    void Awake()
    {
        // 缓存所有 SpriteRenderer，记录原始材质以便后续还原
        spriteRenderers = GetComponentsInChildren<SpriteRenderer>(true);
        originalMaterials = new Dictionary<SpriteRenderer, Material[]>();

        if (spriteRenderers.Length == 0)
        {
            Debug.LogWarning("[BurnController] 未找到任何 SpriteRenderer，燃烧效果可能无效。");
        }
    }

    /// <summary>开始燃烧。</summary>
    public void StartBurn()
    {
        if (isBurning || isComplete) return;

        // 延迟初始化材质实例与粒子（Configure 可能在 Awake 之后、本方法之前调用）
        if (burnMaterialInstances == null)
        {
            burnMaterialInstances = new Material[spriteRenderers.Length];
            for (int i = 0; i < spriteRenderers.Length; i++)
            {
                originalMaterials[spriteRenderers[i]] = spriteRenderers[i].sharedMaterials;

                if (burnMaterial != null)
                {
                    burnMaterialInstances[i] = new Material(burnMaterial);
                    burnMaterialInstances[i].SetFloat("_BurnAmount", 0f);
                }
            }
            SetupParticles();
        }

        // 替换所有 SpriteRenderer 的材质
        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            if (burnMaterialInstances[i] != null)
            {
                var mats = new Material[spriteRenderers[i].sharedMaterials.Length];
                for (int j = 0; j < mats.Length; j++)
                    mats[j] = burnMaterialInstances[i];
                spriteRenderers[i].sharedMaterials = mats;
            }
        }

        // 启动粒子发射
        if (flameParticle != null)
        {
            flameParticle.Play();
            flameEmission = flameParticle.emission;
            flameEmission.rateOverTime = 0f;
        }
        if (smokeParticle != null)
        {
            smokeParticle.Play();
            smokeEmission = smokeParticle.emission;
            smokeEmission.rateOverTime = 0f;
        }

        burnTimer = 0f;
        BurnAmount = 0f;
        isBurning = true;
        isComplete = false;
    }

    /// <summary>运行时配置燃烧参数（在 StartBurn 之前调用）</summary>
    public void Configure(Material burnMat, float duration,
        GameObject flamePrefab = null, GameObject smokePrefab = null, bool autoDestroy = true)
    {
        burnMaterial = burnMat;
        burnDuration = duration;
        flameParticlePrefab = flamePrefab;
        smokeParticlePrefab = smokePrefab;
        destroyOnComplete = autoDestroy;
    }

    void Update()
    {
        if (!isBurning || isComplete) return;

        burnTimer += Time.deltaTime;
        BurnAmount = Mathf.Clamp01(burnTimer / burnDuration);

        // 更新所有材质实例的消融进度
        for (int i = 0; i < burnMaterialInstances.Length; i++)
        {
            if (burnMaterialInstances[i] != null)
                burnMaterialInstances[i].SetFloat("_BurnAmount", BurnAmount);
        }

        // 粒子发射率随燃烧进度变化（中段最强，起止较弱）
        float flameRate = BurnAmount < 0.9f ? maxFlameRate * Mathf.Sin(BurnAmount * Mathf.PI) : maxFlameRate * (1f - BurnAmount) * 10f;
        if (flameParticle != null)
            flameEmission.rateOverTime = flameRate;

        float smokeRate = maxSmokeRate * BurnAmount;
        if (smokeParticle != null)
            smokeEmission.rateOverTime = smokeRate;

        // 燃烧完成
        if (BurnAmount >= 1f && !isComplete)
        {
            OnBurnFinished();
        }
    }

    private void OnBurnFinished()
    {
        isComplete = true;
        isBurning = false;

        // 停止火焰粒子发射，让已有粒子自然消散
        if (flameParticle != null)
        {
            flameParticle.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }
        if (smokeParticle != null)
        {
            smokeParticle.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }

        OnBurnComplete?.Invoke();

        if (destroyOnComplete)
            StartCoroutine(DestroyAfterParticles());
    }

    /// <summary>等待粒子消散后销毁 GameObject（默认 3 秒超时）</summary>
    private IEnumerator DestroyAfterParticles()
    {
        float maxWait = 3f;
        float elapsed = 0f;

        while (elapsed < maxWait)
        {
            bool flameAlive = flameParticle != null && flameParticle.IsAlive(true);
            bool smokeAlive = smokeParticle != null && smokeParticle.IsAlive(true);
            if (!flameAlive && !smokeAlive)
                break;
            elapsed += Time.deltaTime;
            yield return null;
        }

        Destroy(gameObject);
    }

    /// <summary>停止燃烧并还原材质（如果尚未销毁）</summary>
    public void StopBurnAndRestore()
    {
        isBurning = false;

        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            if (spriteRenderers[i] != null && originalMaterials.TryGetValue(spriteRenderers[i], out var mats))
                spriteRenderers[i].sharedMaterials = mats;
        }

        if (flameParticle != null)
            flameParticle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        if (smokeParticle != null)
            smokeParticle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    private void SetupParticles()
    {
        // 优先使用外部拖入的粒子 Prefab
        if (flameParticlePrefab != null)
        {
            GameObject flameGo = Instantiate(flameParticlePrefab, transform);
            flameGo.transform.localPosition = particleOffset;
            flameParticle = flameGo.GetComponent<ParticleSystem>();
            if (flameParticle == null)
                flameParticle = flameGo.GetComponentInChildren<ParticleSystem>();
        }
        if (smokeParticlePrefab != null)
        {
            GameObject smokeGo = Instantiate(smokeParticlePrefab, transform);
            smokeGo.transform.localPosition = particleOffset + Vector3.up * 1.5f;
            smokeParticle = smokeGo.GetComponent<ParticleSystem>();
            if (smokeParticle == null)
                smokeParticle = smokeGo.GetComponentInChildren<ParticleSystem>();
        }
    }

    void OnDestroy()
    {
        // 清理运行时创建的材质实例
        if (burnMaterialInstances != null)
        {
            foreach (var mat in burnMaterialInstances)
            {
                if (mat != null)
                    Destroy(mat);
            }
        }
    }

    // ════════════════════════════════════════════════════════════
    // ParticleSystem 参数配置参考
    // ════════════════════════════════════════════════════════════

    #region 火焰粒子系统默认配置

    /// <summary>
    /// 运行时创建默认火焰粒子系统。
    /// 对应 Inspector 配置指南中的参数。
    /// </summary>
    private static void ConfigureFlameParticleSystem(ParticleSystem ps, Material mat)
    {
        ps.GetComponent<ParticleSystemRenderer>().material = mat;

        // ── Main 模块 ──
        var main = ps.main;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.4f, 0.8f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.5f, 1.5f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.3f, 0.8f);
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(1f, 1f, 0.3f, 0.9f),
            new Color(1f, 0.3f, 0f, 0.7f));
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.scalingMode = ParticleSystemScalingMode.Hierarchy;
        main.maxParticles = 50;

        // ── Emission 模块 ──
        var emission = ps.emission;
        emission.rateOverTime = 0f; // 由脚本动态控制

        // ── Shape 模块 ── 矩形发射，覆盖卡牌宽度 ──
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(2.5f, 0.2f, 1f);
        shape.position = new Vector3(0f, -1.8f, 0f);

        // ── Color over Lifetime ── 亮黄 → 橙红 → 透明 ──
        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        colorOverLifetime.color = new ParticleSystem.MinMaxGradient(
            new Gradient()
            {
                colorKeys = new GradientColorKey[]
                {
                    new GradientColorKey(new Color(1f, 0.85f, 0.2f, 0.9f), 0.0f),
                    new GradientColorKey(new Color(1f, 0.4f, 0.05f, 0.7f), 0.4f),
                    new GradientColorKey(new Color(0.6f, 0.1f, 0f, 0.0f), 1.0f),
                },
                alphaKeys = new GradientAlphaKey[]
                {
                    new GradientAlphaKey(1f, 0.0f),
                    new GradientAlphaKey(0.8f, 0.3f),
                    new GradientAlphaKey(0f, 1.0f),
                }
            });

        // ── Size over Lifetime ── 从小到大再缩小 ──
        var sizeOverLifetime = ps.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        var sizeCurve = new AnimationCurve(
            new Keyframe(0f, 0.2f),
            new Keyframe(0.3f, 1f),
            new Keyframe(0.7f, 0.6f),
            new Keyframe(1f, 0.1f));
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

        // ── Noise 模块 ── 火焰飘动摇曳 ──
        var noise = ps.noise;
        noise.enabled = true;
        noise.strength = new ParticleSystem.MinMaxCurve(0.5f, 0.3f);
        noise.frequency = 0.8f;
        noise.scrollSpeed = 1.5f;
        noise.quality = ParticleSystemNoiseQuality.Medium;

        // ── Rotation over Lifetime ── ──
        var rotationOverLifetime = ps.rotationOverLifetime;
        rotationOverLifetime.enabled = true;
        rotationOverLifetime.z = new ParticleSystem.MinMaxCurve(-45f, 45f);
    }

    #endregion

    #region 烟雾粒子系统默认配置

    /// <summary>
    /// 运行时创建默认烟雾粒子系统。
    /// 对应 Inspector 配置指南中的参数。
    /// </summary>
    private static void ConfigureSmokeParticleSystem(ParticleSystem ps, Material mat)
    {
        ps.GetComponent<ParticleSystemRenderer>().material = mat;

        // ── Main 模块 ──
        var main = ps.main;
        main.startLifetime = new ParticleSystem.MinMaxCurve(1.5f, 3f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.2f, 0.6f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.8f, 2f);
        main.startColor = new Color(0.15f, 0.12f, 0.1f, 0.5f);
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.scalingMode = ParticleSystemScalingMode.Hierarchy;
        main.maxParticles = 30;

        // ── Emission 模块 ──
        var emission = ps.emission;
        emission.rateOverTime = 0f; // 由脚本动态控制

        // ── Shape 模块 ──
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(2.5f, 0.2f, 1f);
        shape.position = new Vector3(0f, -2f, 0f);

        // ── Color over Lifetime ── 深灰 → 透明 ──
        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        colorOverLifetime.color = new ParticleSystem.MinMaxGradient(
            new Gradient()
            {
                colorKeys = new GradientColorKey[]
                {
                    new GradientColorKey(new Color(0.15f, 0.12f, 0.1f, 0.6f), 0.0f),
                    new GradientColorKey(new Color(0.08f, 0.06f, 0.05f, 0.3f), 0.5f),
                    new GradientColorKey(new Color(0.04f, 0.03f, 0.02f, 0f), 1.0f),
                },
                alphaKeys = new GradientAlphaKey[]
                {
                    new GradientAlphaKey(0.6f, 0.0f),
                    new GradientAlphaKey(0.3f, 0.5f),
                    new GradientAlphaKey(0f, 1.0f),
                }
            });

        // ── Size over Lifetime ── 持续膨胀 ──
        var sizeOverLifetime = ps.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        var sizeCurve = new AnimationCurve(
            new Keyframe(0f, 0.5f),
            new Keyframe(1f, 2.5f));
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

        // ── Noise 模块 ── 大范围烟雾扰动 ──
        var noise = ps.noise;
        noise.enabled = true;
        noise.strength = new ParticleSystem.MinMaxCurve(0.8f, 0.4f);
        noise.frequency = 0.2f;
        noise.scrollSpeed = 0.3f;
        noise.quality = ParticleSystemNoiseQuality.Medium;

        // ── Rotation over Lifetime ── 缓慢旋转 ──
        var rotationOverLifetime = ps.rotationOverLifetime;
        rotationOverLifetime.enabled = true;
        rotationOverLifetime.z = new ParticleSystem.MinMaxCurve(-15f, 15f);
    }

    #endregion
}

// ════════════════════════════════════════════════════════════
// 使用说明（中文）
// ════════════════════════════════════════════════════════════
//
// 【快速集成（RestSite 丢弃卡牌）】
// 1. 将 CardBurnShader.shader 放入 Assets/Shaders/ 目录
// 2. 创建 Material 使用 "Custom/CardBurn" Shader（无需额外纹理）
// 3. 在 DeckViewer Inspector 中赋值 burnMaterial 引用
// 4. 在 RestSiteController 中调用 deckViewer.SetBurnMode(true) 即可
//
// 【Shader 无需噪点贴图】
// 使用程序化噪声（hash + value noise + FBM）在 GPU 上实时生成消融图案。
// 调整 Material 的 _NoiseScale（噪声密度）、_NoiseOctaves（细节层数）可改变烧蚀纹理。
//
// 【粒子系统（可选）】
// - 使用 CreateDefaultParticles() 运行时创建默认火焰+烟雾
// - 或在 Inspector 中拖入预制的 ParticleSystem Prefab 到 flameParticlePrefab / smokeParticlePrefab
//
// 【独立使用 BurnController】
// - 挂载到任意带 SpriteRenderer 的 GameObject
// - 设置 burnMaterial、burnDuration
// - 调用 StartBurn() 即可启动消融动画
//
