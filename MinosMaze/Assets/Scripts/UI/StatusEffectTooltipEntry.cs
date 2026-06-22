using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class StatusEffectTooltipEntry : MonoBehaviour
{
    [SerializeField] private Image bgImage;
    [SerializeField] private Image icon;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text descText;

    void Awake()
    {
        var layoutElement = GetComponent<LayoutElement>();
        if (layoutElement == null)
            layoutElement = gameObject.AddComponent<LayoutElement>();
        layoutElement.preferredWidth = 300f;
        layoutElement.preferredHeight = 120f;
    }

    public void Set(Sprite bgSprite, Sprite iconSprite, string name, string description)
    {
        if (bgImage != null)
            bgImage.sprite = bgSprite;
        icon.sprite = iconSprite;
        nameText.text = name;
        descText.text = description;
    }
}
