using UnityEngine;

public class RunStatistics : Singleton<RunStatistics>
{
    public int EnemiesKilled;
    public int TotalDamageDealt;
    public int TotalDamageTaken;
    public int TurnsTaken;
    public float StartTime;
    public bool IsWin;

    public int MaxFloorReached;
    public int GoldCollected;
    public int CardsObtained;
    public int BlessingsObtained;

    public string PlayerName => GameManager.Instance?.WorldMapState?.playerName ?? "";

    public float ElapsedTime => Time.time - StartTime;

    private bool _isActive;

    protected override void Awake()
    {
        base.Awake();
        DontDestroyOnLoad(gameObject);
    }

    void OnEnable()
    {
        ActionSystem.SubscribeReaction<EnemyTurnGA>(OnEnemyTurnPost, ReactionTiming.POST);
        ActionSystem.SubscribeReaction<KillEnemyGA>(OnKillEnemyPost, ReactionTiming.POST);
        ActionSystem.SubscribeReaction<DealDamageGA>(OnDealDamagePost, ReactionTiming.POST);
    }

    void OnDisable()
    {
        ActionSystem.UnsubscribeReaction<EnemyTurnGA>(OnEnemyTurnPost, ReactionTiming.POST);
        ActionSystem.UnsubscribeReaction<KillEnemyGA>(OnKillEnemyPost, ReactionTiming.POST);
        ActionSystem.UnsubscribeReaction<DealDamageGA>(OnDealDamagePost, ReactionTiming.POST);
    }

    public void StartTracking()
    {
        ResetForNewRun();
        _isActive = true;
        StartTime = Time.time;
    }

    public void ResetForNewRun()
    {
        EnemiesKilled = 0;
        TotalDamageDealt = 0;
        TotalDamageTaken = 0;
        TurnsTaken = 0;
        StartTime = 0f;
        IsWin = false;
        MaxFloorReached = 0;
        GoldCollected = 0;
        CardsObtained = 0;
        BlessingsObtained = 0;
        _isActive = false;
    }

    public void Finalize(bool isWin)
    {
        if (!_isActive) return;
        _isActive = false;
        IsWin = isWin;

        var gm = GameManager.Instance;
        if (gm != null && gm.WorldMapState != null)
        {
            MaxFloorReached = gm.WorldMapState.floorLevel;
            GoldCollected = gm.WorldMapState.gold;
            BlessingsObtained = gm.WorldMapState.activeBlessings?.Count ?? 0;
            CardsObtained = gm.WorldMapState.currentDeck?.Count ?? 0;
        }
    }

    private void OnEnemyTurnPost(EnemyTurnGA ga)
    {
        if (!_isActive) return;
        TurnsTaken++;
    }

    private void OnKillEnemyPost(KillEnemyGA ga)
    {
        if (!_isActive) return;
        EnemiesKilled++;
    }

    private void OnDealDamagePost(DealDamageGA ga)
    {
        if (!_isActive) return;
        if (ga.Caster is HeroView)
            TotalDamageDealt += ga.Amount;
        foreach (var target in ga.Targets)
        {
            if (target is HeroView)
                TotalDamageTaken += ga.Amount;
        }
    }

    public int CalculateScore()
    {
        float score = MaxFloorReached * 100
                    + EnemiesKilled * 50
                    + TotalDamageDealt * 2
                    + GoldCollected * 10
                    + CardsObtained * 20
                    + BlessingsObtained * 30
                    + (IsWin ? 500 : 0)
                    - TotalDamageTaken * 5
                    - TurnsTaken * 2;
        return Mathf.Max(0, Mathf.RoundToInt(score));
    }
}
