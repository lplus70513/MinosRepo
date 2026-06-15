using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class StatusEffectUI : MonoBehaviour
{
    [SerializeField] private Image image;

    [SerializeField] private TMP_Text stackCountText;

    [SerializeField] private Image badgeBackground;

    private void Awake()
    {
        var textRect = stackCountText.rectTransform;
        textRect.anchorMin = new Vector2(1f, 0f);
        textRect.anchorMax = new Vector2(1f, 0f);
        textRect.pivot = new Vector2(0.5f, 0.5f);
        textRect.anchoredPosition = new Vector2(-12f, 12f);
        textRect.sizeDelta = new Vector2(20f, 20f);
        stackCountText.fontSize = 14;
        stackCountText.fontStyle = FontStyles.Bold;
        stackCountText.color = Color.white;

        if (badgeBackground != null)
        {
            var badgeRect = badgeBackground.rectTransform;
            badgeRect.anchorMin = new Vector2(1f, 0f);
            badgeRect.anchorMax = new Vector2(1f, 0f);
            badgeRect.pivot = new Vector2(0.5f, 0.5f);
            badgeRect.anchoredPosition = new Vector2(-12f, 12f);
            badgeRect.sizeDelta = new Vector2(24f, 24f);
            badgeBackground.transform.SetAsFirstSibling();
        }
    }

    public void Set(Sprite sprite, int stackCount)
    {
        image.sprite = sprite;
        stackCountText.text = stackCount.ToString();
    }
}
