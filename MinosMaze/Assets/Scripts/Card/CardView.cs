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
    [SerializeField] private float dragAlpha = 0.4f;
    [SerializeField] private float animDuration = 0.25f;

    public Card Card { get; private set; }
    public HandView handView;
    public int handIndex;

    private Vector3 dragStartPosition;
    private Quaternion dragStartRotation;
    private Vector3 dragStartScale;

    private static CardView targetingCard;
    private static CardView hoveredCard;
    private static CardView previousHoveredCard;
    private float hoverOtherCardTimer;

    private bool isHovered;
    private bool isReturning;
    private Vector3 wrapperOrigLocalPos;
    private Vector3 wrapperOrigLocalScale;
    private SpriteRenderer[] allRenderers;
    private TMP_Text[] allTexts;

    void Awake()
    {
        allRenderers = GetComponentsInChildren<SpriteRenderer>();
        allTexts = GetComponentsInChildren<TMP_Text>();
        wrapperOrigLocalPos = wrapper.transform.localPosition;
        wrapperOrigLocalScale = wrapper.transform.localScale;
        dragStartScale = transform.localScale;
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
        if (isReturning) return;
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

        EndHoverImmediate();

        dragStartPosition = transform.position;
        dragStartRotation = transform.rotation;
        dragStartScale = transform.localScale;

        ManualTargetSystem.Instance.StartTargeting(dragStartPosition);

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
        SetAlpha(dragAlpha, 0.15f);

        transform.rotation = Quaternion.Euler(0, 0, 0);
        transform.position = MouseUtil.GetMousePositionInWorldSpace(-1);
    }

    void OnMouseDrag()
    {
        if (!Interactions.Instance.PlayerCanInteract()) return;
        transform.position = MouseUtil.GetMousePositionInWorldSpace(-1);
    }

    void OnMouseUp()
    {
        if (!Interactions.Instance.PlayerCanInteract())
        {
            CancelDrag();
            return;
        }

        if (Card.ManualTargetEffect != null)
        {
            ResolveTargeting();
            return;
        }

        bool hasCost = CostSystem.Instance.HasEnoughCost(Card.Cost);
        bool hitSomething = Physics.Raycast(transform.position, Vector3.forward, out RaycastHit hit, 10f, dropLayer);

        if (hasCost && hitSomething)
        {
            if (hit.transform.tag == "Card")
            {
                CancelDrag();
            }
            else
            {
                CleanupDragOnSuccess();
                PlayCardGA playCardGA = new(Card);
                ActionSystem.Instance.Perform(playCardGA);
            }
        }
        else
        {
            CancelDrag();
        }
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
        isHovered = false;
        handView?.OnCardUnhovered(this);

        wrapper.transform.DOKill();
        wrapper.transform.DOLocalMove(wrapperOrigLocalPos, animDuration).SetEase(Ease.OutQuad);
        wrapper.transform.DOScale(wrapperOrigLocalScale, animDuration).SetEase(Ease.OutQuad);
    }

    void EndHoverImmediate()
    {
        if (!isHovered) return;
        isHovered = false;
        if (hoveredCard == this) hoveredCard = null;
        if (previousHoveredCard == this) previousHoveredCard = null;
        wrapper.transform.DOKill();
        wrapper.transform.localPosition = wrapperOrigLocalPos;
        wrapper.transform.localScale = wrapperOrigLocalScale;
        handView?.OnCardUnhovered(this);
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

        isReturning = true;
        SetAlpha(1f, 0.15f);
        handView?.OnCardDragEnded(this);
        DOVirtual.DelayedCall(animDuration + 0.55f, () => { if (this) isReturning = false; });
    }

    void CleanupDragOnSuccess()
    {
        Interactions.Instance.PlayerIsDragging = false;
        Interactions.Instance.PlayerIsTargeting = false;
        ManualTargetSystem.Instance.StopTargeting();
        if (targetingCard == this) targetingCard = null;
        HexGrid.ClearAllHighlights();
        handView?.ClearDragState();
    }

    // ==================== Alpha ====================

    void SetAlpha(float alpha, float duration = 0f)
    {
        foreach (var sr in allRenderers)
        {
            sr.DOKill();
            sr.DOFade(alpha, duration);
        }
        foreach (var t in allTexts)
        {
            t.DOKill();
            t.DOFade(alpha, duration);
        }
    }
}