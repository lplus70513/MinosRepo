using UnityEngine;

public class DrawCardEffects : Effect
{
    [SerializeField] private int drawAmount;

    public override GameAction GetGameAction()
    {
        DrawCardsGA drawCardsGA = new(drawAmount);
        return drawCardsGA;
    }
}
