using UnityEngine;
using DG.Tweening;

public class CardGridHoverTrigger : MonoBehaviour
{
    [SerializeField] private float hoverScale = 1.1f;
    [SerializeField] private float hoverDuration = 0.1f;

    private Card card;
    private Transform cardTransform;
    private Vector3 originalScale;
    private Tween hoverTween;

    public void Init(Card card)
    {
        this.card = card;
        cardTransform = transform.parent;
        originalScale = cardTransform.localScale;
    }

    void OnMouseEnter()
    {
        if (CardViewHoverSystem.Instance == null || card == null) return;

        Vector3 worldPos = Camera.main.ScreenToWorldPoint(
            new Vector3(Input.mousePosition.x, Input.mousePosition.y, 5f)
        );
        CardViewHoverSystem.Instance.Show(card, worldPos);

        PlayHoverAnim();
    }

    void OnMouseExit()
    {
        if (CardViewHoverSystem.Instance != null)
            CardViewHoverSystem.Instance.Hide();

        StopHoverAnim();
    }

    private void PlayHoverAnim()
    {
        if (cardTransform == null) return;

        hoverTween?.Kill();
        hoverTween = cardTransform.DOScale(originalScale * hoverScale, hoverDuration).SetEase(Ease.OutQuad).SetUpdate(true);
    }

    private void StopHoverAnim()
    {
        hoverTween?.Kill();
        hoverTween = null;
        if (cardTransform != null)
            hoverTween = cardTransform.DOScale(originalScale, hoverDuration).SetEase(Ease.OutQuad).SetUpdate(true);
    }

    void OnDestroy()
    {
        hoverTween?.Kill();
    }
}
