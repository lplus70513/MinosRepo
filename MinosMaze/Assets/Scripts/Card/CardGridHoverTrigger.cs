using UnityEngine;

public class CardGridHoverTrigger : MonoBehaviour
{
    private Card card;

    public void Init(Card card)
    {
        this.card = card;
    }

    void OnMouseEnter()
    {
        if (CardViewHoverSystem.Instance == null || card == null) return;

        Vector3 worldPos = Camera.main.ScreenToWorldPoint(
            new Vector3(Input.mousePosition.x, Input.mousePosition.y, 5f)
        );
        CardViewHoverSystem.Instance.Show(card, worldPos);
    }

    void OnMouseExit()
    {
        if (CardViewHoverSystem.Instance != null)
            CardViewHoverSystem.Instance.Hide();
    }
}
