using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class StatusEffectSlotUI : MonoBehaviour
{
    [SerializeField] private Image icon;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text descText;

    void Awake()
    {
        gameObject.SetActive(false);
    }

    public void Populate(Sprite iconSprite, string name, string description)
    {
        if (icon != null)
            icon.sprite = iconSprite;
        if (nameText != null)
            nameText.text = name;
        if (descText != null)
            descText.text = description;
        gameObject.SetActive(true);
    }

    public void Clear()
    {
        gameObject.SetActive(false);
    }
}
