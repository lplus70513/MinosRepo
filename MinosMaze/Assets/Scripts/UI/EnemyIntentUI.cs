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

        bool showValue = data.IntentType == EnemyActionType.Attack
                      || data.IntentType == EnemyActionType.Defense;
        bool hasValue = data.HitCount > 0 && data.ValuePerHit > 0;

        if (valueText != null)
        {
            if (showValue && hasValue)
            {
                valueText.gameObject.SetActive(true);
                valueText.text = data.HitCount == 1
                    ? data.ValuePerHit.ToString()
                    : data.ValuePerHit + "x" + data.HitCount;
            }
            else
            {
                valueText.gameObject.SetActive(false);
            }
        }
    }
}
