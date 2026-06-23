using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class HealthBarUI : MonoBehaviour
{
    [SerializeField] private Image fillImage;
    [SerializeField] private Image bufferImage;
    [SerializeField] private TMP_Text healthValueText;
    [SerializeField] public TMP_Text enemyNameText;
    [SerializeField] private Image shieldBarImage;
    [SerializeField] private Image shieldIconImage;
    [SerializeField] private TMP_Text shieldValueText;
    [SerializeField] private float bufferDuration = 0.5f;

    private int maxHealth;
    private Tween bufferTween;

    public void Initialize(int maxHp, int currentHp)
    {
        maxHealth = maxHp;
        float fill = (float)currentHp / maxHp;

        bufferImage.type = Image.Type.Filled;
        bufferImage.fillMethod = Image.FillMethod.Horizontal;
        bufferImage.color = Color.white;
        bufferImage.transform.SetAsFirstSibling();

        fillImage.type = Image.Type.Filled;
        fillImage.fillMethod = Image.FillMethod.Horizontal;

        fillImage.fillAmount = fill;
        bufferImage.fillAmount = fill;

        UpdateHealthValueText(currentHp);
        SetArmor(0);
    }

    public void SetArmor(int armor)
    {
        if (shieldBarImage != null)
            shieldBarImage.gameObject.SetActive(armor > 0);
        if (shieldIconImage != null)
            shieldIconImage.gameObject.SetActive(armor > 0);
        if (shieldValueText != null)
        {
            shieldValueText.gameObject.SetActive(armor > 0);
            shieldValueText.text = armor.ToString();
        }
    }

    public void SetHealth(int newHealth)
    {
        float targetFill = (float)newHealth / maxHealth;
        fillImage.fillAmount = targetFill;

        bufferTween?.Kill();
        bufferTween = bufferImage.DOFillAmount(targetFill, bufferDuration);

        UpdateHealthValueText(newHealth);
    }

    private void UpdateHealthValueText(int currentHp)
    {
        if (healthValueText != null)
            healthValueText.text = currentHp + "/" + maxHealth;
    }

    private void OnDestroy()
    {
        bufferTween?.Kill();
    }
}
