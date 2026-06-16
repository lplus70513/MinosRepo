using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ResourceHUD : MonoBehaviour
{
    [Header("金币")]
    [SerializeField] private TMP_Text goldText;

    [Header("线")]
    [SerializeField] private Image stringIcon;
    [SerializeField] private Sprite stringLowSprite;
    [SerializeField] private Sprite stringMediumSprite;
    [SerializeField] private Sprite stringHighSprite;
    [SerializeField] private TMP_Text stringText;

    private Tween _goldTween;
    private Tween _stringTween;

    void Update()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null || gm.WorldMapState == null) return;

        if (goldText != null && _goldTween == null)
            goldText.text = gm.WorldMapState.gold.ToString();

        int sc = gm.WorldMapState.stringCount;
        if (stringText != null && _stringTween == null)
            stringText.text = sc.ToString();

        if (stringIcon != null)
        {
            if (sc <= 5 && stringLowSprite != null)
                stringIcon.sprite = stringLowSprite;
            else if (sc <= 15 && stringMediumSprite != null)
                stringIcon.sprite = stringMediumSprite;
            else if (stringHighSprite != null)
                stringIcon.sprite = stringHighSprite;
        }
    }

    public void AnimateGold(int from, int to)
    {
        _goldTween?.Kill();
        _goldTween = DOTween.To(() => (float)from, x =>
        {
            if (goldText != null)
                goldText.text = Mathf.RoundToInt(x).ToString();
        }, to, 0.8f).SetEase(Ease.InOutCubic).OnComplete(() => _goldTween = null);
    }

    public void AnimateString(int from, int to)
    {
        _stringTween?.Kill();
        _stringTween = DOTween.To(() => (float)from, x =>
        {
            if (stringText != null)
                stringText.text = Mathf.RoundToInt(x).ToString();
        }, to, 0.8f).SetEase(Ease.InOutCubic).OnComplete(() =>
        {
            _stringTween = null;
            GameManager gm = GameManager.Instance;
            if (gm != null && gm.WorldMapState != null && stringIcon != null)
            {
                int sc = gm.WorldMapState.stringCount;
                if (sc <= 5 && stringLowSprite != null)
                    stringIcon.sprite = stringLowSprite;
                else if (sc <= 15 && stringMediumSprite != null)
                    stringIcon.sprite = stringMediumSprite;
                else if (stringHighSprite != null)
                    stringIcon.sprite = stringHighSprite;
            }
        });
    }
}
