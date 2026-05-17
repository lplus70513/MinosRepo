using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class HeroView : CombatantView
{
    public void Setup(HeroData heroData, int hexX, int hexZ)
    {
        HexCoordX = hexX;
        HexCoordZ = hexZ;
        SetupBase(heroData.Health, heroData.Image);
    }
}
 