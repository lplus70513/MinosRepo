using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CardSelectEntry : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private Image background;
    [SerializeField] private Image cardImage;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text costText;
    [SerializeField] private TMP_Text descriptionText;

    public CardData CardData { get; private set; }
    private Action<CardData> _onClick;

    public void Setup(CardData data, Action<CardData> onClick)
    {
        CardData = data;
        _onClick = onClick;
        if (background != null) background.sprite = data.Background;
        if (cardImage != null) cardImage.sprite = data.Image;
        if (nameText != null) nameText.text = data.name;
        if (costText != null) costText.text = data.Cost.ToString();
        if (descriptionText != null) descriptionText.text = data.Description;
    }

    void IPointerClickHandler.OnPointerClick(PointerEventData eventData)
    {
        _onClick?.Invoke(CardData);
    }
}
