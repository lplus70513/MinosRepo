using UnityEngine;
using UnityEngine.Splines;
using System.Collections.Generic;
using DG.Tweening;
using System.Linq; 

public class HandView : MonoBehaviour
{
    [SerializeField] private int maxHandSize = 10;
    [SerializeField] private GameObject cardPrefab;
    [SerializeField] private SplineContainer splineContainer;
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private float hoverGapRatio = 0.15f;

    private List<(GameObject card, float originalZ)> handCards = new List<(GameObject, float)>();

    private Camera mainCamera;
    private int? hoveredCardIndex;
    private int? draggedCardIndex;

    private void Awake()
    {
        mainCamera = Camera.main;
    }

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
            cardView.handView = this;
            cardView.handIndex = handCards.Count - 1;
            UpdateCardZOrder();
            UpdateCardPositions();
        }
        return cardView;
    }

    public CardView RemoveCard(Card card)
    {
        var item = handCards.FirstOrDefault(x => x.card.GetComponent<CardView>().Card == card);

        if (item.card != null)
        {
            int removedIdx = handCards.IndexOf(item);
            if (hoveredCardIndex == removedIdx) hoveredCardIndex = null;
            if (draggedCardIndex == removedIdx) draggedCardIndex = null;
            handCards.Remove(item);
            UpdateCardPositions();
            return item.card.GetComponent<CardView>();
        }
        return null;
    }

    public void OnCardHovered(CardView cardView)
    {
        hoveredCardIndex = FindCardIndex(cardView);
        draggedCardIndex = null;
        RefreshLayout();
    }

    public void OnCardUnhovered(CardView cardView)
    {
        if (hoveredCardIndex == FindCardIndex(cardView))
            hoveredCardIndex = null;
        RefreshLayout();
    }

    public void OnCardDragStarted(CardView cardView)
    {
        draggedCardIndex = FindCardIndex(cardView);
        hoveredCardIndex = null;
        RefreshLayout();
    }

    public void OnCardDragEnded(CardView cardView)
    {
        if (draggedCardIndex == FindCardIndex(cardView))
            draggedCardIndex = null;
        RefreshLayout();
    }

    public void ClearDragState()
    {
        draggedCardIndex = null;
        hoveredCardIndex = null;
    }

    private int FindCardIndex(CardView cardView)
    {
        for (int i = 0; i < handCards.Count; i++)
        {
            if (handCards[i].card.GetComponent<CardView>() == cardView)
                return i;
        }
        return -1;
    }

    private void RefreshLayout()
    {
        UpdateCardPositions();
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

        int effectiveCount = handCards.Count - (draggedCardIndex.HasValue ? 1 : 0);
        if (effectiveCount <= 0) return;

        float cardSpacing = 1f / maxHandSize;
        float firstCardPosition = 0.5f - (effectiveCount - 1) * cardSpacing / 2;

        Spline spline = splineContainer.Splines[0];
        Vector3 cameraForward = mainCamera.transform.forward;

        int layoutIdx = 0;
        for (int i = 0; i < handCards.Count; i++)
        {
            if (draggedCardIndex.HasValue && i == draggedCardIndex.Value) continue;

            var (cardGO, originalZ) = handCards[i];
            float p = firstCardPosition + layoutIdx * cardSpacing;

            if (hoveredCardIndex.HasValue && !draggedCardIndex.HasValue)
            {
                if (i < hoveredCardIndex.Value)
                    p -= cardSpacing * hoverGapRatio;
                else if (i > hoveredCardIndex.Value)
                    p += cardSpacing * hoverGapRatio;
            }

            if (hoveredCardIndex.HasValue && i == hoveredCardIndex.Value && !draggedCardIndex.HasValue)
            {
                layoutIdx++;
                continue;
            }

            Vector3 position = spline.EvaluatePosition(p);
            position.z = originalZ - i * 0.01f;

            Vector3 tangent = spline.EvaluateTangent(p);
            if (tangent.sqrMagnitude < 1e-8f) tangent = Vector3.right;

            Quaternion rotation = Quaternion.LookRotation(cameraForward, Vector3.Cross(cameraForward, tangent));

            cardGO.transform.DOKill();
            cardGO.transform.DOMove(position, 0.5f).SetEase(Ease.OutBack, 0.45f);
            cardGO.transform.DORotateQuaternion(rotation, 0.5f);

            layoutIdx++;
        }
    }
}