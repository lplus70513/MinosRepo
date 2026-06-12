using System.Collections;
using DG.Tweening;
using TMPro;
using UnityEngine;

public class TurnCounterDisplay : MonoBehaviour
{
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private TMP_Text displayText;
    [SerializeField] private float fadeDuration = 0.3f;
    [SerializeField] private float startDelay = 3.1f;

    private int roundNumber = 0;
    private bool turnEnding;
    private bool nextTurnReady;

    private static readonly string[] digits = { "零", "一", "二", "三", "四", "五", "六", "七", "八", "九" };

    private void Awake()
    {
        if (canvasGroup != null)
            canvasGroup.alpha = 0f;
    }

    private void Start()
    {
        ActionSystem.SubscribeReaction<EnemyTurnGA>(OnTurnEnding, ReactionTiming.PRE);
        ActionSystem.SubscribeReaction<EnemyTurnGA>(OnPlayerTurnStart, ReactionTiming.POST);
        StartCoroutine(TurnDisplayLoop());
    }

    private void OnDestroy()
    {
        ActionSystem.UnsubscribeReaction<EnemyTurnGA>(OnTurnEnding, ReactionTiming.PRE);
        ActionSystem.UnsubscribeReaction<EnemyTurnGA>(OnPlayerTurnStart, ReactionTiming.POST);
    }

    private void OnTurnEnding(EnemyTurnGA enemyTurnGA)
    {
        turnEnding = true;
    }

    private void OnPlayerTurnStart(EnemyTurnGA enemyTurnGA)
    {
        nextTurnReady = true;
    }

    private IEnumerator TurnDisplayLoop()
    {
        yield return new WaitForSeconds(startDelay);

        roundNumber = 1;
        yield return StartCoroutine(ShowTurnText(roundNumber));

        while (true)
        {
            yield return new WaitUntil(() => nextTurnReady);
            nextTurnReady = false;
            roundNumber++;
            yield return StartCoroutine(ShowTurnText(roundNumber));
        }
    }

    private IEnumerator ShowTurnText(int round)
    {
        displayText.text = $"第{NumberToChinese(round)}回合";
        yield return canvasGroup.DOFade(1f, fadeDuration).WaitForCompletion();

        yield return new WaitUntil(() => turnEnding);
        turnEnding = false;

        yield return canvasGroup.DOFade(0f, fadeDuration).WaitForCompletion();
    }

    private static string NumberToChinese(int n)
    {
        if (n <= 0) return "零";
        if (n < 10) return digits[n];
        if (n < 20) return "十" + (n % 10 == 0 ? "" : digits[n % 10]);
        if (n < 100)
        {
            int tens = n / 10;
            int ones = n % 10;
            return digits[tens] + "十" + (ones == 0 ? "" : digits[ones]);
        }
        if (n < 1000)
        {
            int hundreds = n / 100;
            int rest = n % 100;
            string result = digits[hundreds] + "百";
            if (rest == 0) return result;
            if (rest < 10) return result + "零" + digits[rest];
            return result + NumberToChinese(rest);
        }
        return n.ToString();
    }
}
