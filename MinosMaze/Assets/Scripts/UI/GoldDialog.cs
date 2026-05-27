using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GoldDialog : MonoBehaviour
{
    [SerializeField] private TMP_Text amountText;
    [SerializeField] private Button collectButton;
    [SerializeField] private Button skipButton;

    public void Show(int goldAmount, Action onCollected, Action onSkipped)
    {
        gameObject.SetActive(true);
        if (amountText != null) amountText.text = "+" + goldAmount + " 金币";
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
