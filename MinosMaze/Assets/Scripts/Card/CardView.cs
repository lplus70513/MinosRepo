using TMPro;
using UnityEngine;
using DG.Tweening;

public class CardView : MonoBehaviour
{
    [SerializeField] private TMP_Text Name;
    [SerializeField] private TMP_Text CostText;
    [SerializeField] private TMP_Text Description;

    [SerializeField] private GameObject wrapper;
    [SerializeField] private LayerMask dropLayer;
    [SerializeField] private SpriteRenderer image;
    [SerializeField] private SpriteRenderer background;

    [Header("动画参数")]
    [SerializeField] private float hoverScale = 1.3f;
    [SerializeField] private float hoverYOffset = 1.5f;
    [SerializeField] private float hoverZOffset = -0.5f;
    [SerializeField] private float animDuration = 0.25f;
    [SerializeField] private int sortingOrderBoost = 100;

    public Card Card { get; private set; }
    public HandView handView;
    public int handIndex;

    private enum DragState { None, Hovered, DraggingNonPlay, DraggingPlay }

    private DragState state;

    private Vector3 dragStartPosition;
    private Quaternion dragStartRotation;
    private Vector3 dragStartScale;

    private static CardView targetingCard;
    private static CardView hoveredCard;
    private static CardView previousHoveredCard;
    private float hoverOtherCardTimer;

    private bool isHovered;
    private bool wasInPlayArea;
    private Vector3 wrapperOrigLocalPos;
    private Vector3 wrapperOrigLocalScale;
    private Renderer[] allRenderers;
    private int[] origSortingOrders;

    void Awake()
    {
        wrapperOrigLocalPos = wrapper.transform.localPosition;
        wrapperOrigLocalScale = wrapper.transform.localScale;
        dragStartScale = transform.localScale;

        allRenderers = GetComponentsInChildren<Renderer>(true);
        origSortingOrders = new int[allRenderers.Length];
        for (int i = 0; i < allRenderers.Length; i++)
            origSortingOrders[i] = allRenderers[i].sortingOrder;
    }

    public void SetUp(Card card)
    {
        Card = card;
        Name.text = card.Name;
        Description.text = card.Description;
        CostText.text = card.Cost.ToString();
        image.sprite = card.Image;
        background.sprite = card.Background;
    }

    // ==================== Hover ====================

    void OnMouseEnter()
    {
        if (!Interactions.Instance.PlayerCanInteract()) return;
        if (!Interactions.Instance.PlayerCanHover()) return;
        hoveredCard = this;
    }

    void OnMouseExit()
    {
        if (hoveredCard == this)
            hoveredCard = null;
    }

    // ==================== Drag ====================

    void OnMouseDown()
    {
        if (!Interactions.Instance.PlayerCanInteract()) return;

        if (targetingCard != null && targetingCard != this)
            targetingCard.CancelTargeting();

        dragStartPosition = transform.position;
        dragStartRotation = transform.rotation;
        dragStartScale = transform.localScale;

        if (!isHovered)
        {
            BringToFront();
            wrapper.transform.DOKill();
            Vector3 raised = wrapperOrigLocalPos;
            raised.y = hoverYOffset;
            raised.z = hoverZOffset;
            wrapper.transform.localPosition = raised;
            wrapper.transform.localScale = Vector3.one * hoverScale;
            isHovered = true;
        }

        Interactions.Instance.PlayerIsDragging = true;

        if (Card.ManualTargetEffect != null)
        {
            Interactions.Instance.PlayerIsTargeting = true;
            targetingCard = this;
            if (Card.HasAttackRange)
            {
                HeroView hero = HeroSystem.Instance.HeroView;
                HexGrid.HighlightCellsInRange(hero.HexCoordX, hero.HexCoordZ, Card.AttackRange);
            }
        }

        handView?.OnCardDragStarted(this);

        transform.rotation = Quaternion.Euler(0, 0, 0);
        transform.position = MouseUtil.GetMousePositionInWorldSpace(-1);

        state = DragState.DraggingNonPlay;
        wasInPlayArea = false;
    }

    void OnMouseDrag()
    {
        if (!Interactions.Instance.PlayerCanInteract()) return;

        bool inPlayArea = IsMouseInPlayArea();

        if (Card.ManualTargetEffect != null)
        {
            if (inPlayArea && !wasInPlayArea)
            {
                EnterPlayAreaTargeting();
            }
            else if (!inPlayArea && wasInPlayArea)
            {
                LeavePlayAreaTargeting();
            }

            if (state == DragState.DraggingNonPlay)
            {
                transform.position = MouseUtil.GetMousePositionInWorldSpace(-1);
            }
        }
        else
        {
            transform.position = MouseUtil.GetMousePositionInWorldSpace(-1);
        }

        wasInPlayArea = inPlayArea;
    }

    void OnMouseUp()
    {
        if (!Interactions.Instance.PlayerCanInteract())
        {
            CancelDrag();
            return;
        }

        bool inPlayArea = IsMouseInPlayArea();

        if (Card.ManualTargetEffect != null)
        {
            if (inPlayArea)
            {
                ResolveTargeting();
            }
            else
            {
                CancelDrag();
            }
            return;
        }

        if (inPlayArea)
        {
            bool hasCost = CostSystem.Instance.HasEnoughCost(Card.Cost);
            if (hasCost)
            {
                CleanupDragOnSuccess();
                PlayCardGA playCardGA = new(Card);
                ActionSystem.Instance.Perform(playCardGA);
            }
            else
            {
                CancelDrag();
            }
        }
        else
        {
            CancelDrag();
        }
    }

    // ==================== Play-area transitions (targeting) ====================

