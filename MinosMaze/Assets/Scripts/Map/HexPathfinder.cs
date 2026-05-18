using System.Collections.Generic;
using UnityEngine;

public static class HexPathfinder
{
    public static List<(int x, int z)> FindPath(int startX, int startZ, int goalX, int goalZ,
        CombatantView exclude = null, HashSet<(int, int)> extraExcluded = null)
    {
        if (startX == goalX && startZ == goalZ)
            return new List<(int, int)> { (startX, startZ) };

        var openSet = new List<PathNode>();
        var closedSet = new HashSet<(int, int)>();
        var nodeDict = new Dictionary<(int, int), PathNode>();

        PathNode startNode = new(startX, startZ, 0, HexGrid.HexDistance(startX, startZ, goalX, goalZ), null);
        openSet.Add(startNode);
        nodeDict[(startX, startZ)] = startNode;

        while (openSet.Count > 0)
        {
            openSet.Sort((a, b) => a.F.CompareTo(b.F));
            PathNode current = openSet[0];
            openSet.RemoveAt(0);

            if (current.X == goalX && current.Z == goalZ)
                return ReconstructPath(current);

            closedSet.Add((current.X, current.Z));

            var neighbors = HexGrid.GetWalkableNeighbors(current.X, current.Z, exclude);

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

                int g = current.G + 1;
                if (nodeDict.TryGetValue((nx, nz), out PathNode existing))
                {
                    if (g < existing.G)
                    {
                        existing.G = g;
                        existing.Parent = current;
                    }
                }
                else
                {
                    PathNode neighbor = new(nx, nz, g, HexGrid.HexDistance(nx, nz, goalX, goalZ), current);
                    openSet.Add(neighbor);
                    nodeDict[(nx, nz)] = neighbor;
                }
            }
        }

        return null;
    }

    private static List<(int x, int z)> ReconstructPath(PathNode node)
    {
        List<(int x, int z)> path = new();
        while (node != null)
        {
            path.Add((node.X, node.Z));
            node = node.Parent;
        }
        path.Reverse();
        return path;
    }

    private class PathNode
    {
        public int X, Z;
        public int G;
        public int H;
        public int F => G + H;
        public PathNode Parent;

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
