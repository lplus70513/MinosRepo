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

    void Awake()
    {
        var layoutElement = GetComponent<LayoutElement>();
        if (layoutElement == null)
            layoutElement = gameObject.AddComponent<LayoutElement>();
        layoutElement.preferredWidth = 260f;

        var fitter = GetComponent<ContentSizeFitter>();
        if (fitter == null)
            fitter = gameObject.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
    }

    public void Set(Sprite iconSprite, string name, int stacks, string description)
    {
        if (bgImage != null)
            bgImage.enabled = true;

        icon.sprite = iconSprite;
        nameText.text = name;

        if (stackCountText != null)
        {
            if (stacks > 0)
            {
                stackCountText.text = stacks.ToString();
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
