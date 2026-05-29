using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CardSelectDialog : MonoBehaviour
{
    [SerializeField] private List<CardSelectEntry> cardEntries;
    [SerializeField] private Button skipButton;

    public void Show(List<CardData> choices, Action<CardData> onSelected, Action onSkipped)
    {
        gameObject.SetActive(true);
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
