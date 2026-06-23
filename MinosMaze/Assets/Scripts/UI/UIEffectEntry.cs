using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIEffectEntry : MonoBehaviour
{
    [SerializeField] private Image bgImage;
    [SerializeField] private Image icon;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text stackCountText;
    [SerializeField] private TMP_Text descText;

    public void Set(Sprite iconSprite, string name, int stacks, string description)
    {
        icon.sprite = iconSprite;
        nameText.text = name;

        if (stackCountText != null)
        {
            if (stacks > 0)
            {
                stackCountText.text = $"x{stacks}";
                stackCountText.gameObject.SetActive(true);
            }
            else
            {
                stackCountText.gameObject.SetActive(false);
            }
        }

        descText.text = description;
    }
}
