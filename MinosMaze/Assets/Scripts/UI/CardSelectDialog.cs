using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class CardSelectDialog : MonoBehaviour
{
    [SerializeField] private List<CardSelectEntry> cardEntries;
    [SerializeField] private Button skipButton;

    [Header("弹出动画")]
    [SerializeField] private Transform scaleRoot;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private GameObject backgroundMask;
    [SerializeField] private float popDuration = 0.2f;

    void Awake()
    {
        if (scaleRoot == null)
            scaleRoot = transform;

        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
    }

    public void Show(List<CardData> choices, Action<CardData> onSelected, Action onSkipped)
    {
        gameObject.SetActive(true);
        scaleRoot.localScale = Vector3.zero;
        canvasGroup.alpha = 0f;
        if (backgroundMask != null)
            backgroundMask.SetActive(true);

        scaleRoot.DOScale(1f, popDuration).SetEase(Ease.OutBack);
        canvasGroup.DOFade(1f, popDuration).SetEase(Ease.OutQuad);
        for (int i = 0; i < cardEntries.Count; i++)
        {
            if (i < choices.Count)
            {
                cardEntries[i].Setup(choices[i], (card) =>
                {
                    onSelected?.Invoke(card);
                    gameObject.SetActive(false);
                });
                cardEntries[i].gameObject.SetActive(true);
            }
            else
            {
                cardEntries[i].gameObject.SetActive(false);
            }
        }
        if (skipButton != null)
        {
            skipButton.onClick.RemoveAllListeners();
            skipButton.onClick.AddListener(() =>
            {
                onSkipped?.Invoke();
                gameObject.SetActive(false);
            });
        }
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }
}
