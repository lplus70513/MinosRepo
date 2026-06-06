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

    [Header("遭遇配置池（按层数过滤 + 权重随机）")]
    [SerializeField] private CombatPoolSO combatPool;

    private void Start()
    {
        _ = MoveSystem.Instance;
        _ = PlayerMovementSystem.Instance;
        _ = BattleResultSystem.Instance;
        _ = RewardSystem.Instance;

        var gm = GameManager.Instance;

        CombatConfig combat = PickWeightedRandom(gm?.PendingEncounter);

        List<EnemyData> enemies = (combat?.enemyDatas != null && combat.enemyDatas.Count > 0)
            ? combat.enemyDatas : enemyDatas;
        List<Vector2Int> spawns = (combat?.enemySpawnCoords != null && combat.enemySpawnCoords.Count > 0)
            ? combat.enemySpawnCoords : enemySpawnCoords;
        Vector2Int heroCoord = (combat != null) ? combat.heroSpawnCoord : heroSpawnCoord;
        RewardConfig reward = combat?.rewardConfig ?? rewardConfig;

        if (reward != null)
            RewardSystem.Instance.SetConfig(reward);

        HeroSystem.Instance.Setup(heroData, heroCoord);
        if (gm != null && gm.WorldMapState != null && gm.WorldMapState.maxHealth > 0)
        {
            HeroSystem.Instance.HeroView.SetCurrentHealth(gm.WorldMapState.currentHealth);
        }
        EnemySystem.Instance.Setup(enemies, spawns);
        if (healthBarPanel != null)
            healthBarPanel.SetupBattle(HeroSystem.Instance.HeroView, EnemySystem.Instance.Enemies);
        List<DeckCardEntry> deck;
        if (gm != null && gm.WorldMapState != null && gm.WorldMapState.currentDeck != null && gm.WorldMapState.currentDeck.Count > 0)
            deck = gm.WorldMapState.currentDeck;
        else if (heroData != null && heroData.Deck != null)
            deck = heroData.Deck.ConvertAll(cd => new DeckCardEntry(cd, false));
        else
            deck = new List<DeckCardEntry>();

        CardSystem.Instance.SetUp(deck);
        PerkSystem.Instance.AddPerk(new Perk(perkData));

        DrawCardsGA drawCardsGA = new(5);
        ActionSystem.Instance.Perform(drawCardsGA);
    }

    private CombatConfig PickWeightedRandom(EncounterConfig pending)
    {
        if (pending == null || combatPool == null || combatPool.configs == null || combatPool.configs.Count == 0)
            return null;

        int floor = pending.floorLevel;
        List<CombatConfig> candidates = new();
        int totalWeight = 0;
        foreach (var cfg in combatPool.configs)
        {
            if (cfg == null) continue;
            if (cfg.minFloor <= floor && floor <= cfg.maxFloor && cfg.weight > 0)
            {
                candidates.Add(cfg);
                totalWeight += cfg.weight;
            }
        }

        if (candidates.Count == 0 || totalWeight <= 0)
            return null;

        int roll = Random.Range(0, totalWeight);
        int cumulative = 0;
        foreach (var cfg in candidates)
        {
            cumulative += cfg.weight;
            if (roll < cumulative)
                return cfg;
        }
        return candidates[candidates.Count - 1];
    }
}
