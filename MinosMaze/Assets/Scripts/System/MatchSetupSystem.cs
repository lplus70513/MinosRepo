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
        StartCoroutine(DelayedSetup());
    }

    private IEnumerator DelayedSetup()
    {
        Debug.Log($"[MatchSetupSystem] === 初始化开始 === scene={gameObject.scene.name}");

        _ = MoveSystem.Instance;
        _ = PlayerMovementSystem.Instance;
        PlayerMovementSystem.Instance.ResetForBattle();
        _ = BattleResultSystem.Instance;
        BattleResultSystem.Instance.ResetForBattle();
        _ = RewardSystem.Instance;

        var gm = GameManager.Instance;
        Debug.Log($"[MatchSetupSystem] GameManager={(gm != null ? "OK" : "NULL")}, PendingEncounter={(gm?.PendingEncounter != null ? $"cellType={gm.PendingEncounter.cellType} floor={gm.PendingEncounter.floorLevel}" : "NULL")}");

        CombatConfig combat = PickWeightedRandom(gm?.PendingEncounter);
        Debug.Log($"[MatchSetupSystem] combatPool={(combatPool != null ? combatPool.name : "NULL")}, combat={(combat != null ? combat.configName : "NULL (使用 fallback)")}");

        if (combat != null && combat.useCustomMap)
        {
            var hexGrid = FindObjectOfType<HexGrid>();
            if (hexGrid != null)
            {
                hexGrid.RebuildFromConfig(combat.mapRadius, combat.specialCells);
                Debug.Log($"[MatchSetupSystem] 已根据 CombatConfig 重建地图: radius={combat.mapRadius}, specialCells={combat.specialCells?.Count ?? 0}");
            }
            else
            {
                Debug.LogWarning("[MatchSetupSystem] 场景中未找到 HexGrid，无法重建地图");
            }
        }

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
        Debug.Log($"[MatchSetupSystem] enemies count={enemies?.Count ?? 0}, spawns count={spawns?.Count ?? 0}, heroCoord={heroCoord}");

        if (enemies == null || enemies.Count == 0)
        {
            Debug.LogError("[MatchSetupSystem] 敌人列表为空，无法初始化战斗。请配置 CombatPool 或在 Inspector 中填写 enemyDatas。");
            yield break;
        }

        if (reward != null)
            RewardSystem.Instance.SetConfig(reward);

        Debug.Log($"[MatchSetupSystem] 调用 HeroSystem.Setup, heroData={(heroData != null ? heroData.name : "NULL")}, coord={heroCoord}");
        HeroSystem.Instance.Setup(heroData, heroCoord);
        Debug.Log($"[MatchSetupSystem] HeroSystem.Setup 完成, HeroView={(HeroSystem.Instance.HeroView != null ? "存在" : "NULL")}");

        var camCtrl = FindObjectOfType<CameraController>();
        if (camCtrl != null && HeroSystem.Instance.HeroView != null)
        {
            camCtrl.CenterOn(HeroSystem.Instance.HeroView.transform.position);
            Debug.Log($"[MatchSetupSystem] 摄像机已居中到英雄位置 {HeroSystem.Instance.HeroView.transform.position}");
        }

        if (gm != null && gm.WorldMapState != null && gm.WorldMapState.maxHealth > 0)
        {
            HeroSystem.Instance.HeroView.SetCurrentHealth(gm.WorldMapState.currentHealth);
        }
        if (gm != null && gm.WorldMapState != null)
        {
            gm.WorldMapState.maxHealth = HeroSystem.Instance.HeroView.MaxHealth;
        }
        Debug.Log($"[MatchSetupSystem] 调用 EnemySystem.Setup, enemies count={enemies?.Count ?? 0}");
        EnemySystem.Instance.Setup(enemies, spawns);
        Debug.Log($"[MatchSetupSystem] EnemySystem.Setup 完成, Enemies={(EnemySystem.Instance.Enemies != null ? EnemySystem.Instance.Enemies.Count : 0)}");
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
        Debug.Log($"[MatchSetupSystem] 场景初始化完成, 等待淡入结束再抽牌");

        if (SceneTransitionSystem.Instance != null)
            yield return new WaitUntil(() => !SceneTransitionSystem.Instance.IsTransitioning);

        Debug.Log($"[MatchSetupSystem] === 计算首轮敌人意图并抽牌 === scene={gameObject.scene.name}");
        EnemySystem.Instance.ComputeAndStoreNextTurnIntents();
        foreach (var enemy in EnemySystem.Instance.Enemies)
            enemy.ShowIntents();

        _ = BlessingSystem.Instance;
        BlessingSystem.Instance.ApplyBattleStartBlessings();

        Debug.Log($"[MatchSetupSystem] === 抽牌 === scene={gameObject.scene.name}");
        DrawCardsGA drawCardsGA = new(5);
        ActionSystem.Instance.Perform(drawCardsGA);
        Debug.Log($"[MatchSetupSystem] DrawCardsGA 已提交, ActionSystem.IsPerforming={ActionSystem.Instance.IsPerforming}");
        Debug.Log($"[MatchSetupSystem] === Start 结束 ===");
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
