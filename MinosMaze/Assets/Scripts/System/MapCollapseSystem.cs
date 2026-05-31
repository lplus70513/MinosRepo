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

    // 玩家移动后调用，传入新坐标（协程，由 caller 的 MoveToCell 中 yield return 驱动）
    public static IEnumerator OnPlayerMoved(int x, int z)
    {
        EnsureInit();

        int playerRing = HexGrid.HexDistance(0, 0, x, z);

        while (playerRing < currentMaxRing - 1)
        {
            Debug.Log($"[MapCollapse] 塌陷环 {currentMaxRing}");
            yield return CollapseRing(currentMaxRing);
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

    // 将指定环上的所有六角格播放塌陷动画，返回 IEnumerator 供协程等待
    static IEnumerator CollapseRing(int ring)
    {
        int count = 0;
        foreach (var cell in HexGrid.AllCells)
        {
            if (HexGrid.HexDistance(0, 0, cell.hexCoordX, cell.hexCoordZ) == ring)
            {
                cell.PlayCollapseAnimation(collapseDuration, fallDistance);
                count++;
            }
        }
        yield return new WaitForSeconds(collapseDuration);
        Debug.Log($"[MapCollapse] 环 {ring} 塌陷完成，共 {count} 个格子");
    }
}
