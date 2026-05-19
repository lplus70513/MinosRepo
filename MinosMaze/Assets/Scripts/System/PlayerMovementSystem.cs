using UnityEngine;

public class PlayerMovementSystem : Singleton<PlayerMovementSystem>
{
    public int RemainingMovementPoints { get; private set; } = 1;

    private bool highlightsVisible;

    void OnEnable()
    {
        ActionSystem.SubscribeReaction<EnemyTurnGA>(OnEnemyTurnPost, ReactionTiming.POST);
    }

    void OnDisable()
    {
        ActionSystem.UnsubscribeReaction<EnemyTurnGA>(OnEnemyTurnPost, ReactionTiming.POST);
    }

    void Start()
    {
        RemainingMovementPoints = 1;
    }

    void Update()
    {
        bool shouldShow = ShouldShowMoveHighlights();
        if (shouldShow != highlightsVisible)
        {
            if (shouldShow)
            {
                HeroView hero = HeroSystem.Instance.HeroView;
                HexMove.HighlightMoveCellsInRange(hero.HexCoordX, hero.HexCoordZ, RemainingMovementPoints);
            }
            else
            {
                HexMove.ClearMoveHighlights();
            }
            highlightsVisible = shouldShow;
        }
    }

    private bool ShouldShowMoveHighlights()
    {
        if (!Interactions.Instance.PlayerCanInteract()) return false;
        if (Interactions.Instance.PlayerIsDragging) return false;
        if (Interactions.Instance.PlayerIsTargeting) return false;
        if (RemainingMovementPoints <= 0) return false;
        return true;
    }

    public void HandleClick(int hexX, int hexZ)
    {
        if (!Interactions.Instance.PlayerCanInteract()) return;
        if (Interactions.Instance.PlayerIsDragging) return;
        if (Interactions.Instance.PlayerIsTargeting) return;
        if (RemainingMovementPoints <= 0) return;

        HeroView hero = HeroSystem.Instance.HeroView;
        if (hero == null) return;

        int dist = HexGrid.HexDistance(hero.HexCoordX, hero.HexCoordZ, hexX, hexZ);
        if (dist != 1) return;

        if (HexMove.IsCellOccupied(hexX, hexZ)) return;

        RemainingMovementPoints--;
        MoveGA moveGA = new(hero, hexX, hexZ);
        ActionSystem.Instance.Perform(moveGA);
    }

    private void OnEnemyTurnPost(EnemyTurnGA ga)
    {
        RemainingMovementPoints = 1;
    }
}
