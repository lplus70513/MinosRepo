using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RandomPlayFromHandGA : GameAction
{
    public int Amount { get; private set; }

    public RandomPlayFromHandGA(int amount)
    {
        Amount = amount;
    }
}
