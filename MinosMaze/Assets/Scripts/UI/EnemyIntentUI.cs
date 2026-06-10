using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public class EnemyIntentUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private Image iconImage;
    [SerializeField] private GameObject tooltipPanel;
    [SerializeField] private TMP_Text tooltipText;

    private EnemyIntentData intentData;

    private void Awake()
    {
        if (tooltipPanel != null)
            tooltipPanel.SetActive(false);
    }

    public void SetData(EnemyIntentData data, Sprite icon)
    {
        intentData = data;
        iconImage.sprite = icon;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (tooltipPanel != null && intentData != null)
        {
            tooltipText.text = BuildTooltipText(intentData);
            tooltipPanel.SetActive(true);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (tooltipPanel != null)
            tooltipPanel.SetActive(false);
    }

    private string BuildTooltipText(EnemyIntentData data)
    {
        switch (data.IntentType)
        {
            case EnemyActionType.Attack:
                return "攻击 " + data.HitCount + "×" + data.ValuePerHit;
            case EnemyActionType.Defense:
                return "防御 " + data.HitCount + "×" + data.ValuePerHit;
            case EnemyActionType.Move:
                return "移动";
            case EnemyActionType.Buff:
                return "增益";
            case EnemyActionType.Debuff:
                return "减益";
            default:
                return "";
        }
    }
}
