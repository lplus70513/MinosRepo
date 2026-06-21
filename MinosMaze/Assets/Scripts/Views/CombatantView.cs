using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using Spine;
using Spine.Unity;
using UI.PopupText;

public class CombatantView : MonoBehaviour
{
    [SerializeField] private TMP_Text healthText;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private SkeletonAnimation skeletonAnimation;

    [SerializeField] private Transform portraitRoot;

    [SerializeField] private StatusEffectsUI statusEffectsUI;

    [SerializeField] private CombatantTooltipUI tooltipUI;

    public int HexCoordX { get; set; }
    public int HexCoordZ { get; set; }

    public int MaxHealth { get; private set; }
    public int CurrentHealth { get; private set; }

    public int LastUnblockedDamage { get; private set; }

    public bool PersistArmor { get; set; } = false;

    public int ThornsDamage { get; set; }

    public event Action<CombatantView, int> OnHealthChanged;
    public event Action OnStatusChanged;

    private Dictionary<StatusEffectType, int> statusEffects = new();

    protected bool facingRight = false;

    protected static Camera camera3D;
    private static Material alwaysVisibleMaterial;
    private MaterialPropertyBlock propertyBlock;
    private static readonly int EnableOutlineId = Shader.PropertyToID("_EnableOutline");

    public static EnemyView HoveredEnemy { get; set; }

    void Awake()
    {
        if (camera3D == null)
        {
            var camObj = GameObject.FindGameObjectWithTag("3D Camera");
            if (camObj != null) camera3D = camObj.GetComponent<Camera>();
        }

        if (alwaysVisibleMaterial == null)
        {
            Shader shader = Shader.Find("Custom/SpriteAlwaysVisibleOutline");
            if (shader != null)
                alwaysVisibleMaterial = new Material(shader);
        }

        if (spriteRenderer != null && alwaysVisibleMaterial != null)
        {
            spriteRenderer.material = alwaysVisibleMaterial;
            propertyBlock = new MaterialPropertyBlock();
        }

        if (spriteRenderer != null)
        {
            spriteRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            spriteRenderer.receiveShadows = true;
        }

        BoxCollider col = GetComponent<BoxCollider>();
        if (col == null)
        {
            col = gameObject.AddComponent<BoxCollider>();
            col.isTrigger = true;
            col.size = new Vector3(2f, 3f, 1f);
            col.center = new Vector3(0f, 1.5f, 0f);
        }

        tooltipUI = GetComponentInChildren<CombatantTooltipUI>(true);
        if (tooltipUI == null)
        {
            tooltipUI = CreateTooltipChild();
        }
        if (tooltipUI != null)
        {
            tooltipUI.gameObject.SetActive(false);
            Debug.Log($"[CombatantView] {name} tooltipUI 已连线, activeSelf={tooltipUI.gameObject.activeSelf}");
        }
    }

    private CombatantTooltipUI CreateTooltipChild()
    {
        Transform parent = portraitRoot != null ? portraitRoot : transform;

        GameObject go = new GameObject("CombatantTooltipUI");
        go.transform.SetParent(parent, false);
        go.transform.localPosition = new Vector3(2.5f, 1.5f, 0f);
        go.transform.localRotation = Quaternion.identity;
        go.transform.localScale = Vector3.one;
        go.layer = gameObject.layer;

        RectTransform rect = go.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(3f, 2f);
        rect.pivot = new Vector2(0f, 1f);

        Canvas canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.overrideSorting = true;
        canvas.sortingOrder = 101;

        CanvasScaler scaler = go.AddComponent<CanvasScaler>();
        scaler.dynamicPixelsPerUnit = 100f;

        go.AddComponent<GraphicRaycaster>();

        CombatantTooltipUI tooltip = go.AddComponent<CombatantTooltipUI>();

        Debug.Log($"[CombatantView] {name} 自动创建 tooltipUI 子物体");
        return tooltip;
    }

    void OnDestroy()
    {
        if (HoveredEnemy == this)
        {
            HoveredEnemy = null;
        }
    }

    protected void SetupBase(int health, Sprite image)
    {
        MaxHealth = CurrentHealth = health;
        if (spriteRenderer != null)
            spriteRenderer.sprite = image;

        if (skeletonAnimation != null)
            FreezeAtFirstFrame();

        UpdateHealthText();
        OnHealthChanged?.Invoke(this, CurrentHealth);
    }

