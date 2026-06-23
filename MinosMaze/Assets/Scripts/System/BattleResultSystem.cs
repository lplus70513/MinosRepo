using System.Collections;
using UnityEngine;

public class BattleResultSystem : Singleton<BattleResultSystem>
{
    [SerializeField] private GameObject winPanel;
    [SerializeField] private GameObject losePanel;

    private bool _battleEnded;
    private Interactions _interactions;
    private RewardSystem _rewardSystem;

    void OnEnable()
    {
        _battleEnded = false;

        // 直接查找当前场景实例，绕过 Singleton 静态引用（Domain Reload 关闭时可能指向已销毁对象）
        _interactions = FindObjectOfType<Interactions>();
        _rewardSystem = FindObjectOfType<RewardSystem>();

        ActionSystem.SubscribeReaction<KillEnemyGA>(OnKillEnemyPost, ReactionTiming.POST);
        ActionSystem.SubscribeReaction<DealDamageGA>(OnDealDamagePost, ReactionTiming.POST);
        ActionSystem.AttachPerformer<BattleWinGA>(BattleWinPerformer);
        ActionSystem.AttachPerformer<BattleLoseGA>(BattleLosePerformer);
    }

    public void ResetForBattle()
    {
        _battleEnded = false;
    }

    void OnDisable()
    {
        ActionSystem.UnsubscribeReaction<KillEnemyGA>(OnKillEnemyPost, ReactionTiming.POST);
        ActionSystem.UnsubscribeReaction<DealDamageGA>(OnDealDamagePost, ReactionTiming.POST);
        ActionSystem.DetachPerformer<BattleWinGA>();
        ActionSystem.DetachPerformer<BattleLoseGA>();
    }

    void Start()
    {
        FindPanelsIfNull();
    }

    private void FindPanelsIfNull()
    {
        if (winPanel == null)
        {
            var allObjects = FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var obj in allObjects)
            {
                if (obj.name == "WinPanel")
                {
                    winPanel = obj;
                    Debug.Log("[BattleResultSystem] 自动找到了 WinPanel");
                    break;
                }
            }
        }
    }

    private void OnKillEnemyPost(KillEnemyGA killEnemyGA)
    {
        var enemies = EnemySystem.Instance?.Enemies;
        Debug.Log($"[BattleResult] OnKillEnemyPost, 剩余敌人: {enemies?.Count ?? -1}, _battleEnded={_battleEnded}");
        if (_battleEnded) return;
        if (enemies == null || enemies.Count > 0) return;
        _battleEnded = true;
        ActionSystem.Instance.AddReaction(new BattleWinGA());
    }

    private void OnDealDamagePost(DealDamageGA dealDamageGA)
    {
        if (_battleEnded) return;
        var hero = HeroSystem.Instance?.HeroView;
        if (hero == null || hero.CurrentHealth > 0) return;

        if (BlessingSystem.Instance != null && BlessingSystem.Instance.TryResurrect(hero))
            return;

        _battleEnded = true;
        ActionSystem.Instance.AddReaction(new BattleLoseGA());
    }

    private IEnumerator BattleWinPerformer(BattleWinGA battleWinGA)
    {
        Debug.Log("[BattleResultSystem] 战斗胜利！所有敌人已消灭。");

        Debug.Log("[BattleResultSystem] A: 准备设置 IsShowingReward");
        if (_interactions != null) _interactions.IsShowingReward = true;
        Debug.Log("[BattleResultSystem] B: IsShowingReward 已设置");

        var gm = GameManager.Instance;
        Debug.Log($"[BattleResultSystem] C: gm={(gm != null ? "OK" : "null")}");

        bool isBoss = gm != null
            && gm.IsBossEncounter
            && gm.PendingEncounter?.cellType == MapCellType.WorldMap_Boss;
        Debug.Log($"[BattleResultSystem] D: isBoss={isBoss}");

        if (isBoss)
        {
            Debug.Log("[BattleResultSystem] BOSS 格战斗胜利 → 游戏全局胜利");
            GameManager.Instance.ShowGameWin();
            yield return null;
            yield break;
        }

        // 普通战斗胜利：显示 WinPanel + 奖励
        Debug.Log("[BattleResultSystem] E: 准备查找 WinPanel");
        if (winPanel == null) FindPanelsIfNull();
        Debug.Log($"[BattleResultSystem] F: winPanel={(winPanel != null ? "OK" : "null")}");

        if (winPanel != null)
        {
            winPanel.SetActive(true);
            Debug.Log("[BattleResultSystem] G: WinPanel 已激活");
            var controller = winPanel.GetComponent<WinPanelController>();
            if (controller != null)
            {
                Debug.Log("[BattleResultSystem] H: 准备生成奖励");
                BattleReward reward = _rewardSystem != null ? _rewardSystem.GenerateReward() : new BattleReward();
                Debug.Log($"[BattleResultSystem] I: 奖励已生成, gold={reward.GoldAmount}");
                controller.Initialize(reward);
                Debug.Log("[BattleResultSystem] J: Initialize 完成");
            }
        }
        Debug.Log("[BattleResultSystem] K: 结束");
        yield return null;
    }

    private IEnumerator BattleLosePerformer(BattleLoseGA battleLoseGA)
    {
        Debug.Log("[BattleResultSystem] 战斗失败！英雄已死亡。");
        if (_interactions != null) _interactions.IsShowingReward = true;
        GameManager.Instance.ShowGameLose();
        yield return null;
    }
}
