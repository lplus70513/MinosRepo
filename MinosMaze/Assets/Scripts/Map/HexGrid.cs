using System.Collections.Generic;
using UnityEngine;

// 特殊格配置条目：坐标 + 类型
[System.Serializable]
public struct SpecialCellConfig
{
    public Vector2Int coord;
    public MapCellType cellType;
}

// 六边形地图生成与管理器。
// 负责在 Awake 时根据 mapRadius 生成六角格地图，
// 并提供坐标查询、距离计算、范围获取、邻居获取、高亮显示等静态工具方法。
public class HexGrid : MonoBehaviour
{
    // 地图半径（以六角格为单位的环数），0 = 仅中心一格
    public int mapRadius = 2;

    // 六角格预制体，需挂载 HexCell 组件
    public GameObject hexPrefab;

    // 特殊格配置：在 Inspector 中指定 (坐标, 类型)，其余格默认为 Battle_Empty
    [SerializeField] private List<SpecialCellConfig> specialCellsConfig;

    // 各类型的特殊格所用预制体（墙格等）——找不到映射时 fallback 到 hexPrefab
    [SerializeField] private CellPrefabMapping[] cellPrefabMap;

    // 轴向坐标 (x, z) → HexCell 的映射字典，全局静态以便跨类访问
    // protected 使得 WorldMapGrid 子类可写入同一字典
    protected static Dictionary<(int x, int z), HexCell> cellDict = new();

    // 特殊格坐标 → 类型 的快速查找表
    private Dictionary<(int, int), MapCellType> specialCellLookup = new();

    // 类型 → 预制体 的快速查找表
    private Dictionary<MapCellType, GameObject> prefabLookup = new();

    protected virtual void Awake()
    {
        cellDict.Clear();
        BuildPrefabLookup();
        BuildSpecialCellLookup();
        CreateHexagonMap();
    }

    // 构建类型 → 预制体的快速查找表
    private void BuildPrefabLookup()
    {
        prefabLookup.Clear();
        if (cellPrefabMap != null)
        {
            foreach (var entry in cellPrefabMap)
            {
                if (entry.prefab != null)
                    prefabLookup[entry.cellType] = entry.prefab;
            }
        }
    }

    // 构建特殊格坐标 → 类型的快速查找表
    private void BuildSpecialCellLookup()
    {
        specialCellLookup.Clear();
        if (specialCellsConfig != null)
        {
            foreach (var entry in specialCellsConfig)
            {
                specialCellLookup[(entry.coord.x, entry.coord.y)] = entry.cellType;
            }
        }
    }

    // 生成六边形区域地图：以 (0,0) 为中心，逐 Z 层、逐 X 列生成六角格。
    // 内层循环的 X 范围在两端各缩进，形成六边形轮廓。
    protected virtual void CreateHexagonMap()
    {
        Vector3 center = Vector3.zero;

        for (int z = -mapRadius; z <= mapRadius; z++)
        {
            // 每一行 Z 的 X 范围：越靠近两端越窄，形成六边形形状
            for (int x = -mapRadius - Mathf.Min(z,0) ; x <= mapRadius - Mathf.Max(z,0); x++)
            {
                Vector3 position = center + GetHexWorldPosition(x, z);
                CreateHexCell(position, x, z);
            }
        }
    }

    // 轴向坐标 (x, z) → 世界坐标（Y 轴固定为 0）。
    // 水平偏移：(2*x + z) * innerRadius，错行排列实现六边形平铺。
    // 垂直偏移：-z * outerRadius * 1.5，保证行与行之间紧密贴合。
    protected virtual Vector3 GetHexWorldPosition(int x, int z)
    {
        Vector3 position;
        position.x = (2*x + z) * HexMetrics.innerRadius;
        position.y = 0f;
        position.z = -z * HexMetrics.outerRadius * 1.5f ;
        return position;
    }

    // 获取指定坐标处应使用的格子预制体（子类可覆写以支持多 prefab）
    protected virtual GameObject GetPrefabForCell(int x, int z)
    {
        if (specialCellLookup.TryGetValue((x, z), out MapCellType cellType))
        {
            if (prefabLookup.TryGetValue(cellType, out GameObject prefab))
                return prefab;
        }
        return hexPrefab;
    }

    // 在指定世界坐标处实例化一个六角格，设置其轴向坐标并注册到 cellDict
    protected virtual void CreateHexCell(Vector3 position, int x, int z)
    {
        GameObject prefab = GetPrefabForCell(x, z);
        if (prefab == null)
        {
            Debug.LogWarning($"[HexGrid] 坐标 ({x}, {z}) 无对应 prefab，跳过创建");
            return;
        }
        GameObject hexCellObject = Instantiate(prefab, position, Quaternion.Euler(0, 90, 0), transform);
        HexCell hexCell = hexCellObject.GetComponent<HexCell>();
        hexCell.SetCoord(x, z);
        if (specialCellLookup.TryGetValue((x, z), out MapCellType cellType))
            hexCell.cellType = cellType;
        cellDict[(x, z)] = hexCell;
    }

