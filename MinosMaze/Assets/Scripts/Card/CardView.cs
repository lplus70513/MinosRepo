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
        Debug.Log($"[CardView] OnMouseDown: {Card.Name}");

        if (!Interactions.Instance.PlayerCanInteract())
        {
            Debug.LogWarning("[CardView] Player cannot interact (Blocked by Interactions).");
            return;
        }

        if (Card.ManualTargetEffect != null)
        {
            if (Interactions.Instance.PlayerIsDragging) return;
            Debug.Log("[CardView] Detected ManualTargetEffect. Starting Targeting System.");
            Interactions.Instance.PlayerIsTargeting = true;
            targetingCard = this;
            ManualTargetSystem.Instance.StartTargeting(transform.position);
        }
        else
        {
            if (targetingCard != null) targetingCard.CancelTargeting();
            Debug.Log("[CardView] No ManualTargetEffect. Starting Drag logic.");
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
            CancelTargeting();
            return;
        }

        Debug.Log("[CardView] Non-targeted card logic.");
        bool hasCost = CostSystem.Instance.HasEnoughCost(Card.Cost);

        bool hitSomething = Physics.Raycast(transform.position, Vector3.forward, out RaycastHit hit, 10f, dropLayer);

        if (hasCost && hitSomething)
        {
            Debug.Log($"[CardView] Raycast Hit: {hit.transform.name}, Tag: {hit.transform.tag}");

            if (hit.transform.tag == "Card")
            {
                Debug.Log("[CardView] Hit another Card. Returning to start.");
                transform.position = dragStartPosition;
                transform.rotation = dragStartRotation;
            }
            else
            {
                Debug.Log("[CardView] SUCCESS: Performing PlayCardGA (No Target).");
                PlayCardGA playCardGA = new(Card);
                ActionSystem.Instance.Perform(playCardGA);
            }
        }
        else
        {
            if (!hasCost) Debug.LogWarning("[CardView] Not enough cost.");
            if (!hitSomething) Debug.LogWarning("[CardView] Raycast hit nothing valid.");

            Debug.Log("[CardView] Returning card to hand.");
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
                Debug.Log("[CardView] Hovering over another card for 0.5s, cancel targeting.");
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

        Vector3 mousePos = MouseUtil.GetMousePositionInWorldSpace(-1);
        EnemyView target = ManualTargetSystem.Instance.EndTargeting(mousePos);

        if (target == null)
        {
            Debug.LogWarning("[CardView] Target is NULL. Did you release over an enemy?");
        }
        else
        {
            Debug.Log($"[CardView] Target acquired: {target.name}");
        }

        bool hasCost = CostSystem.Instance.HasEnoughCost(Card.Cost);
        if (!hasCost) Debug.LogWarning("[CardView] Not enough Cost!");

        if (target != null && hasCost)
        {
            Debug.Log("[CardView] SUCCESS: Performing PlayCardGA with Target.");
            PlayCardGA playCardGA = new(Card, target);
            ActionSystem.Instance.Perform(playCardGA);
            return;
        }

        Debug.Log("[CardView] Conditions failed. Card remains in hand.");
        CardViewHoverSystem.Instance.Hide();
        wrapper.SetActive(true);
    }

    private void CancelTargeting()
    {
        CleanupTargeting();
        Debug.Log("[CardView] Targeting cancelled.");
        CardViewHoverSystem.Instance.Hide();
        wrapper.SetActive(true);
        hoverOtherCardTimer = 0f;
    }

    private void CleanupTargeting()
    {
        if (targetingCard == this) targetingCard = null;
        Interactions.Instance.PlayerIsTargeting = false;
        ManualTargetSystem.Instance.StopTargeting();
    }
}
