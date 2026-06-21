using System.Collections.Generic;
using System.Text.RegularExpressions;
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

    public SpriteRenderer Background => background;

    [Header("升级高亮")]
    [SerializeField] private Color upgradeHighlightColor = Color.green;

    [Header("动画参数")]
    [SerializeField] private float hoverScale = 1.2f;
    [SerializeField] private float hoverYOffset = 1.5f;
    [SerializeField] private float hoverZOffset = -0.5f;
    [SerializeField] private float animDuration = 0.25f;

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

    private Color defaultNameColor;
    private Color defaultDescColor;
    private Color defaultCostColor;
    private bool colorsInitialized;
    private bool isTransitioning;
    private Vector3 wrapperOrigLocalPos;
    private Vector3 wrapperOrigLocalScale;
    private Renderer[] allRenderers;
    private int[] origSortingLayerIDs;
    private int[] origSortingOrders;
    private int[] origRenderQueues;
    private Canvas[] allCanvases;
    private int[] origCanvasSortingLayerIDs;
    private int[] origCanvasSortingOrders;

    private bool useLiveDamage;
    private EnemyView lastHoveredTarget;
    public EnemyView LastHoveredTarget => lastHoveredTarget;
    public bool UseLiveDamage => useLiveDamage;

    void Awake()
    {
        wrapperOrigLocalPos = wrapper.transform.localPosition;
        wrapperOrigLocalScale = wrapper.transform.localScale;
        dragStartScale = transform.localScale;

        allRenderers = GetComponentsInChildren<Renderer>(true);
        origSortingLayerIDs = new int[allRenderers.Length];
        origSortingOrders = new int[allRenderers.Length];
        origRenderQueues = new int[allRenderers.Length];
        for (int i = 0; i < allRenderers.Length; i++)
        {
            origSortingLayerIDs[i] = allRenderers[i].sortingLayerID;
            origSortingOrders[i] = allRenderers[i].sortingOrder;
            origRenderQueues[i] = allRenderers[i].sharedMaterial.renderQueue;
        }

        allCanvases = GetComponentsInChildren<Canvas>(true);
        origCanvasSortingLayerIDs = new int[allCanvases.Length];
        origCanvasSortingOrders = new int[allCanvases.Length];
        for (int i = 0; i < allCanvases.Length; i++)
        {
            origCanvasSortingLayerIDs[i] = allCanvases[i].sortingLayerID;
            origCanvasSortingOrders[i] = allCanvases[i].sortingOrder;
        }
    }

    public void SetUp(Card card, bool useLiveDamage = false)
    {
        Card = card;
        this.useLiveDamage = useLiveDamage;
        lastHoveredTarget = null;
        Name.text = card.Name;
        CostText.text = card.Cost.ToString();
        image.sprite = card.Image;
        background.sprite = card.Background;

        if (!colorsInitialized)
        {
            if (Name != null) defaultNameColor = Name.color;
            if (Description != null) defaultDescColor = Description.color;
            if (CostText != null) defaultCostColor = CostText.color;
            colorsInitialized = true;
        }

        ResetTextColors();
        if (card.IsUpgraded && card.CardData != null)
            ApplyUpgradeHighlight(card);
        else
            RefreshDescription(null);
    }

    private void ResetTextColors()
    {
        if (Name != null) Name.color = defaultNameColor;
        if (Description != null) Description.color = defaultDescColor;
        if (CostText != null) CostText.color = defaultCostColor;
    }

    private void ApplyUpgradeHighlight(Card card)
    {
        Card baseCard = new Card(card.CardData, isUpgraded: false);

        if (card.Name != baseCard.Name && Name != null)
            Name.color = upgradeHighlightColor;
        if (card.Cost != baseCard.Cost && CostText != null)
            CostText.color = upgradeHighlightColor;
        if (card.Description != baseCard.Description && Description != null)
        {
            CombatantView caster = useLiveDamage ? HeroSystem.Instance?.HeroView : null;
            string baseDesc = useLiveDamage && caster != null
                ? baseCard.GetLiveDescription(caster, null)
                : baseCard.Description;
            string upgradedDesc = useLiveDamage && caster != null
                ? card.GetLiveDescription(caster, null)
                : card.Description;
            Description.text = HighlightChangedNumbers(baseDesc, upgradedDesc);
        }
    }

    private string HighlightChangedNumbers(string baseText, string upgradedText)
    {
        string hexColor = ColorUtility.ToHtmlStringRGB(upgradeHighlightColor);
        var baseMatches = Regex.Matches(baseText, @"\d+");
        var upgradeMatches = Regex.Matches(upgradedText, @"\d+");

        if (upgradeMatches.Count == 0)
            return $"<color=#{hexColor}>{upgradedText}</color>";

        var baseNums = new List<string>();
        foreach (Match m in baseMatches)
            baseNums.Add(m.Value);

        string result = upgradedText;
        for (int i = upgradeMatches.Count - 1; i >= 0; i--)
        {
            Match m = upgradeMatches[i];
            string upgradeNum = m.Value;
            string baseNum = i < baseNums.Count ? baseNums[i] : "";
            if (upgradeNum != baseNum)
            {
                string colored = $"<color=#{hexColor}>{upgradeNum}</color>";
                result = result.Remove(m.Index, m.Length).Insert(m.Index, colored);
            }
        }

        if (result == upgradedText)
            return $"<color=#{hexColor}>{upgradedText}</color>";
        return result;
    }

    public void RefreshDescription(CombatantView target)
    {
        if (Description == null || Card == null) return;
        if (!useLiveDamage)
        {
            Description.text = Card.Description;
            return;
        }
        CombatantView caster = HeroSystem.Instance?.HeroView;
        if (caster == null)
        {
            Description.text = Card.Description;
            return;
        }

        if (Card.IsUpgraded && Card.CardData != null)
        {
            Card baseCard = new Card(Card.CardData, isUpgraded: false);
            string baseDesc = baseCard.GetLiveDescription(caster, target);
            string upgradedDesc = Card.GetLiveDescription(caster, target);
            Description.text = HighlightChangedNumbers(baseDesc, upgradedDesc);
        }
        else
        {
            Description.text = Card.GetLiveDescription(caster, target);
        }
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

        if (CardSystem.Instance.IsSelectingCardFromHand)
        {
            CardSystem.Instance.OnHandCardSelected(this);
            return;
        }

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
        if (CardSystem.Instance.IsSelectingCardFromHand) return;

        bool inPlayArea = IsMouseInPlayArea();

        if (Card.ManualTargetEffect != null)
        {
            if (inPlayArea && !wasInPlayArea && !isTransitioning)
            {
                EnterPlayAreaTargeting();
            }
            else if (!inPlayArea && wasInPlayArea && !isTransitioning)
            {
                LeavePlayAreaTargeting();
            }

            if (state == DragState.DraggingNonPlay && !isTransitioning)
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

        if (CardSystem.Instance.IsSelectingCardFromHand) return;

        var hero = HeroSystem.Instance?.HeroView;
        if (hero != null)
        {
            if (hero.HasStatusEffect(StatusEffectType.STUN))
            {
                CancelDrag();
                return;
            }

            if (hero.HasStatusEffect(StatusEffectType.ROOT))
            {
                bool isAttackCard = Card.ManualTargetEffect is DealDamageEffect
                    || (Card.OtherEffects != null && Card.OtherEffects.Exists(e => e.Effect is DealDamageEffect));
                if (!isAttackCard)
                {
                    CancelDrag();
                    return;
                }
            }
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
        isTransitioning = true;
        state = DragState.DraggingPlay;
        lastHoveredTarget = null;
        RefreshDescription(null);
        Vector3 target = handView != null ? handView.GetHandCenterPosition() : transform.position;
        transform.DOKill();
        transform.DOMove(target, animDuration).SetEase(Ease.OutBack, 0.7f)
            .OnComplete(() =>
            {
                ManualTargetSystem.Instance.StartTargeting(transform.position);
                isTransitioning = false;
                wasInPlayArea = true;
            });
    }

    void LeavePlayAreaTargeting()
    {
        isTransitioning = true;
        ManualTargetSystem.Instance.StopTargeting();
        state = DragState.DraggingNonPlay;
        if (lastHoveredTarget != null)
        {
            lastHoveredTarget.SetOutline(false);
            lastHoveredTarget = null;
        }
        RefreshDescription(null);
        Vector3 mousePos = MouseUtil.GetMousePositionInWorldSpace(-1);
        transform.DOKill();
        transform.DOMove(mousePos, animDuration * 0.5f).SetEase(Ease.OutQuad)
            .OnComplete(() =>
            {
                isTransitioning = false;
                wasInPlayArea = false;
            });
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

            if (state == DragState.DraggingPlay && ManualTargetSystem.Instance != null)
            {
                EnemyView currentTarget = ManualTargetSystem.Instance.GetCurrentHoveredEnemy();
                if (currentTarget != lastHoveredTarget)
                {
                    if (lastHoveredTarget != null)
                        lastHoveredTarget.SetOutline(false);
                    lastHoveredTarget = currentTarget;
                    if (currentTarget != null)
                        currentTarget.SetOutline(true);
                    RefreshDescription(currentTarget);
                }
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

        if (target != null && !Card.CanHitFlying && target.EnemyType == EnemyType.Flying)
        {
            target = null;
        }

        int effectiveCost = Card.GetEffectiveCost(target);
        bool hasCost = CostSystem.Instance.HasEnoughCost(effectiveCost);

        if (target != null && hasCost && Card.HasAttackRange)
        {
            HeroView hero = HeroSystem.Instance.HeroView;
            int dist = HexGrid.HexDistance(hero.HexCoordX, hero.HexCoordZ, target.HexCoordX, target.HexCoordZ);
            if (dist > Card.AttackRange)
                target = null;
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
        if (lastHoveredTarget != null)
        {
            lastHoveredTarget.SetOutline(false);
            lastHoveredTarget = null;
        }
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

        if (lastHoveredTarget != null)
        {
            lastHoveredTarget.SetOutline(false);
            lastHoveredTarget = null;
        }

        state = DragState.None;
        isTransitioning = false;

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

        if (lastHoveredTarget != null)
        {
            lastHoveredTarget.SetOutline(false);
            lastHoveredTarget = null;
        }

        state = DragState.None;
        isTransitioning = false;
        isHovered = false;
        RestoreSortingOrder();
        handView?.ClearDragState();
    }

    // ==================== Sorting Order ====================

    public void BringToFront()
    {
        int topID = GetTopSortingLayerID();
        for (int i = 0; i < allRenderers.Length; i++)
        {
            allRenderers[i].sortingLayerID = topID;
            allRenderers[i].sortingOrder = 32767;
            allRenderers[i].material.renderQueue = 5000;
        }
        for (int i = 0; i < allCanvases.Length; i++)
        {
            allCanvases[i].overrideSorting = true;
            allCanvases[i].sortingLayerID = topID;
            allCanvases[i].sortingOrder = 32767;
        }
    }

    public void RestoreSortingOrder()
    {
        for (int i = 0; i < allRenderers.Length; i++)
        {
            allRenderers[i].sortingLayerID = origSortingLayerIDs[i];
            allRenderers[i].sortingOrder = origSortingOrders[i];
            allRenderers[i].material.renderQueue = origRenderQueues[i];
        }
        for (int i = 0; i < allCanvases.Length; i++)
        {
            allCanvases[i].overrideSorting = false;
            allCanvases[i].sortingLayerID = origCanvasSortingLayerIDs[i];
            allCanvases[i].sortingOrder = origCanvasSortingOrders[i];
        }
    }

    private static int GetTopSortingLayerID()
    {
        int bestID = 0;
        int bestValue = int.MinValue;
        foreach (var layer in SortingLayer.layers)
        {
            if (layer.value > bestValue)
            {
                bestValue = layer.value;
                bestID = layer.id;
            }
        }
        return bestID;
    }
}