    private void FreezeAtFirstFrame()
    {
        if (skeletonAnimation.Skeleton == null || skeletonAnimation.Skeleton.Data == null) return;
        var anim = skeletonAnimation.Skeleton.Data.FindAnimation("animation");
        if (anim != null)
            anim.Apply(skeletonAnimation.Skeleton, 0, 0, false, null, 1, MixBlend.Setup, MixDirection.In);
        skeletonAnimation.enabled = false;
    }

    public IEnumerator PlayAttackAnimation()
    {
        if (skeletonAnimation == null) yield break;
        if (skeletonAnimation.Skeleton == null || skeletonAnimation.Skeleton.Data == null) yield break;
        var anim = skeletonAnimation.Skeleton.Data.FindAnimation("animation");
        if (anim == null) yield break;

        skeletonAnimation.enabled = true;
        var track = skeletonAnimation.AnimationState.SetAnimation(0, "animation", false);

        while (!track.IsComplete)
            yield return null;

        FreezeAtFirstFrame();
    }

    public void SetFacing(int targetHexX, int targetHexZ)
    {
        int worldDx = 2 * (targetHexX - HexCoordX) + (targetHexZ - HexCoordZ);
        bool shouldFaceRight = worldDx > 0;
        if (shouldFaceRight == facingRight) return;
        facingRight = shouldFaceRight;

        Transform flipTarget = GetVisualTransform();
        float absX = Mathf.Abs(flipTarget.localScale.x);
        float targetX = absX * (facingRight ? -1f : 1f);

        Sequence seq = DOTween.Sequence();
        seq.Append(flipTarget.DOScaleX(0, 0.06f).SetEase(Ease.InQuad));
        seq.Append(flipTarget.DOScaleX(targetX, 0.06f).SetEase(Ease.OutQuad));
    }

    private Transform GetVisualTransform()
    {
        if (spriteRenderer != null) return spriteRenderer.transform;
        if (skeletonAnimation != null) return skeletonAnimation.transform;
        if (portraitRoot != null) return portraitRoot;
        return transform;
    }

    private void UpdateHealthText()
    {
        if (healthText != null)
            healthText.text = CurrentHealth + "/" + MaxHealth;
    }

    public void Damage(int damageAmount)
    {
        LastUnblockedDamage = 0;

        if (HasStatusEffect(StatusEffectType.BLOCK))
        {
            RemoveStatusEffect(StatusEffectType.BLOCK, 1);
            return;
        }

        int remainingDamage = damageAmount;
        int currentArmor = GetStatusEffectStacks(StatusEffectType.ARMOR);
        if (currentArmor > 0)
        {
            if(currentArmor >= damageAmount)
            {
                RemoveStatusEffect(StatusEffectType.ARMOR, remainingDamage);
                remainingDamage = 0;
            }
            else if(currentArmor < damageAmount)
            {
                RemoveStatusEffect(StatusEffectType.ARMOR, currentArmor);
                remainingDamage -= currentArmor;
            }
        }

        LastUnblockedDamage = remainingDamage;

        if (remainingDamage > 0)
        {
            CurrentHealth -= remainingDamage;
            PopupTextManager.Instance.ShowDamageText(transform, remainingDamage, PopupTextType.Damage);
            if (CurrentHealth < 0)
            {
                CurrentHealth = 0;
            }

        }

        Transform shakeTarget = portraitRoot != null ? portraitRoot : transform;
        shakeTarget.DOShakePosition(0.2f, 0.5f);
        UpdateHealthText();
        OnHealthChanged?.Invoke(this, CurrentHealth);
    }

    public void SetCurrentHealth(int health)
    {
        CurrentHealth = Mathf.Clamp(health, 0, MaxHealth);
        UpdateHealthText();
        OnHealthChanged?.Invoke(this, CurrentHealth);
    }

    private static bool IsNonStackable(StatusEffectType type)
    {
        return type == StatusEffectType.WEAKNESS
            || type == StatusEffectType.VULNERABLE
            || type == StatusEffectType.FRAGILE
            || type == StatusEffectType.SLOW
            || type == StatusEffectType.CHAIN_LIGHTNING
            || type == StatusEffectType.ROOT
            || type == StatusEffectType.STUN
            || type == StatusEffectType.FLYING;
    }

