using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MatchSetupSystem : MonoBehaviour
{
    [Header("单位数据")]
    [SerializeField] private HeroData heroData;
    [SerializeField] private PerkData perkData;
    [SerializeField] private List<EnemyData> enemyDatas;

    [Header("奖励配置")]
    [SerializeField] private RewardConfig rewardConfig;

    [Header("UI")]
    [SerializeField] private HealthBarPanel healthBarPanel;

    [Header("六角格生成坐标")]
    [SerializeField] private Vector2Int heroSpawnCoord = Vector2Int.zero;
    [SerializeField] private List<Vector2Int> enemySpawnCoords;

    private void Start()
    {
        _ = MoveSystem.Instance;
        _ = PlayerMovementSystem.Instance;
        _ = BattleResultSystem.Instance;
        _ = RewardSystem.Instance;
        if (rewardConfig != null)
            RewardSystem.Instance.SetConfig(rewardConfig);
        HeroSystem.Instance.Setup(heroData, heroSpawnCoord);
        // 如果大地图携带了生命值，覆盖当前生命值
        var gm = GameManager.Instance;
        if (gm != null && gm.WorldMapState != null && gm.WorldMapState.maxHealth > 0)
        {
            HeroSystem.Instance.HeroView.SetCurrentHealth(gm.WorldMapState.currentHealth);
        }
        EnemySystem.Instance.Setup(enemyDatas, enemySpawnCoords);
        if (healthBarPanel != null)
            healthBarPanel.SetupBattle(HeroSystem.Instance.HeroView, EnemySystem.Instance.Enemies);
        List<CardData> deck;
        if (gm != null && gm.WorldMapState != null && gm.WorldMapState.currentDeck != null && gm.WorldMapState.currentDeck.Count > 0)
            deck = gm.WorldMapState.currentDeck;
        else if (heroData != null && heroData.Deck != null)
            deck = heroData.Deck;
        else
            deck = new List<CardData>();

        CardSystem.Instance.SetUp(deck);
        PerkSystem.Instance.AddPerk(new Perk(perkData));
        DrawCardsGA drawCardsGA = new(5);
        ActionSystem.Instance.Perform(drawCardsGA);
    }
}
