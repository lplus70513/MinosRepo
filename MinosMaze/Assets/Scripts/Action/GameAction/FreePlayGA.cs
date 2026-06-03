using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FreePlayGA : GameAction
{
    public int Amount { get; private set; }

    public FreePlayGA(int amount)
    {
        Amount = amount;
    }
}