    // 根据轴向坐标获取 HexCell，不存在则返回 null
    public static HexCell GetCell(int x, int z)
    {
        cellDict.TryGetValue((x, z), out HexCell cell);
        return cell;
    }

    // 检查指定轴向坐标处是否存在六角格
    public static bool ContainsCell(int x, int z)
    {
        return cellDict.ContainsKey((x, z));
    }

    // 获取指定六角格上挂载的 standingPoint 世界坐标（单位站立位置）
    public static Vector3 GetStandingPoint(int x, int z)
    {
        if (cellDict.TryGetValue((x, z), out HexCell cell) && cell.standingPoint != null)
            return cell.standingPoint.position;
        Debug.LogWarning($"[HexGrid] 未找到六角格 ({x}, {z}) 或 standingPoint 为空");
        return Vector3.zero;
    }

    // 计算两个轴向坐标之间的六边形距离（步数）。
    // 内部将轴向坐标转为立方体坐标 (q, r, s) 后取切比雪夫距离。
    // 公式：distance = (|Δq| + |Δr| + |Δs|) / 2
    public static int HexDistance(int x1, int z1, int x2, int z2)
    {
        // 轴向 → 立方体坐标
        int q1 = x1 + z1;
        int r1 = -z1;
        int s1 = -q1 - r1;
        int q2 = x2 + z2;
        int r2 = -z2;
        int s2 = -q2 - r2;
        return (Mathf.Abs(q1 - q2) + Mathf.Abs(r1 - r2) + Mathf.Abs(s1 - s2)) / 2;
    }

    // 获取以 (centerX, centerZ) 为中心、range 步内的所有六角格坐标（含中心）
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

    // 判断两点是否在六角格的同一直线上（6个主方向之一）
    public static bool IsOnStraightLine(int fromX, int fromZ, int toX, int toZ)
    {
        int dx = toX - fromX;
        int dz = toZ - fromZ;
        if (dx == 0 && dz == 0) return true;
        (int x, int z)[] dirs = { (1, 0), (-1, 0), (0, 1), (-1, 1), (1, -1), (0, -1) };
        foreach (var (ddx, ddz) in dirs)
        {
            if (ddx == 0)
            {
                if (dx != 0) continue;
                if (ddz > 0 && dz > 0 && dz % ddz == 0) return true;
                if (ddz < 0 && dz < 0 && dz % ddz == 0) return true;
            }
            else if (ddz == 0)
            {
                if (dz != 0) continue;
                if (ddx > 0 && dx > 0 && dx % ddx == 0) return true;
                if (ddx < 0 && dx < 0 && dx % ddx == 0) return true;
            }
            else
            {
                if (dx % ddx != 0 || dz % ddz != 0) continue;
                if (dx / ddx != dz / ddz) continue;
                if (dx / ddx > 0) return true;
            }
        }
        return false;
    }

    // 高亮显示中心格 range 步内、且在同一直线上的所有格子
    public static void HighlightCellsOnStraightLines(int centerX, int centerZ, int range)
    {
        (int dx, int dz)[] dirs = { (1, 0), (-1, 0), (0, 1), (-1, 1), (1, -1), (0, -1) };
        foreach (var (ddx, ddz) in dirs)
        {
            for (int step = 1; step <= range; step++)
            {
                int x = centerX + ddx * step;
                int z = centerZ + ddz * step;
                if (!ContainsCell(x, z)) break;
                HexCell cell = GetCell(x, z);
                if (cell != null) cell.SetHighlight(true);
            }
        }
    }

    // 高亮显示中心格 range 步内的所有格子（用于显示攻击范围等）
    public static void HighlightCellsInRange(int centerX, int centerZ, int range)
    {
        var coords = GetCoordsInRange(centerX, centerZ, range);
        foreach (var (x, z) in coords)
        {
            HexCell cell = GetCell(x, z);
            if (cell != null) cell.SetHighlight(true);
        }
    }

    // 清除所有格子的攻击高亮
    public static void ClearAllHighlights()
    {
        foreach (var cell in cellDict.Values)
        {
            cell.SetHighlight(false);
        }
    }

    // 根据外部配置重建地图：销毁现有格子，用指定的半径和特殊格列表重新生成
    public void RebuildFromConfig(int radius, List<SpecialCellConfig> specialCells)
    {
        foreach (var cell in cellDict.Values)
        {
            if (cell != null)
                Destroy(cell.gameObject);
        }
        cellDict.Clear();

        mapRadius = radius;

        specialCellLookup.Clear();
        if (specialCells != null)
        {
            foreach (var entry in specialCells)
            {
                specialCellLookup[(entry.coord.x, entry.coord.y)] = entry.cellType;
            }
        }

        CreateHexagonMap();
        Debug.Log($"[HexGrid] RebuildFromConfig 完成: radius={mapRadius}, specialCells={specialCells?.Count ?? 0}");
    }

    // 供 HexMove 遍历所有六角格的外部访问器
    public static IEnumerable<HexCell> AllCells => cellDict.Values;
}
