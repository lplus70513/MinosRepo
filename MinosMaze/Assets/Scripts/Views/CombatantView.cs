using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using DG.Tweening;
using Spine;
using Spine.Unity;

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
        healthText.text = "HP: " + CurrentHealth;
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
            DamageTextManager.Instance.ShowDamage(transform.position, remainingDamage);
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

    public void AddStatusEffect(StatusEffectType type, int stackCount)
    {
        if (statusEffects.ContainsKey(type))
        {
            statusEffects[type] += stackCount;
        }
        else
        {
            statusEffects.Add(type, stackCount);
        }
        // ���÷���
        statusEffectsUI.UpdateStatusEffectUI(type, GetStatusEffectStacks(type));
    }

    private void RemoveStatusEffect(StatusEffectType type, int stackCount)
    {
        if (statusEffects.ContainsKey(type))
        {
            statusEffects[type] -= stackCount;
            if (statusEffects[type] <= 0)
            {
                statusEffects.Remove(type);
            }
        }

        // �޸ĵ� 2: �޸�ƴд���� GetStstusEffectStakes -> GetStatusEffectStacks
        statusEffectsUI.UpdateStatusEffectUI(type, GetStatusEffectStacks(type));
    }

    public int GetStatusEffectStacks(StatusEffectType type)
    {
        if (statusEffects.ContainsKey(type)) return statusEffects[type];
        else return 0;
    }

    void LateUpdate()
    {
        if (camera3D != null)
        {
            transform.rotation = camera3D.transform.rotation;
        }
    }
}