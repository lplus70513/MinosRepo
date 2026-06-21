using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class StatusEffectTooltipEntry : MonoBehaviour
{
    [SerializeField] private Image icon;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text descText;

    public void Set(Sprite sprite, string name, string description)
    {
        icon.sprite = sprite;
        nameText.text = name;
        descText.text = description;
    }
}
