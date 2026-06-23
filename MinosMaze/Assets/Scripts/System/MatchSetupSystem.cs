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

    [Header("BOSS 战斗池（中心格专属）")]
    [SerializeField] private CombatPoolSO bossCombatPool;

    private void Start()
    {
        StartCoroutine(DelayedSetup());
    }

    private IEnumerator DelayedSetup()
    {
        Debug.Log($"[MatchSetupSystem] 初始化开始 {gameObject.scene.name}");

        _ = MoveSystem.Instance;
        _ = PlayerMovementSystem.Instance;
        PlayerMovementSystem.Instance.ResetForBattle();
        _ = BattleResultSystem.Instance;
        BattleResultSystem.Instance.ResetForBattle();
        _ = RewardSystem.Instance;

        var gm = GameManager.Instance;

        bool isBoss = gm != null && gm.IsBossEncounter;
        CombatPoolSO activePool = isBoss ? bossCombatPool : combatPool;

        CombatConfig combat = PickWeightedRandom(gm?.PendingEncounter, activePool);

        if (combat != null && combat.useCustomMap)
        {
            var hexGrid = FindObjectOfType<HexGrid>();
            if (hexGrid != null)
                hexGrid.RebuildFromConfig(combat.mapRadius, combat.specialCells);
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

        if (enemies == null || enemies.Count == 0)
        {
            Debug.LogError("[MatchSetupSystem] 敌人列表为空，无法初始化战斗。请配置 CombatPool 或在 Inspector 中填写 enemyDatas。");
            yield break;
        }

        if (reward != null)
            RewardSystem.Instance.SetConfig(reward);

        HeroSystem.Instance.Setup(heroData, heroCoord);

        var camCtrl = FindObjectOfType<CameraController>();
        if (camCtrl != null && HeroSystem.Instance.HeroView != null)
        {
            camCtrl.CenterOn(HeroSystem.Instance.HeroView.transform.position);
        }

        if (gm != null && gm.WorldMapState != null && gm.WorldMapState.maxHealth > 0)
        {
            HeroSystem.Instance.HeroView.SetMaxHealth(gm.WorldMapState.maxHealth);
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
        if (perkData != null)
            PerkSystem.Instance.AddPerk(new Perk(perkData));

        var sts = SceneTransitionSystem.Instance;
        if (sts != null)
            yield return new WaitUntil(() => !sts.IsTransitioning);

        EnemySystem.Instance.ComputeAndStoreNextTurnIntents();
        foreach (var enemy in EnemySystem.Instance.Enemies)
            enemy.ShowIntents();

        DrawCardsGA drawCardsGA = new(5);
        ActionSystem.Instance.Perform(drawCardsGA);
        _ = BlessingSystem.Instance;
        yield return BlessingSystem.Instance.ApplyBattleStartBlessingsCoroutine();
        Debug.Log($"[MatchSetupSystem] 初始化结束 {gameObject.scene.name}");
    }

    private CombatConfig PickWeightedRandom(EncounterConfig pending, CombatPoolSO pool)
    {
        if (pool == null || pool.configs == null || pool.configs.Count == 0)
            return null;

        List<CombatConfig> candidates = new();
        int totalWeight = 0;

        if (pending != null)
        {
            int floor = pending.floorLevel;
            int difficulty = pending.difficultyLevel;
            foreach (var cfg in pool.configs)
            {
                if (cfg == null) continue;
                if (cfg.weight <= 0) continue;
                if (cfg.minFloor > floor || cfg.maxFloor < floor) continue;
                // 难度等级过滤（difficultyLevel>0 才生效，向后兼容旧存档）
                if (difficulty > 0 && cfg.difficultyLevel != difficulty) continue;
                candidates.Add(cfg);
                totalWeight += cfg.weight;
            }
        }

        if (candidates.Count == 0)
        {
            candidates = pool.configs.FindAll(c => c != null && c.weight > 0);
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
