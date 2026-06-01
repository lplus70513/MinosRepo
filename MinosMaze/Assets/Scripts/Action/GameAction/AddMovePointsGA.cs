using UnityEngine;

public class AddMovePointsGA : GameAction
{
    public int Amount { get; set; }

    public AddMovePointsGA(int amount)
    {
        Amount = amount;
    }
}
