using TMPro;
using UnityEngine;

public class CardView : MonoBehaviour
{
    [SerializeField] private TMP_Text Name;
    [SerializeField] private TMP_Text CostText; 
    [SerializeField] private TMP_Text Description;

    [SerializeField] private GameObject wrapper;

    [SerializeField] private SpriteRenderer image;
    [SerializeField] private SpriteRenderer background;

    public Card Card { get; private set; }

    public void SetUp(Card card)
    {
        Card = card;
        Name.text = card.Name;
        Description.text = card.Description;
        CostText.text = card.Cost.ToString();
        image.sprite = card.Image;
    }
}