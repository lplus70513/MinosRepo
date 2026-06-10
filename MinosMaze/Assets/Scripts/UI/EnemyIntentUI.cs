using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class EnemyIntentUI : MonoBehaviour
{
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text valueText;

    public void SetData(EnemyIntentData data, Sprite icon)
    {
        iconImage.sprite = icon;

        bool showValue = data.IntentType == EnemyActionType.Attack || data.IntentType == EnemyActionType.Defense;
        if (showValue && data.HitCount > 0)
        {
            valueText.gameObject.SetActive(true);
            valueText.text = data.HitCount + "×" + data.ValuePerHit;
        }
        else
        {
            valueText.gameObject.SetActive(false);
        }
    }
}
