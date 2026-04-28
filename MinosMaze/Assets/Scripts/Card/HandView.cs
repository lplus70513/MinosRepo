using UnityEngine;
using UnityEngine.Splines;
using System.Collections.Generic;
using DG.Tweening;
using System.Linq; 

public class HandView : MonoBehaviour
{
    [SerializeField] private int maxHandSize = 5;
    [SerializeField] private GameObject cardPrefab;
    [SerializeField] private SplineContainer splineContainer;
    [SerializeField] private Transform spawnPoint;

    // 对应报错：之前代码里写成了 dataForTesting，这里统一用 deckData
    [SerializeField] private List<CardData> deckData;

    // 对应报错：之前代码里写成了 cards，这里统一用 handCards
    private List<(GameObject card, float originalZ)> handCards = new List<(GameObject, float)>();

    private Camera mainCamera;

    private void Awake()
    {
        mainCamera = Camera.main;
    }

    private void Start()
    {
        CardSystem.Instance.SetUp(deckData);
    }

    // 供 CardSystem 调用的方法（修正 CardSystem 里的报错）
    public CardView AddCard(Card card, Vector3 position, Quaternion rotation)
    {
        if (handCards.Count >= maxHandSize) return null;

        GameObject newCardGO = Instantiate(cardPrefab, position, rotation, transform);
        CardView cardView = newCardGO.GetComponent<CardView>();

        if (cardView != null)
        {
            cardView.SetUp(card);
            float initialZ = newCardGO.transform.position.z;
            handCards.Add((newCardGO, initialZ));
            UpdateCardZOrder();
            UpdateCardPositions();
        }
        return cardView;
    }

    public CardView RemoveCard(Card card)
    {
        // 找到对应的 CardView
        var item = handCards.FirstOrDefault(x => x.card.GetComponent<CardView>().Card == card);

        if (item.card != null)
        {
            handCards.Remove(item);
            UpdateCardPositions(); // 移除后刷新布局
            return item.card.GetComponent<CardView>();
        }
        return null;
    }

    private void UpdateCardZOrder()
    {
        for (int i = 0; i < handCards.Count; i++)
        {
            var (cardGO, originalZ) = handCards[i];
            float newZ = originalZ - i * 0.01f;
            cardGO.transform.DOKill();
            cardGO.transform.DOMoveZ(newZ, 0.3f);
        }
    }

    private void UpdateCardPositions()
    {
        if (handCards.Count == 0 || splineContainer == null) return;

        float cardSpacing = 1f / maxHandSize;
        float firstCardPosition = 0.5f - (handCards.Count - 1) * cardSpacing / 2;

        Spline spline = splineContainer.Splines[0];
        Vector3 cameraForward = mainCamera.transform.forward;

        for (int i = 0; i < handCards.Count; i++)
        {
            var (cardGO, originalZ) = handCards[i];
            float p = firstCardPosition + (i * cardSpacing);

            Vector3 position = spline.EvaluatePosition(p);
            position.z = originalZ - i * 0.01f;

            Vector3 tangent = spline.EvaluateTangent(p);
            if (tangent.sqrMagnitude < 1e-8f) tangent = Vector3.right;

            Quaternion rotation = Quaternion.LookRotation(cameraForward, Vector3.Cross(cameraForward, tangent));

            cardGO.transform.DOKill();
            cardGO.transform.DOMove(position, 0.5f).SetEase(Ease.OutBack, 0.45f);
            cardGO.transform.DORotateQuaternion(rotation, 0.5f);
        }
    }
}