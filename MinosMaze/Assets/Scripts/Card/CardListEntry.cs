using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class CardListEntry : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private Image background;
    [SerializeField] private Image cardImage;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text costText;

    private Card card;

    public void SetUp(Card card)
    {
        this.card = card;
        if (nameText != null) nameText.text = card.Name;
        if (costText != null) costText.text = card.Cost.ToString();
        if (cardImage != null) cardImage.sprite = card.Image;
        if (background != null) background.sprite = card.Background;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (CardViewHoverSystem.Instance == null || card == null) return;

        Vector3 worldPos = Camera.main.ScreenToWorldPoint(
            new Vector3(Input.mousePosition.x, Input.mousePosition.y, 5f)
        );
        CardViewHoverSystem.Instance.Show(card, worldPos);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (CardViewHoverSystem.Instance != null)
            CardViewHoverSystem.Instance.Hide();
    }
}
