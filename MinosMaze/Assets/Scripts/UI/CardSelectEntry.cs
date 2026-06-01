using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using DG.Tweening;

public class CardSelectEntry : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private Image background;
    [SerializeField] private Image cardImage;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text costText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private float hoverScale = 1.1f;
    [SerializeField] private float hoverDuration = 0.1f;

    public CardData CardData { get; private set; }
    private Action<CardData> _onClick;
    private Vector3 _originalScale;
    private Tween _hoverTween;

    public void Setup(CardData data, Action<CardData> onClick)
    {
        CardData = data;
        _onClick = onClick;
        _originalScale = transform.localScale;
        if (background != null) background.sprite = data.Background;
        if (cardImage != null) cardImage.sprite = data.Image;
        if (nameText != null) nameText.text = data.name;
        if (costText != null) costText.text = data.Cost.ToString();
        if (descriptionText != null) descriptionText.text = data.Description;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        _hoverTween?.Kill();
        _hoverTween = transform.DOScale(_originalScale * hoverScale, hoverDuration).SetEase(Ease.OutQuad).SetUpdate(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _hoverTween?.Kill();
        _hoverTween = transform.DOScale(_originalScale, hoverDuration).SetEase(Ease.OutQuad).SetUpdate(true);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        _onClick?.Invoke(CardData);
    }

    void OnDestroy()
    {
        _hoverTween?.Kill();
    }
}