    void EnterPlayAreaTargeting()
    {
        state = DragState.DraggingPlay;
        if (handView != null)
        {
            Vector3 handCenter = handView.GetHandCenterPosition();
            transform.DOKill();
            transform.position = handCenter;
        }
        ManualTargetSystem.Instance.StartTargeting(transform.position);
    }

    void LeavePlayAreaTargeting()
    {
        state = DragState.DraggingNonPlay;
        ManualTargetSystem.Instance.StopTargeting();
        transform.position = MouseUtil.GetMousePositionInWorldSpace(-1);
    }

    bool IsMouseInPlayArea()
    {
        Vector3 mousePos = MouseUtil.GetMousePositionInWorldSpace(-1);
        return Physics.Raycast(mousePos, Vector3.forward, out RaycastHit hit, 10f, dropLayer)
               && hit.transform.tag != "Card";
    }

    // ==================== Update ====================

    void Update()
    {
        if (targetingCard == this)
        {
            if (Input.GetMouseButtonUp(0))
            {
                ResolveTargeting();
                return;
            }

            if (hoveredCard != null && hoveredCard != this)
            {
                hoverOtherCardTimer += Time.deltaTime;
                if (hoverOtherCardTimer >= 0.5f)
                {
                    CancelTargeting();
                }
            }
            else
            {
                hoverOtherCardTimer = 0f;
            }
            return;
        }

        if (hoveredCard != previousHoveredCard)
        {
            if (previousHoveredCard != null && previousHoveredCard.isHovered)
                previousHoveredCard.EndHoverAnimation();
            if (hoveredCard != null && !hoveredCard.isHovered)
                hoveredCard.StartHoverAnimation();
            previousHoveredCard = hoveredCard;
        }
    }

    // ==================== Hover Animation ====================

    void StartHoverAnimation()
    {
        if (isHovered) return;
        isHovered = true;
        BringToFront();
        handView?.OnCardHovered(this);

        wrapper.transform.DOKill();
        Vector3 targetLocalPos = wrapperOrigLocalPos;
        targetLocalPos.y = hoverYOffset;
        targetLocalPos.z = hoverZOffset;
        wrapper.transform.DOLocalMove(targetLocalPos, animDuration).SetEase(Ease.OutBack);
        wrapper.transform.DOScale(hoverScale, animDuration).SetEase(Ease.OutBack);
    }

    void EndHoverAnimation()
    {
        if (!isHovered) return;
        isHovered = false;
        state = DragState.None;
        RestoreSortingOrder();
        handView?.OnCardUnhovered(this);

        wrapper.transform.DOKill();
        wrapper.transform.DOLocalMove(wrapperOrigLocalPos, animDuration).SetEase(Ease.OutQuad);
        wrapper.transform.DOScale(wrapperOrigLocalScale, animDuration).SetEase(Ease.OutQuad);
    }

    // ==================== Targeting ====================

    private void ResolveTargeting()
    {
        CleanupTargeting();

        if (!Interactions.Instance.PlayerCanInteract())
        {
            CancelDrag();
            return;
        }

        EnemyView target = ManualTargetSystem.Instance.EndTargeting();

        bool hasCost = CostSystem.Instance.HasEnoughCost(Card.Cost);

        if (target != null && hasCost && Card.HasAttackRange)
        {
            HeroView hero = HeroSystem.Instance.HeroView;
            int dist = HexGrid.HexDistance(hero.HexCoordX, hero.HexCoordZ, target.HexCoordX, target.HexCoordZ);
            if (dist > Card.AttackRange)
            {
                target = null;
            }
        }

        if (target != null && hasCost)
        {
            CleanupDragOnSuccess();
            PlayCardGA playCardGA = new(Card, target);
            ActionSystem.Instance.Perform(playCardGA);
            return;
        }

        CancelDrag();
    }

    private void CancelTargeting()
    {
        CleanupTargeting();
        hoverOtherCardTimer = 0f;
        CancelDrag();
    }

    private void CleanupTargeting()
    {
        if (targetingCard == this) targetingCard = null;
        Interactions.Instance.PlayerIsTargeting = false;
        ManualTargetSystem.Instance.StopTargeting();
        HexGrid.ClearAllHighlights();
    }

    // ==================== Drag Lifecycle ====================

    void CancelDrag()
    {
        if (!this || !gameObject) return;

        Interactions.Instance.PlayerIsDragging = false;
        Interactions.Instance.PlayerIsTargeting = false;
        ManualTargetSystem.Instance.StopTargeting();
        if (targetingCard == this) targetingCard = null;
        HexGrid.ClearAllHighlights();

        state = DragState.None;

        RestoreSortingOrder();
        wrapper.transform.DOKill();
        wrapper.transform.DOLocalMove(wrapperOrigLocalPos, animDuration).SetEase(Ease.OutQuad);
        wrapper.transform.DOScale(wrapperOrigLocalScale, animDuration).SetEase(Ease.OutQuad);
        isHovered = false;

        handView?.OnCardDragEnded(this);
    }

    void CleanupDragOnSuccess()
    {
        Interactions.Instance.PlayerIsDragging = false;
        Interactions.Instance.PlayerIsTargeting = false;
        ManualTargetSystem.Instance.StopTargeting();
        if (targetingCard == this) targetingCard = null;
        HexGrid.ClearAllHighlights();
        state = DragState.None;
        isHovered = false;
        RestoreSortingOrder();
        handView?.ClearDragState();
    }

    // ==================== Sorting Order ====================

    void BringToFront()
    {
        for (int i = 0; i < allRenderers.Length; i++)
            allRenderers[i].sortingOrder = origSortingOrders[i] + sortingOrderBoost;
    }

    void RestoreSortingOrder()
    {
        for (int i = 0; i < allRenderers.Length; i++)
            allRenderers[i].sortingOrder = origSortingOrders[i];
    }
}