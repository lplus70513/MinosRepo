using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using DG.Tweening; // ȷ�������� DOTween

public class CombatantView : MonoBehaviour
{
    [SerializeField] private TMP_Text healthText;
    [SerializeField] private SpriteRenderer spriteRenderer;

    [SerializeField] private StatusEffectsUI statusEffectsUI;

    public int MaxHealth { get; private set; }
    public int CurrentHealth { get; private set; }

    private Dictionary<StatusEffectType, int> statusEffects = new();

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
    }

    protected void SetupBase(int health, Sprite image)
    {
        MaxHealth = CurrentHealth = health;
        spriteRenderer.sprite = image;
        UpdateHealthText();
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
            if (CurrentHealth < 0)
            {
                CurrentHealth = 0;
            }

        }

        transform.DOShakePosition(0.2f, 0.5f);
        UpdateHealthText();
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