    public void AddStatusEffect(StatusEffectType type, int stackCount)
    {
        if (stackCount == 0) return;

        if (stackCount < 0)
        {
            RemoveStatusEffect(type, -stackCount);
            return;
        }

        if (IsNonStackable(type) && HasStatusEffect(type))
            return;

        int effectiveStacks = stackCount;

        if (type == StatusEffectType.ARMOR)
        {
            effectiveStacks += GetStatusEffectStacks(StatusEffectType.FORTIFY);
            if (HasStatusEffect(StatusEffectType.FRAGILE))
                effectiveStacks = Mathf.FloorToInt(effectiveStacks * 0.5f);
        }

        if (statusEffects.ContainsKey(type))
        {
            statusEffects[type] += effectiveStacks;
        }
        else
        {
            statusEffects.Add(type, effectiveStacks);
        }
        if (statusEffectsUI != null)
            statusEffectsUI.UpdateStatusEffectUI(type, GetStatusEffectStacks(type));
        OnStatusChanged?.Invoke();
    }

    public void RemoveStatusEffect(StatusEffectType type, int stackCount)
    {
        if (statusEffects.ContainsKey(type))
        {
            statusEffects[type] -= stackCount;
            if (statusEffects[type] <= 0)
            {
                statusEffects.Remove(type);
            }
        }

        if (statusEffectsUI != null)
            statusEffectsUI.UpdateStatusEffectUI(type, GetStatusEffectStacks(type));
        OnStatusChanged?.Invoke();
    }

    public void SetStatusEffectStacks(StatusEffectType type, int stackCount)
    {
        if (stackCount <= 0)
        {
            statusEffects.Remove(type);
        }
        else
        {
            statusEffects[type] = stackCount;
        }
        if (statusEffectsUI != null)
            statusEffectsUI.UpdateStatusEffectUI(type, stackCount);
        OnStatusChanged?.Invoke();
    }

    public bool HasStatusEffect(StatusEffectType type)
    {
        return GetStatusEffectStacks(type) > 0;
    }

    public int GetStatusEffectStacks(StatusEffectType type)
    {
        if (statusEffects.ContainsKey(type)) return statusEffects[type];
        else return 0;
    }

    public IReadOnlyDictionary<StatusEffectType, int> GetStatusEffects()
    {
        return statusEffects;
    }

    public Sprite GetStatusEffectSprite(StatusEffectType type)
    {
        if (statusEffectsUI != null)
            return statusEffectsUI.GetSpriteByType(type);
        return null;
    }

    public void ShowTooltip()
    {
        if (tooltipUI == null) return;
        if (GetStatusEffects().Count == 0) return;
        tooltipUI.Populate(this);
        tooltipUI.Show();
    }

    public void HideTooltip()
    {
        if (tooltipUI != null)
            tooltipUI.Hide();
    }

    public void DecayTurnEndEffects()
    {
        StatusEffectType[] decayTypes = {
            StatusEffectType.BLEED,
            StatusEffectType.WEAKNESS,
            StatusEffectType.VULNERABLE,
            StatusEffectType.FRAGILE,
            StatusEffectType.SLOW,
            StatusEffectType.ROOT,
            StatusEffectType.STUN
        };

        foreach (var type in decayTypes)
        {
            if (HasStatusEffect(type))
                RemoveStatusEffect(type, 1);
        }
    }

    public void ClearArmorOnTurnEnd()
    {
        if (!PersistArmor)
        {
            int armorStacks = GetStatusEffectStacks(StatusEffectType.ARMOR);
            if (armorStacks > 0)
            {
                RemoveStatusEffect(StatusEffectType.ARMOR, armorStacks);
                Debug.Log($"[CombatantView] {name} 回合结束清空护甲 {armorStacks} 层");
            }
        }
    }

    public void SetOutline(bool enabled)
    {
        if (spriteRenderer == null || propertyBlock == null) return;
        spriteRenderer.GetPropertyBlock(propertyBlock);
        propertyBlock.SetFloat(EnableOutlineId, enabled ? 1f : 0f);
        spriteRenderer.SetPropertyBlock(propertyBlock);
    }

    void LateUpdate()
    {
        if (camera3D != null)
        {
            Transform billboardTarget = portraitRoot != null ? portraitRoot : transform;
            billboardTarget.rotation = camera3D.transform.rotation;
            BillboardUI();
        }

        if (tooltipUI != null && tooltipUI.gameObject.activeSelf)
        {
            tooltipUI.transform.rotation = camera3D.transform.rotation;
            Vector3 scale = tooltipUI.transform.localScale;
            scale.x = Mathf.Abs(scale.x);
            tooltipUI.transform.localScale = scale;
        }
    }

    protected virtual void BillboardUI()
    {
        if (camera3D == null || statusEffectsUI == null) return;
        Canvas canvas = statusEffectsUI.GetComponentInParent<Canvas>();
        if (canvas != null)
            canvas.transform.rotation = camera3D.transform.rotation;
    }
}