using System.Collections;
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
    [Header("场景跳转配置（暂留空）")]
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

        // 禁用战斗地图的移动系统，避免双高亮冲突
        if (PlayerMovementSystem.Instance != null)
            PlayerMovementSystem.Instance.enabled = false;

        // 恢复存档或使用初始值
        GameManager gm = GameManager.Instance;
        if (gm != null && gm.WorldMapState != null && gm.WorldMapState.remainingMovePoints > 0)
        {
            MovePoints = gm.WorldMapState.remainingMovePoints;
        }
        else
        {
            MovePoints = wmg.InitialMovePoints;
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
        if (!Interactions.Instance.PlayerCanInteract()) return false;
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

        // 离开当前格时标记为已走过
        GameManager gm = GameManager.Instance;
        if (gm != null)
        {
            Vector2Int currentCoord = new(player.HexCoordX, player.HexCoordZ);
            if (!gm.WorldMapState.clearedCells.Contains(currentCoord))
                gm.WorldMapState.clearedCells.Add(currentCoord);
        }

        MovePoints--;
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

    // 将当前状态保存到 GameManager（用于跨场景恢复）
    private void SaveStateToGameManager()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null) return;

        WorldMapPlayerView player = WorldMapPlayerSystem.Instance.PlayerView;
        if (player != null)
            gm.SaveWorldMapState(player.HexCoordX, player.HexCoordZ, MovePoints, player.CurrentHealth, player.MaxHealth);
    }

    // 外部补给移动点
    public void AddMovePoints(int amount)
    {
        MovePoints += amount;
    }
}
