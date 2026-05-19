using System.Collections.Generic;
using UnityEngine;

// 六边形地图移动相关工具方法（静态类）。
// 从 HexGrid 拆分出来，负责可行走邻居查询、格子占用判断、移动范围高亮。
public static class HexMove
{
    // 检查指定格子是否被英雄或敌人占据（exclude 参数可指定忽略某个单位，用于自身寻路）
    public static bool IsCellOccupied(int x, int z, CombatantView exclude = null)
    {
        HeroView hero = HeroSystem.Instance.HeroView;
        if (hero != null && hero != exclude && hero.HexCoordX == x && hero.HexCoordZ == z)
            return true;

        foreach (var enemy in EnemySystem.Instance.Enemies)
        {
            if (enemy != null && enemy != exclude && enemy.HexCoordX == x && enemy.HexCoordZ == z)
                return true;
        }
        return false;
    }

    // 获取指定坐标的 6 个可行走邻居（排除被占用的格子，exclude 参数可指定忽略某个单位）
    public static List<(int x, int z)> GetWalkableNeighbors(int x, int z, CombatantView exclude = null)
    {
        List<(int x, int z)> neighbors = new();
        // 六边形轴向坐标下的 6 个邻居偏移
        (int dx, int dz)[] offsets = { (1, 0), (-1, 0), (0, 1), (-1, 1), (1, -1), (0, -1) };

        foreach (var (dx, dz) in offsets)
        {
            int nx = x + dx;
            int nz = z + dz;
            if (HexGrid.ContainsCell(nx, nz) && !IsCellOccupied(nx, nz, exclude))
                neighbors.Add((nx, nz));
        }
        return neighbors;
    }

    // 高亮显示中心格 range 步内的可行走格子（排除已被占据的格子，用于显示移动范围）
    public static void HighlightMoveCellsInRange(int centerX, int centerZ, int range)
    {
        var coords = HexGrid.GetCoordsInRange(centerX, centerZ, range);
        foreach (var (x, z) in coords)
        {
            if (IsCellOccupied(x, z)) continue;
            HexCell cell = HexGrid.GetCell(x, z);
            if (cell != null) cell.SetMoveHighlight(true);
        }
    }

    // 清除所有格子的移动高亮
    public static void ClearMoveHighlights()
    {
        foreach (var cell in HexGrid.AllCells)
        {
            cell.SetMoveHighlight(false);
        }
    }
}
