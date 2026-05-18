using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class CardView : MonoBehaviour
{
    [SerializeField] private TMP_Text Name;
    [SerializeField] private TMP_Text CostText;
    [SerializeField] private TMP_Text Description;

    [SerializeField] private GameObject wrapper;
    [SerializeField] private LayerMask dropLayer;
    [SerializeField] private SpriteRenderer image;
    [SerializeField] private SpriteRenderer background;

    public Card Card { get; private set; }

    private Vector3 dragStartPosition;
    private Quaternion dragStartRotation;

    private static CardView targetingCard;
    private static CardView hoveredCard;
    private float hoverOtherCardTimer;

    public void SetUp(Card card)
    {
        Card = card;
        Name.text = card.Name;
        Description.text = card.Description;
        CostText.text = card.Cost.ToString();
        image.sprite = card.Image;
        background.sprite = card.Background;
    }

    void OnMouseEnter()
    {
        hoveredCard = this;
        if (!Interactions.Instance.PlayerCanHover()) return;
        wrapper.SetActive(false);
        Vector3 pos = new(transform.position.x, -2, 0);
        CardViewHoverSystem.Instance.Show(Card, pos);
    }

    void OnMouseExit()
    {
        if (hoveredCard == this) hoveredCard = null;
        if (!Interactions.Instance.PlayerCanHover()) return;
        CardViewHoverSystem.Instance.Hide();
        wrapper.SetActive(true);
    }

    void OnMouseDown()
    {

        if (!Interactions.Instance.PlayerCanInteract())
        {
            return;
        }

        if (Card.ManualTargetEffect != null)
        {
            if (Interactions.Instance.PlayerIsDragging) return;
            Interactions.Instance.PlayerIsTargeting = true;
            targetingCard = this;
            ManualTargetSystem.Instance.StartTargeting(transform.position);
            if (Card.HasAttackRange)
            {
                HeroView hero = HeroSystem.Instance.HeroView;
                HexGrid.HighlightCellsInRange(hero.HexCoordX, hero.HexCoordZ, Card.AttackRange);
            }
        }
        else
        {
            if (targetingCard != null) targetingCard.CancelTargeting();
            Interactions.Instance.PlayerIsDragging = true;
            wrapper.SetActive(true);
            CardViewHoverSystem.Instance.Hide();
            dragStartPosition = transform.position;
            dragStartRotation = transform.rotation;
            transform.rotation = Quaternion.Euler(0, 0, 0);
            transform.position = MouseUtil.GetMousePositionInWorldSpace(-1);
        }
    }

    void OnMouseDrag()
    {
        if (!Interactions.Instance.PlayerCanInteract()) return;

        if (Card.ManualTargetEffect != null)
        {
            return;
        }

        transform.position = MouseUtil.GetMousePositionInWorldSpace(-1);
    }

    void OnMouseUp()
    {
        if (!Interactions.Instance.PlayerCanInteract())
        {
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
                transform.position = dragStartPosition;
                transform.rotation = dragStartRotation;
            }
            else
            {
                PlayCardGA playCardGA = new(Card);
                ActionSystem.Instance.Perform(playCardGA);
            }
        }
        else
        {
            transform.position = dragStartPosition;
            transform.rotation = dragStartRotation;
        }
        Interactions.Instance.PlayerIsDragging = false;
    }

    void Update()
    {
        if (targetingCard != this) return;

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
    }

    private void ResolveTargeting()
    {
        CleanupTargeting();

        if (!Interactions.Instance.PlayerCanInteract()) return;

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
            PlayCardGA playCardGA = new(Card, target);
            ActionSystem.Instance.Perform(playCardGA);
            return;
        }

        CardViewHoverSystem.Instance.Hide();
        wrapper.SetActive(true);
    }

    private void CancelTargeting()
    {
        CleanupTargeting();
        CardViewHoverSystem.Instance.Hide();
        wrapper.SetActive(true);
        hoverOtherCardTimer = 0f;
    }

    private void CleanupTargeting()
    {
        if (targetingCard == this) targetingCard = null;
        Interactions.Instance.PlayerIsTargeting = false;
        ManualTargetSystem.Instance.StopTargeting();
        HexGrid.ClearAllHighlights();
    }
}