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

        List<EnemyData> enemies;
        List<Vector2Int> spawns;
        Vector2Int heroCoord;
        RewardConfig reward;

        if (combat != null)
        {
            enemies = combat.enemyDatas;
            spawns = combat.enemySpawnCoords;
            heroCoord = combat.heroSpawnCoord;
            reward = combat.rewardConfig ?? rewardConfig;
        }
        else
        {
            enemies = enemyDatas;
            spawns = enemySpawnCoords;
            heroCoord = heroSpawnCoord;
            reward = rewardConfig;
        }

        if (enemies == null || enemies.Count == 0)
        {
            Debug.LogError("[MatchSetupSystem] 敌人列表为空，无法初始化战斗。请配置 CombatPool 或在 Inspector 中填写 enemyDatas。");
            return;
        }

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
            deck = heroData.Deck.FindAll(cd => cd != null).ConvertAll(cd => new DeckCardEntry(cd, false));
        else
            deck = new List<DeckCardEntry>();

        CardSystem.Instance.SetUp(deck);
        PerkSystem.Instance.AddPerk(new Perk(perkData));

        DrawCardsGA drawCardsGA = new(5);
        ActionSystem.Instance.Perform(drawCardsGA);
    }

    private CombatConfig PickWeightedRandom(EncounterConfig pending)
    {
        if (combatPool == null || combatPool.configs == null || combatPool.configs.Count == 0)
            return null;

        List<CombatConfig> candidates = new();
        int totalWeight = 0;

        if (pending != null)
        {
            int floor = pending.floorLevel;
            foreach (var cfg in combatPool.configs)
            {
                if (cfg == null) continue;
                if (cfg.minFloor <= floor && floor <= cfg.maxFloor && cfg.weight > 0)
                {
                    candidates.Add(cfg);
                    totalWeight += cfg.weight;
                }
            }
        }

        if (candidates.Count == 0)
        {
            candidates = combatPool.configs.FindAll(c => c != null && c.weight > 0);
            foreach (var cfg in candidates)
                totalWeight += cfg.weight;
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
