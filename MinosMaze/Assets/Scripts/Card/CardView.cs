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
        if (!Interactions.Instance.PlayerCanHover()) return;
        wrapper.SetActive(false);
        Vector3 pos = new(transform.position.x, -2, 0);
        CardViewHoverSystem.Instance.Show(Card, pos);
    }

    void OnMouseExit()
    {
        if (!Interactions.Instance.PlayerCanHover()) return;
        CardViewHoverSystem.Instance.Hide();
        wrapper.SetActive(true);
    }

    void OnMouseDown()
    {
        Debug.Log($"[CardView] OnMouseDown: {Card.Name}"); // 调试：鼠标按下

        if (!Interactions.Instance.PlayerCanInteract())
        {
            Debug.LogWarning("[CardView] Player cannot interact (Blocked by Interactions).");
            return;
        }

        // 检查是否有指向性效果
        if (Card.ManualTargetEffect != null)
        {
            Debug.Log("[CardView] Detected ManualTargetEffect. Starting Targeting System.");
            ManualTargetSystem.Instance.StartTargeting(transform.position);
        }
        else
        {
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

        // 如果是指向性卡牌，通常不需要拖动卡牌本身跟随鼠标，而是由 ManualTargetSystem 处理箭头
        if (Card.ManualTargetEffect != null)
        {
            // 可以在这里加一个Log看看是否被拦截了
            // Debug.Log("Dragging Targeted Card (Arrow should update)..."); 
            return;
        }

        transform.position = MouseUtil.GetMousePositionInWorldSpace(-1);
    }

    void OnMouseUp()
    {
        Debug.Log("[CardView] OnMouseUp triggered."); // 调试：鼠标抬起

        if (!Interactions.Instance.PlayerCanInteract())
        {
            Debug.LogWarning("[CardView] Player cannot interact on release.");
            return;
        }

        // --- 分支 1：指向性卡牌 ---
        if (Card.ManualTargetEffect != null)
        {
            Debug.Log("[CardView] Ending Targeting System...");
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
            }
            else
            {
                Debug.Log("[CardView] Conditions failed. Card returns to hand (implicit).");
            }
        }
        // --- 分支 2：非指向性卡牌 ---
        else
        {
            Debug.Log("[CardView] Non-targeted card logic.");
            bool hasCost = CostSystem.Instance.HasEnoughCost(Card.Cost);

            // 射线检测
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
    }
}