using System.Collections.Generic;
using UnityEngine;

// 六边形地图上的 A* 寻路算法（静态工具类）。
// 在 HexGrid 生成的六角格地图上查找从起点到终点的最短路径，
// 自动避开已被英雄/敌人占据的格子。
public static class HexPathfinder
{
    // 查找从 (startX, startZ) 到 (goalX, goalZ) 的最短路径。
    // exclude: 寻路时忽略此单位（通常为寻路者自身），避免把自己当作障碍物
    // extraExcluded: 额外排除的格子（如路径上其他单位占用的格子），但目标格除外
    // 返回路径坐标列表（从起点到终点），无路径时返回 null
    public static List<(int x, int z)> FindPath(int startX, int startZ, int goalX, int goalZ,
        CombatantView exclude = null, HashSet<(int, int)> extraExcluded = null)
    {
        // 起点即终点，直接返回单步路径
        if (startX == goalX && startZ == goalZ)
            return new List<(int, int)> { (startX, startZ) };

        // A* 核心数据结构
        var openSet = new List<PathNode>();              // 待探索节点列表（每次取 F 最小者）
        var closedSet = new HashSet<(int, int)>();       // 已探索节点集合
        var nodeDict = new Dictionary<(int, int), PathNode>(); // 坐标 → PathNode 快速查找

        // 初始化起点节点：G=0（已走步数），H=到终点的六边形距离（启发函数）
        PathNode startNode = new(startX, startZ, 0, HexGrid.HexDistance(startX, startZ, goalX, goalZ), null);
        openSet.Add(startNode);
        nodeDict[(startX, startZ)] = startNode;

        while (openSet.Count > 0)
        {
            // 按 F = G + H 排序，取 F 最小的节点（贪心 + 启发式）
            openSet.Sort((a, b) => a.F.CompareTo(b.F));
            PathNode current = openSet[0];
            openSet.RemoveAt(0);

            // 到达终点，回溯重建路径
            if (current.X == goalX && current.Z == goalZ)
                return ReconstructPath(current);

            closedSet.Add((current.X, current.Z));

            var neighbors = HexMove.GetWalkableNeighbors(current.X, current.Z, exclude);

            // 目标格被英雄占用时 GetWalkableNeighbors 会将其排除，手动补回
            if (HexGrid.HexDistance(current.X, current.Z, goalX, goalZ) == 1)
            {
                bool goalInNeighbors = false;
                foreach (var (nx, nz) in neighbors)
                {
                    if (nx == goalX && nz == goalZ) { goalInNeighbors = true; break; }
                }
                if (!goalInNeighbors && HexGrid.ContainsCell(goalX, goalZ))
                    neighbors.Add((goalX, goalZ));
            }

            foreach (var (nx, nz) in neighbors)
            {
                if (closedSet.Contains((nx, nz))) continue;
                if (extraExcluded != null && extraExcluded.Contains((nx, nz)) && (nx != goalX || nz != goalZ)) continue;

                // 从当前节点走一步的代价
                int g = current.G + 1;
                if (nodeDict.TryGetValue((nx, nz), out PathNode existing))
                {
                    // 发现更优路径，更新该节点的 G 值和父节点
                    if (g < existing.G)
                    {
                        existing.G = g;
                        existing.Parent = current;
                    }
                }
                else
                {
                    // 新节点，计算 H 值并加入待探索集合
                    PathNode neighbor = new(nx, nz, g, HexGrid.HexDistance(nx, nz, goalX, goalZ), current);
                    openSet.Add(neighbor);
                    nodeDict[(nx, nz)] = neighbor;
                }
            }
        }

        // openSet 为空：所有可达节点已探索完毕，未找到路径
        return null;
    }

    // 从目标节点沿 Parent 链回溯到起点，得到完整路径（起点→终点顺序）
    private static List<(int x, int z)> ReconstructPath(PathNode node)
    {
        List<(int x, int z)> path = new();
        while (node != null)
        {
            path.Add((node.X, node.Z));
            node = node.Parent;
        }
        path.Reverse(); // 反转后为起点→终点顺序
        return path;
    }

    // A* 节点内部类，记录坐标、代价和父节点引用
    private class PathNode
    {
        public int X, Z;           // 轴向坐标
        public int G;              // 从起点到当前节点的实际步数
        public int H;              // 当前节点到终点的启发式估算距离（六边形距离）
        public int F => G + H;     // 总代价，A* 依此值择优扩展
        public PathNode Parent;    // 父节点，用于回溯重建路径

        public PathNode(int x, int z, int g, int h, PathNode parent)
        {
            X = x;
            Z = z;
            G = g;
            H = h;
            Parent = parent;
        }
    }
}
