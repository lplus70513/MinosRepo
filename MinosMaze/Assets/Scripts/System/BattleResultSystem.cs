using System.Collections;
using UnityEngine;

public class BattleResultSystem : Singleton<BattleResultSystem>
{
    [SerializeField] private GameObject winPanel;
    [SerializeField] private GameObject losePanel;

    private bool _battleEnded;

    void OnEnable()
    {
        _battleEnded = false;
        ActionSystem.SubscribeReaction<KillEnemyGA>(OnKillEnemyPost, ReactionTiming.POST);
        ActionSystem.SubscribeReaction<DealDamageGA>(OnDealDamagePost, ReactionTiming.POST);
        ActionSystem.AttachPerformer<BattleWinGA>(BattleWinPerformer);
        ActionSystem.AttachPerformer<BattleLoseGA>(BattleLosePerformer);
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
        if (losePanel == null)
        {
            var allObjects = FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var obj in allObjects)
            {
                if (obj.name == "LosePanel")
                {
                    losePanel = obj;
                    Debug.Log("[BattleResultSystem] 自动找到了 LosePanel");
                    break;
                }
            }
        }
    }

    private void OnKillEnemyPost(KillEnemyGA killEnemyGA)
    {
        if (_battleEnded) return;
        var enemies = EnemySystem.Instance?.Enemies;
        if (enemies == null || enemies.Count > 0) return;
        _battleEnded = true;
        ActionSystem.Instance.AddReaction(new BattleWinGA());
    }

    private void OnDealDamagePost(DealDamageGA dealDamageGA)
    {
        if (_battleEnded) return;
        var hero = HeroSystem.Instance?.HeroView;
        if (hero == null || hero.CurrentHealth > 0) return;
        _battleEnded = true;
        ActionSystem.Instance.AddReaction(new BattleLoseGA());
    }

    private IEnumerator BattleWinPerformer(BattleWinGA battleWinGA)
    {
        Debug.Log("[BattleResultSystem] 战斗胜利！所有敌人已消灭。");
        if (winPanel == null) FindPanelsIfNull();
        if (winPanel != null)
            winPanel.SetActive(true);
        yield return null;
    }

    private IEnumerator BattleLosePerformer(BattleLoseGA battleLoseGA)
    {
        Debug.Log("[BattleResultSystem] 战斗失败！英雄已死亡。");
        if (losePanel == null) FindPanelsIfNull();
        if (losePanel != null)
            losePanel.SetActive(true);
        yield return null;
    }
}
