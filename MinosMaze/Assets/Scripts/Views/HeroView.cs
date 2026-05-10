using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class HeroView : CombatantView
{
    public void Setup(HeroData heroData)
    {
        SetupBase(heroData.Health, heroData.Image);
    }
}
 