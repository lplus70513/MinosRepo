using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CardSelectEntry : MonoBehaviour
{
    [SerializeField] private Image background;
    [SerializeField] private Image cardImage;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text costText;
    [SerializeField] private Button button;

    public CardData CardData { get; private set; }

    public void Setup(CardData data, Action<CardData> onClick)
    {
        CardData = data;
        if (background != null) background.sprite = data.Background;
        if (cardImage != null) cardImage.sprite = data.Image;
        if (nameText != null) nameText.text = data.name;
        if (costText != null) costText.text = data.Cost.ToString();
        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => onClick?.Invoke(data));
        }
    }
}
