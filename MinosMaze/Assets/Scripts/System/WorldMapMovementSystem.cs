using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

// 格子类型 → 跳转场景映射
[System.Serializable]
public struct SceneMapping
{
    public MapCellType cellType;
    public string sceneName;
}

// 大地图移动系统：移动点数管理、格子间移动、场景跳转、GameOver 判定
public class WorldMapMovementSystem : Singleton<WorldMapMovementSystem>
{
    [Header("场景跳转配置")]
    [SerializeField] private SceneMapping[] sceneMap;

    [Header("移动配置")]
    [SerializeField] private float moveDuration = 0.2f;

    // 当前剩余移动点数
    public int MovePoints { get; private set; }

    private bool isMoving;
    private bool highlightsVisible;

    void Start()
    {
        // 检查是否在大地图场景中（存在 WorldMapGrid）
        WorldMapGrid wmg = FindObjectOfType<WorldMapGrid>();
        if (wmg == null)
        {
            // 不在大地图场景，禁用自身
            enabled = false;
            return;
        }

        // 从 WorldMapState.stringCount 恢复线数量
        GameManager gm = GameManager.Instance;
        if (gm != null && gm.WorldMapState != null)
        {
            MovePoints = gm.WorldMapState.stringCount;
        }

        // 为大地图牌库检视初始化 CardSystem（读取持久化牌组）
        if (gm != null && gm.WorldMapState != null && gm.WorldMapState.currentDeck != null && gm.WorldMapState.currentDeck.Count > 0)
        {
            CardSystem.Instance.SetUp(gm.WorldMapState.currentDeck);
        }
    }

    void Update()
    {
        bool shouldShow = ShouldShowMoveHighlights();
        if (shouldShow != highlightsVisible)
        {
            if (shouldShow)
                ShowAdjacentHighlights();
            else
                ClearAllHighlights();
            highlightsVisible = shouldShow;
        }
    }

    private bool ShouldShowMoveHighlights()
    {
        if (isMoving) return false;
        if (MovePoints <= 0) return false;
        if (Interactions.Instance == null) return true;
        if (Interactions.Instance.IsViewingDeck) return false;
        if (Interactions.Instance.PlayerIsDragging) return false;
        if (Interactions.Instance.PlayerIsTargeting) return false;
        return true;
    }

    // 高亮当前玩家周围距离=1的格子（自实现，不依赖 HexMove，大地图无 EnemySystem）
    private void ShowAdjacentHighlights()
    {
        WorldMapPlayerView player = WorldMapPlayerSystem.Instance.PlayerView;
        if (player == null) return;

        var coords = HexGrid.GetCoordsInRange(player.HexCoordX, player.HexCoordZ, 1);
        foreach (var (x, z) in coords)
        {
            HexCell cell = HexGrid.GetCell(x, z);
            if (cell != null) cell.SetMoveHighlight(true);
        }
    }

    private void ClearAllHighlights()
    {
        foreach (var cell in HexGrid.AllCells)
        {
            cell.SetMoveHighlight(false);
        }
    }

    // HexRayCast 点击大地图格时调用
    public void HandleClick(int hexX, int hexZ)
    {
        if (isMoving) return;
        if (MovePoints <= 0) return;

        WorldMapPlayerView player = WorldMapPlayerSystem.Instance.PlayerView;
        if (player == null) return;

        int dist = HexGrid.HexDistance(player.HexCoordX, player.HexCoordZ, hexX, hexZ);
        if (dist != 1) return;

        HexCell targetCell = HexGrid.GetCell(hexX, hexZ);
        if (targetCell == null) return;
        if (!targetCell.IsWorldMapCell) return;

        // 禁止走回已走过的格子
        Vector2Int targetCoord = new(hexX, hexZ);
        GameManager gm = GameManager.Instance;
        if (gm != null && gm.WorldMapState.clearedCells.Contains(targetCoord))
            return;

        StartCoroutine(MoveToCell(player, hexX, hexZ, targetCell.cellType));
    }

    private IEnumerator MoveToCell(WorldMapPlayerView player, int hexX, int hexZ, MapCellType cellType)
    {
        isMoving = true;
        ClearAllHighlights();
        highlightsVisible = false;

        // 离开当前格时标记为已走过并播放塌陷动画
        GameManager gm = GameManager.Instance;
        if (gm != null)
        {
            Vector2Int currentCoord = new(player.HexCoordX, player.HexCoordZ);
            if (!gm.WorldMapState.clearedCells.Contains(currentCoord))
                gm.WorldMapState.clearedCells.Add(currentCoord);

            HexCell leavingCell = HexGrid.GetCell(player.HexCoordX, player.HexCoordZ);
            if (leavingCell != null)
                leavingCell.PlayCollapseAnimation(0.5f, 3f);
        }

        MovePoints--;
        if (gm != null) gm.WorldMapState.stringCount = MovePoints;
        Vector3 targetPos = HexGrid.GetStandingPoint(hexX, hexZ);
        Tween tween = player.transform.DOMove(targetPos, moveDuration);
        yield return tween.WaitForCompletion();

        player.HexCoordX = hexX;
        player.HexCoordZ = hexZ;
        yield return MapCollapseSystem.OnPlayerMoved(hexX, hexZ);

        // 根据抵达格类型处理场景跳转
        string sceneName = GetSceneForCellType(cellType);
        if (!string.IsNullOrEmpty(sceneName))
        {
            // 保存状态后跳转
            SaveStateToGameManager();
            SetPendingEncounter(cellType);
            if (GameManager.Instance != null)
                GameManager.Instance.EnterEncounter(sceneName);
            yield break;
        }

        // 检查 GameOver：移动点耗尽且不在 BOSS 格
        if (MovePoints <= 0 && cellType != MapCellType.WorldMap_Boss)
        {
            GameManager.Instance.OnGameOver();
            yield break;
        }

        isMoving = false;
    }

    // 根据格子类型查找对应场景名
    private string GetSceneForCellType(MapCellType cellType)
    {
        if (sceneMap == null) return null;
        foreach (var entry in sceneMap)
        {
            if (entry.cellType == cellType)
                return entry.sceneName;
        }
        return null;
    }

    // 根据格子类型设置待加载的遭遇标识到 GameManager
    private void SetPendingEncounter(MapCellType cellType)
    {
        GameManager gm = GameManager.Instance;
        if (gm == null) return;

        int floor = gm.WorldMapState?.floorLevel ?? 1;
        gm.PendingEncounter = new EncounterConfig { cellType = cellType, floorLevel = floor };
    }

    // 将当前状态保存到 GameManager（用于跨场景恢复）
    public void SaveStateToGameManager()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null) return;

        WorldMapPlayerView player = WorldMapPlayerSystem.Instance.PlayerView;
        if (player != null)
            gm.SaveWorldMapState(player.HexCoordX, player.HexCoordZ, MovePoints, player.CurrentHealth, player.MaxHealth);
    }

    // 外部补给线
    public void AddString(int amount)
    {
        MovePoints += amount;
        GameManager gm = GameManager.Instance;
        if (gm != null) gm.WorldMapState.stringCount = MovePoints;
    }
}
