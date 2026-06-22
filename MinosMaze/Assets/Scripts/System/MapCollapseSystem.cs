using System.Collections;
using UnityEngine;

// 地图塌陷系统：当玩家向中心移动时，最外圈六角格以动画形式塌陷。
// 始终保留玩家所在环外一圈的可见范围，超出部分播放塌陷动画后禁用。
// 被动调用模式：由 WorldMapMovementSystem 在玩家移动协程中 yield return 调用。
public static class MapCollapseSystem
{
    private const float collapseDuration = 0.5f;
    private const float fallDistance = 3f;

    private static int currentMaxRing;
    private static bool inited;

    public static void Reset()
    {
        inited = false;
        currentMaxRing = 0;
    }

    // 玩家移动后调用，传入新坐标（协程，由 caller 的 MoveToCell 中 yield return 驱动）
    public static IEnumerator OnPlayerMoved(int x, int z)
    {
        EnsureInit();

        int playerRing = HexGrid.HexDistance(0, 0, x, z);

        while (playerRing < currentMaxRing - 1)
        {
            yield return CollapseRing(currentMaxRing);
            currentMaxRing--;
        }

        // 检查玩家周围格子是否全部坍塌
        if (CheckPlayerSurrounded(x, z))
        {
            Debug.Log("[MapCollapseSystem] 玩家周围格子全部坍塌，游戏失败");
            GameManager.Instance.OnGameOver();
        }
    }

    // 检查玩家周围所有合法邻居是否全部已清除（坍塌）
    public static bool CheckPlayerSurrounded(int x, int z)
    {
        GameManager gm = GameManager.Instance;
        if (gm == null) return false;

        // 六边形轴向坐标下的 6 个邻居偏移
        (int dx, int dz)[] offsets = { (1, 0), (-1, 0), (0, 1), (-1, 1), (1, -1), (0, -1) };
        int neighborCount = 0;
        int clearedCount = 0;

        foreach (var (dx, dz) in offsets)
        {
            int nx = x + dx;
            int nz = z + dz;
            if (!HexGrid.ContainsCell(nx, nz)) continue;
            neighborCount++;
            Vector2Int coord = new(nx, nz);
            if (gm.WorldMapState.clearedCells.Contains(coord))
                clearedCount++;
        }

        // 所有地图内的邻居都在已清除列表中则判定为被围困
        return neighborCount > 0 && neighborCount == clearedCount;
    }

    // 首次调用时从活跃格子计算地图当前最大环数
    static void EnsureInit()
    {
        if (inited) return;
        currentMaxRing = 0;
        foreach (var cell in HexGrid.AllCells)
        {
            if (!cell.gameObject.activeSelf) continue;
            int dist = HexGrid.HexDistance(0, 0, cell.hexCoordX, cell.hexCoordZ);
            if (dist > currentMaxRing) currentMaxRing = dist;
        }
        inited = true;
    }

    // 将指定环上的所有六角格播放塌陷动画并持久化到 clearedCells
    static IEnumerator CollapseRing(int ring)
    {
        GameManager gm = GameManager.Instance;
        foreach (var cell in HexGrid.AllCells)
        {
            if (HexGrid.HexDistance(0, 0, cell.hexCoordX, cell.hexCoordZ) == ring)
            {
                cell.PlayCollapseAnimation(collapseDuration, fallDistance);
                if (gm != null)
                {
                    Vector2Int coord = new(cell.hexCoordX, cell.hexCoordZ);
                    if (!gm.WorldMapState.clearedCells.Contains(coord))
                        gm.WorldMapState.clearedCells.Add(coord);
                }
            }
        }
        yield return new WaitForSeconds(collapseDuration);
    }
}
