using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
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

    [SerializeField] private StatusEffectsUI statusEffectsUI;

    public int HexCoordX { get; set; }
    public int HexCoordZ { get; set; }

    public int MaxHealth { get; private set; }
    public int CurrentHealth { get; private set; }

    public event Action<CombatantView, int> OnHealthChanged;

    private Dictionary<StatusEffectType, int> statusEffects = new();

    protected bool facingRight = false;

    private static Camera camera3D;
    private static Material alwaysVisibleMaterial;

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
            Shader shader = Shader.Find("Custom/SpriteAlwaysVisible");
            if (shader != null)
                alwaysVisibleMaterial = new Material(shader);
        }

        if (spriteRenderer != null && alwaysVisibleMaterial != null)
            spriteRenderer.material = alwaysVisibleMaterial;

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

        float absX = Mathf.Abs(transform.localScale.x);
        float targetX = absX * (facingRight ? -1f : 1f);

        Sequence seq = DOTween.Sequence();
        seq.Append(transform.DOScaleX(0, 0.06f).SetEase(Ease.InQuad));
        seq.Append(transform.DOScaleX(targetX, 0.06f).SetEase(Ease.OutQuad));
    }

    private void UpdateHealthText()
    {
        if (healthText != null)
            healthText.text = CurrentHealth + "/" + MaxHealth;
    }

    public void Damage(int damageAmount)
    {
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

        if (remainingDamage > 0)
        {
            CurrentHealth -= remainingDamage;
            PopupTextManager.Instance.ShowDamageText(transform, remainingDamage, PopupTextType.Damage);
            if (CurrentHealth < 0)
            {
                CurrentHealth = 0;
            }

        }

        transform.DOShakePosition(0.2f, 0.5f);
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
            || type == StatusEffectType.STUN;
    }

    public void AddStatusEffect(StatusEffectType type, int stackCount)
    {
        if (stackCount <= 0) return;

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
        statusEffectsUI.UpdateStatusEffectUI(type, GetStatusEffectStacks(type));
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

        statusEffectsUI.UpdateStatusEffectUI(type, GetStatusEffectStacks(type));
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

    void LateUpdate()
    {
        if (camera3D != null)
        {
            transform.rotation = camera3D.transform.rotation;
        }
    }
}