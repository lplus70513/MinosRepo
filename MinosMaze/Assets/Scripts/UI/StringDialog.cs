using System;
using UnityEngine;
using UnityEngine.UI;

public class StringDialog : MonoBehaviour
{
    [SerializeField] private Button collectButton;
    [SerializeField] private Button skipButton;

    public void Show(Action onCollected, Action onSkipped)
    {
        gameObject.SetActive(true);
        if (collectButton != null)
        {
            collectButton.onClick.RemoveAllListeners();
            collectButton.onClick.AddListener(() =>
            {
                onCollected?.Invoke();
                gameObject.SetActive(false);
            });
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
}
