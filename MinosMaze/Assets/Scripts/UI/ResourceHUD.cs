using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ResourceHUD : MonoBehaviour
{
    [Header("金币")]
    [SerializeField] private TMP_Text goldText;

    [Header("线")]
    [SerializeField] private Image stringIcon;
    [SerializeField] private Sprite stringLowSprite;
    [SerializeField] private Sprite stringMediumSprite;
    [SerializeField] private Sprite stringHighSprite;
    [SerializeField] private TMP_Text stringText;

    void Update()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null || gm.WorldMapState == null) return;

        if (goldText != null)
            goldText.text = gm.WorldMapState.gold.ToString();

        int sc = gm.WorldMapState.stringCount;
        if (stringText != null)
            stringText.text = sc.ToString();

        if (stringIcon != null)
        {
            if (sc <= 5 && stringLowSprite != null)
                stringIcon.sprite = stringLowSprite;
            else if (sc <= 15 && stringMediumSprite != null)
                stringIcon.sprite = stringMediumSprite;
            else if (stringHighSprite != null)
                stringIcon.sprite = stringHighSprite;
        }
    }
}
