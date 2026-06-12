using System.Collections;
using DG.Tweening;
using TMPro;
using UnityEngine;

public class RoundDisplayController : MonoBehaviour
{
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private TMP_Text displayText;
    [SerializeField] private float fadeInDuration = 0.3f;
    [SerializeField] private float stayDuration = 1f;
    [SerializeField] private float fadeOutDuration = 0.3f;
    [SerializeField] private float battleStartDelay = 1.5f;

    private void Awake()
    {
        if (canvasGroup != null)
            canvasGroup.alpha = 0f;
    }

    private void Start()
    {
        StartCoroutine(BattleStartSequence());
    }

    private IEnumerator BattleStartSequence()
    {
        yield return new WaitForSeconds(battleStartDelay);
        displayText.text = "战斗开始";
        yield return canvasGroup.DOFade(1f, fadeInDuration).WaitForCompletion();
        yield return new WaitForSeconds(stayDuration);
        yield return canvasGroup.DOFade(0f, fadeOutDuration).WaitForCompletion();
    }
}
