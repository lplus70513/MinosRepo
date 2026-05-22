using System;
using UnityEngine;
using TMPro;
using DG.Tweening;

public class DamageFloatText : MonoBehaviour
{
    [SerializeField] private TMP_Text damageText;
    [SerializeField] private float startYOffset = 2.5f;
    [SerializeField] private float floatDistance = 1.5f;
    [SerializeField] private float randomOffsetX = 0.4f;
    [SerializeField] private float randomEndOffsetX = 0.5f;
    [SerializeField] private float duration = 0.8f;

    private Action<DamageFloatText> onComplete;
    private Color startColor;

    private static Camera camera3D;

    void Awake()
    {
        if (damageText != null)
            startColor = damageText.color;

        if (camera3D == null)
        {
            var camObj = GameObject.FindGameObjectWithTag("3D Camera");
            if (camObj != null) camera3D = camObj.GetComponent<Camera>();
        }
    }

    public void Show(Vector3 worldPosition, int amount, Action<DamageFloatText> callback)
    {
        onComplete = callback;
        gameObject.SetActive(true);

        damageText.text = "-" + amount;
        damageText.color = startColor;

        float startX = UnityEngine.Random.Range(-randomOffsetX, randomOffsetX);
        float endX = startX + UnityEngine.Random.Range(-randomEndOffsetX, randomEndOffsetX);

        Vector3 startPos = worldPosition + new Vector3(startX, startYOffset, 0);
        Vector3 endPos = startPos + new Vector3(endX - startX, floatDistance, 0);

        transform.position = startPos;
        transform.DOKill();

        Color fadedColor = new Color(startColor.r, startColor.g, startColor.b, 0);

        Sequence seq = DOTween.Sequence();
        seq.Join(transform.DOMove(endPos, duration).SetEase(Ease.OutQuad));
        seq.Join(DOTween.To(
            () => damageText.color,
            c => damageText.color = c,
            fadedColor,
            duration).SetEase(Ease.InQuad));
        seq.OnComplete(OnAnimationComplete);
    }

    void LateUpdate()
    {
        if (camera3D != null)
            transform.rotation = camera3D.transform.rotation;
    }

    void OnDestroy()
    {
        transform.DOKill();
    }

    private void OnAnimationComplete()
    {
        gameObject.SetActive(false);
        onComplete?.Invoke(this);
    }
}
