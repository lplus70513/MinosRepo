using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class HealthBarUI : MonoBehaviour
{
    [SerializeField] private Image fillImage;
    [SerializeField] private Image bufferImage;
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
    }

    public void SetHealth(int newHealth)
    {
        float targetFill = (float)newHealth / maxHealth;
        fillImage.fillAmount = targetFill;

        bufferTween?.Kill();
        bufferTween = bufferImage.DOFillAmount(targetFill, bufferDuration);
    }

    private void OnDestroy()
    {
        bufferTween?.Kill();
    }
}
