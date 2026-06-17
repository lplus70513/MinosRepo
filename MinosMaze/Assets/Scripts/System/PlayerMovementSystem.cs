using System.Collections;
using UnityEngine;

public class PlayerMovementSystem : Singleton<PlayerMovementSystem>
{
    public int RemainingMovementPoints { get; private set; } = 1;

    private bool highlightsVisible;
    private bool showingEnemyRange;
    private EnemyView lastShownEnemy;

    void OnEnable()
    {
        ActionSystem.SubscribeReaction<EnemyTurnGA>(OnEnemyTurnPost, ReactionTiming.POST);
        ActionSystem.AttachPerformer<AddMovePointsGA>(AddMovePointsPerformer);
    }

    void OnDisable()
    {
        ActionSystem.UnsubscribeReaction<EnemyTurnGA>(OnEnemyTurnPost, ReactionTiming.POST);
        ActionSystem.DetachPerformer<AddMovePointsGA>();
    }

    void Start()
    {
        RemainingMovementPoints = 1;
    }

    public void ResetForBattle()
    {
        RemainingMovementPoints = 1;
        if (!enabled) enabled = true;
    }

    private bool IsHeroRootedOrStunned()
    {
        var hero = HeroSystem.Instance?.HeroView;
        if (hero == null) return false;
        return hero.HasStatusEffect(StatusEffectType.ROOT)
            || hero.HasStatusEffect(StatusEffectType.STUN);
    }

    void Update()
    {
        if (HeroSystem.Instance?.HeroView == null) return;
        UpdateHoveredEnemy();

        if (ShouldShowEnemyRange())
        {
            ShowEnemyRange();
            return;
        }

        if (showingEnemyRange)
        {
            ClearEnemyRange();
            showingEnemyRange = false;
            highlightsVisible = false;
        }

        if (IsHeroRootedOrStunned())
        {
            if (highlightsVisible)
            {
                HexMove.ClearMoveHighlights();
                highlightsVisible = false;
            }
            return;
        }

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

    private Camera GetCamera3D()
    {
        var camObj = GameObject.FindGameObjectWithTag("3D Camera");
        if (camObj != null) return camObj.GetComponent<Camera>();
        return null;
    }

    private void UpdateHoveredEnemy()
    {
        Camera cam = GetCamera3D();
        if (cam == null)
        {
            CombatantView.HoveredEnemy = null;
            return;
        }

        var enemies = EnemySystem.Instance?.Enemies;
        if (enemies == null)
        {
            CombatantView.HoveredEnemy = null;
            return;
        }
        Vector2 mouseScreen = Input.mousePosition;
        EnemyView best = null;
        float bestDist = 100f;

        foreach (var enemy in enemies)
        {
            if (enemy == null) continue;
            Vector3 screenPos = cam.WorldToScreenPoint(enemy.transform.position);
            Vector2 enemyScreen = new(screenPos.x, screenPos.y);
            float dist = Vector2.Distance(mouseScreen, enemyScreen);
            if (dist < bestDist)
            {
                bestDist = dist;
                best = enemy;
            }
        }

        CombatantView.HoveredEnemy = bestDist < 100f ? best : null;
    }

    private bool ShouldShowEnemyRange()
    {
        var hovered = CombatantView.HoveredEnemy;
        if (hovered == null) return false;
        if (Interactions.Instance.PlayerIsDragging) return false;
        if (Interactions.Instance.PlayerIsTargeting) return false;
        return true;
    }

    private void ShowEnemyRange()
    {
        EnemyView enemy = CombatantView.HoveredEnemy;
        if (enemy == lastShownEnemy) return;

        HexMove.ClearMoveHighlights();
        HexGrid.ClearAllHighlights();
        highlightsVisible = false;

        int maxMove = enemy.GetMaxMoveRange();
        var reachable = HexMove.GetReachableCells(enemy.HexCoordX, enemy.HexCoordZ, maxMove, enemy);

        if (maxMove > 0)
        {
            foreach (var (x, z) in reachable)
            {
                HexCell cell = HexGrid.GetCell(x, z);
                if (cell != null) cell.SetMoveHighlight(true);
            }
        }

        var attackCells = HexMove.GetAttackRangeFromCells(reachable, 1);
        foreach (var (x, z) in attackCells)
        {
            HexCell cell = HexGrid.GetCell(x, z);
            if (cell != null) cell.SetHighlight(true);
        }

        showingEnemyRange = true;
        lastShownEnemy = enemy;
    }

    private void ClearEnemyRange()
    {
        HexMove.ClearMoveHighlights();
        HexGrid.ClearAllHighlights();
        lastShownEnemy = null;
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
        if (IsHeroRootedOrStunned()) return;

        HeroView hero = HeroSystem.Instance.HeroView;
        if (hero == null) return;

        int dist = HexGrid.HexDistance(hero.HexCoordX, hero.HexCoordZ, hexX, hexZ);
        if (dist != 1) return;

        HexCell targetCell = HexGrid.GetCell(hexX, hexZ);
        if (targetCell == null || !targetCell.IsWalkable) return;

        if (HexMove.IsCellOccupied(hexX, hexZ)) return;

        RemainingMovementPoints--;
        MoveGA moveGA = new(hero, hexX, hexZ);
        ActionSystem.Instance.Perform(moveGA);
    }

    private IEnumerator AddMovePointsPerformer(AddMovePointsGA ga)
    {
        RemainingMovementPoints += ga.Amount;
        yield return null;
    }

    public void AddMovementPoints(int amount)
    {
        RemainingMovementPoints += amount;
    }

    private void OnEnemyTurnPost(EnemyTurnGA ga)
    {
        RemainingMovementPoints = 1;
    }
}
