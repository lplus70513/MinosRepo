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
                    string path = GetGameObjectPath(obj);
                    var components = obj.GetComponents<Component>();
                    string componentNames = "";
                    for (int i = 0; i < components.Length; i++)
                    {
                        if (i > 0) componentNames += ", ";
                        componentNames += components[i].GetType().Name;
                    }
                    Debug.Log($"[BattleResultSystem] 自动找到了 WinPanel: 路径={path}, 组件=[{componentNames}], 实例ID={obj.GetInstanceID()}");
                    break;
                }
            }
        }
    }

    private static string GetGameObjectPath(GameObject obj)
    {
        string path = obj.name;
        Transform parent = obj.transform.parent;
        while (parent != null)
        {
            path = parent.name + "/" + path;
            parent = parent.parent;
        }
        return path;
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

        Interactions.Instance.IsShowingReward = true;

        var gm = GameManager.Instance;
        bool isBoss = gm != null
            && gm.PendingEncounter?.cellType == MapCellType.WorldMap_Boss;
        Debug.Log($"[BattleResultSystem] isBoss判断: gm={(gm != null ? "OK" : "NULL")}, IsBossEncounter={gm?.IsBossEncounter}, PendingEncounter={(gm?.PendingEncounter != null ? $"cellType={gm.PendingEncounter.cellType}" : "null")}, => isBoss={isBoss}");
        if (isBoss)
        {
            Debug.Log("[BattleResultSystem] BOSS 格战斗胜利 → 游戏全局胜利");
            GameManager.Instance.ShowGameWin();
            yield return null;
            yield break;
        }

        // 普通战斗胜利：显示 WinPanel + 奖励
        if (winPanel == null) FindPanelsIfNull();
        if (winPanel != null)
        {
            Debug.Log($"[BattleResultSystem] 激活 winPanel: name={winPanel.name}, 路径={GetGameObjectPath(winPanel)}, 实例ID={winPanel.GetInstanceID()}");
            winPanel.SetActive(true);
            var controller = winPanel.GetComponent<WinPanelController>();
            if (controller != null)
            {
                BattleReward reward = RewardSystem.Instance.GenerateReward();
                controller.Initialize(reward);
            }
        }
        yield return null;
    }

    private IEnumerator BattleLosePerformer(BattleLoseGA battleLoseGA)
    {
        Debug.Log("[BattleResultSystem] 战斗失败！英雄已死亡。");
        Interactions.Instance.IsShowingReward = true;
        GameManager.Instance.ShowGameLose();
        yield return null;
    }
}
