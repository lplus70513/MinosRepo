using UnityEngine;

public class CardViewHoverSystem : Singleton<CardViewHoverSystem>
{
    [SerializeField] private CardView cardViewHover;

    private Collider hoverCollider;

    void Awake()
    {
        if (cardViewHover != null)
            hoverCollider = cardViewHover.GetComponent<Collider>();
    }

    public void Show(Card card, Vector3 position)
    {
        if (cardViewHover == null) return;
        cardViewHover.gameObject.SetActive(true);
        cardViewHover.SetUp(card);
        cardViewHover.transform.position = position;
        if (hoverCollider != null) hoverCollider.enabled = false;
    }

    public void Hide()
    {
        if (cardViewHover == null) return;
        if (hoverCollider != null) hoverCollider.enabled = true;
        cardViewHover.gameObject.SetActive(false);
    }
}