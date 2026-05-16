using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MatchSetupSystem : MonoBehaviour
{
    [SerializeField] private HeroData heroData;

    [SerializeField] private PerkData perkData;

    [SerializeField] private List<EnemyData> enemyDatas;

    private void Start()
    {
        HeroSystem.Instance.Setup(heroData);
        EnemySystem.Instance.Setup(enemyDatas);
        CardSystem.Instance.SetUp(heroData.Deck);
        PerkSystem.Instance.AddPerk(new Perk(perkData));
        DrawCardsGA drawCardsGA = new(5);
        ActionSystem.Instance.Perform(drawCardsGA);
    }
}
