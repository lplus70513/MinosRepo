using System.Collections.Generic;
using UnityEngine;

public class HexGrid : MonoBehaviour
{
    public int mapRadius = 2;

    public GameObject hexPrefab;

    private static Dictionary<(int x, int z), HexCell> cellDict = new();

    void Awake()
    {
        cellDict.Clear();
        CreateHexagonMap();
    }

    void CreateHexagonMap()
    {
        Vector3 center = Vector3.zero;

        for (int z = -mapRadius; z <= mapRadius; z++)
        {
            for (int x = -mapRadius - Mathf.Min(z,0) ; x <= mapRadius - Mathf.Max(z,0); x++)
            {
                Vector3 position = center + GetHexWorldPosition(x, z);
                CreateHexCell(position, x, z);
            }
        }
    }

    Vector3 GetHexWorldPosition(int x, int z)
    {
        Vector3 position;
        position.x = (2*x + z) * HexMetrics.innerRadius;
        position.y = 0f;
        position.z = -z * HexMetrics.outerRadius * 1.5f ;
        return position;
    }

    void CreateHexCell(Vector3 position, int x, int z)
    {
        GameObject hexCellObject = Instantiate(hexPrefab, position, Quaternion.Euler(0, 90, 0), transform);
        HexCell hexCell = hexCellObject.GetComponent<HexCell>();
        hexCell.SetCoord(x, z);
        cellDict[(x, z)] = hexCell;
    }

    public static HexCell GetCell(int x, int z)
    {
        cellDict.TryGetValue((x, z), out HexCell cell);
        return cell;
    }

    public static Vector3 GetStandingPoint(int x, int z)
    {
        if (cellDict.TryGetValue((x, z), out HexCell cell) && cell.standingPoint != null)
            return cell.standingPoint.position;
        Debug.LogWarning($"[HexGrid] 未找到六角格 ({x}, {z}) 或 standingPoint 为空");
        return Vector3.zero;
    }

    public static int HexDistance(int x1, int z1, int x2, int z2)
    {
        int q1 = x1 - (z1 - (z1 & 1)) / 2;
        int r1 = z1;
        int q2 = x2 - (z2 - (z2 & 1)) / 2;
        int r2 = z2;
        return (Mathf.Abs(q1 - q2) + Mathf.Abs(r1 - r2) + Mathf.Abs(q1 + r1 - q2 - r2)) / 2;
    }

    public static List<(int x, int z)> GetCoordsInRange(int centerX, int centerZ, int range)
    {
        List<(int x, int z)> result = new();
        for (int dz = -range; dz <= range; dz++)
        {
            for (int dx = -range - Mathf.Min(dz, 0); dx <= range - Mathf.Max(dz, 0); dx++)
            {
                int x = centerX + dx;
                int z = centerZ + dz;
                if (cellDict.ContainsKey((x, z)))
                    result.Add((x, z));
            }
        }
        return result;
    }

    public static List<(int x, int z)> GetWalkableNeighbors(int x, int z, CombatantView exclude = null)
    {
        List<(int x, int z)> neighbors = new();
        int parity = z & 1;
        (int dx, int dz)[] offsets = parity == 0
            ? new (int, int)[] { (1, 0), (0, -1), (-1, -1), (-1, 0), (-1, 1), (0, 1) }
            : new (int, int)[] { (1, 0), (1, -1), (0, -1), (-1, 0), (0, 1), (1, 1) };

        foreach (var (dx, dz) in offsets)
        {
            int nx = x + dx;
            int nz = z + dz;
            if (cellDict.ContainsKey((nx, nz)) && !IsCellOccupied(nx, nz, exclude))
                neighbors.Add((nx, nz));
        }
        return neighbors;
    }

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

    public static void HighlightCellsInRange(int centerX, int centerZ, int range)
    {
        var coords = GetCoordsInRange(centerX, centerZ, range);
        foreach (var (x, z) in coords)
        {
            HexCell cell = GetCell(x, z);
            if (cell != null) cell.SetHighlight(true);
        }
    }

    public static void ClearAllHighlights()
    {
        foreach (var cell in cellDict.Values)
        {
            cell.SetHighlight(false);
        }
    }

    public static void HighlightMoveCellsInRange(int centerX, int centerZ, int range)
    {
        var coords = GetCoordsInRange(centerX, centerZ, range);
        foreach (var (x, z) in coords)
        {
            if (IsCellOccupied(x, z)) continue;
            HexCell cell = GetCell(x, z);
            if (cell != null) cell.SetMoveHighlight(true);
        }
    }

    public static void ClearMoveHighlights()
    {
        foreach (var cell in cellDict.Values)
        {
            cell.SetMoveHighlight(false);
        }
    }
}
