using UnityEngine;

public class CardViewHoverSystem : Singleton<CardViewHoverSystem>
{
    [SerializeField] private CardView cardViewHover;

    public void Show(Card card, Vector3 position)
    {
        cardViewHover.GameObject.SetActive(true);
        cardViewHover.transform.position = position;
    }

    public void Hide() 
    {
        cardViewHover.gameObject.SetActive(false);
    }
}
