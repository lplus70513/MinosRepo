using UnityEngine;

// 地图塌陷系统：当玩家向中心移动时，最外圈六角格自动关闭。
// 始终保留玩家所在环外一圈的可见范围，超出部分 SetActive(false)。
// 被动调用模式：由 WorldMapMovementSystem 在玩家移动后调用 OnPlayerMoved。
public static class MapCollapseSystem
{
    private static int currentMaxRing;
    private static bool inited;

    // 玩家移动后调用，传入新坐标
    public static void OnPlayerMoved(int x, int z)
    {
        EnsureInit();

        int playerRing = HexGrid.HexDistance(0, 0, x, z);

        Debug.Log($"[MapCollapse] 玩家移动至 ({x},{z}) | playerRing={playerRing} | currentMaxRing={currentMaxRing} | 条件(playerRing < maxRing-1)={playerRing < currentMaxRing - 1}");

        while (playerRing < currentMaxRing - 1)
        {
            Debug.Log($"[MapCollapse] 塌陷环 {currentMaxRing}");
            CollapseRing(currentMaxRing);
            currentMaxRing--;
        }
    }

    // 首次调用时计算地图初始最大环数（延迟到 HexGrid.Awake 之后）
    static void EnsureInit()
    {
        if (inited) return;
        currentMaxRing = 0;
        foreach (var cell in HexGrid.AllCells)
        {
            int dist = HexGrid.HexDistance(0, 0, cell.hexCoordX, cell.hexCoordZ);
            if (dist > currentMaxRing) currentMaxRing = dist;
        }
        inited = true;
        Debug.Log($"[MapCollapse] EnsureInit: currentMaxRing={currentMaxRing}");
    }

    // 将指定环上的所有六角格 SetActive(false)
    static void CollapseRing(int ring)
    {
        int count = 0;
        foreach (var cell in HexGrid.AllCells)
        {
            if (HexGrid.HexDistance(0, 0, cell.hexCoordX, cell.hexCoordZ) == ring)
            {
                cell.gameObject.SetActive(false);
                count++;
            }
        }
        Debug.Log($"[MapCollapse] 环 {ring} 塌陷完成，共关闭 {count} 个格子");
    }
}
