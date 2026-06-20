using System.Collections.Generic;
using UnityEngine;

// 格子类型 → Prefab 映射条目
[System.Serializable]
public struct CellPrefabMapping
{
    public MapCellType cellType;
    public GameObject prefab;
}

// 随机类型条目（权重分配，用于预计算各类型数量）
[System.Serializable]
public struct RandomCellTypeEntry
{
    public MapCellType cellType;
    [Range(0, 100)] public int weight;
}

// 大地图六边形网格生成器，继承 HexGrid。
// 中心 (0,0) 固定为 BOSS 格，birthCoord 为出生格，其余按 randomPool 权重预计算数量后带约束放置。
public class WorldMapGrid : HexGrid
{
    [Header("大地图配置")]
    [SerializeField] private Vector2Int birthCoord = new(2, 0);
    [SerializeField] private int initialMovePoints = 10;
    [SerializeField] private int playerMaxHealth = 100;
    [SerializeField] private HealthBarPanel healthBarPanel;

    [Header("格子类型-Prefab映射")]
    [SerializeField] private CellPrefabMapping[] worldCellPrefabMap;

    [Header("随机类型池（按权重计算各类型数量）")]
    [SerializeField] private RandomCellTypeEntry[] randomPool;

    // 运行时格子类型布局
    private Dictionary<(int, int), MapCellType> cellTypeLayout = new();
    // prefab 查找缓存
    private Dictionary<MapCellType, GameObject> prefabLookup = new();

    public Vector2Int BirthCoord => birthCoord;
    public int InitialMovePoints => initialMovePoints;

    protected override void Awake()
    {
        BuildPrefabLookup();
        BuildCellTypeLayout();
        base.Awake();
        SaveCellLayoutToState();
        PositionPlayer();
        DisableClearedCells();
        MapCollapseSystem.Reset();
    }

    // 场景恢复时直接禁用已走过的格子（不播放动画），跳过玩家当前站立的格子
    private void DisableClearedCells()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null || gm.WorldMapState == null) return;

        if (gm.WorldMapState.isNewGame)
        {
            gm.WorldMapState.clearedCells.Clear();
            return;
        }

        Vector2Int playerPos = new(gm.WorldMapState.playerPosX, gm.WorldMapState.playerPosZ);

        foreach (var coord in gm.WorldMapState.clearedCells)
        {
            if (coord == playerPos) continue;
            HexCell cell = HexGrid.GetCell(coord.x, coord.y);
            if (cell != null)
                cell.gameObject.SetActive(false);
        }
    }

    // 构建 prefab 类型查找表
    private void BuildPrefabLookup()
    {
        prefabLookup.Clear();
        if (worldCellPrefabMap != null)
        {
            foreach (var entry in worldCellPrefabMap)
            {
                if (entry.prefab != null)
                    prefabLookup[entry.cellType] = entry.prefab;
            }
        }
    }

    // 构建格子类型布局：优先从 WorldMapState 恢复，否则预设固定格后预生成所有格子类型
    private void BuildCellTypeLayout()
    {
        cellTypeLayout.Clear();

        GameManager gm = GameManager.Instance;

        if (GameManager.PendingNewGame)
        {
            GameManager.PendingNewGame = false;
            gm.ResetWorldMapStateForNewGame();
        }

        if (gm != null && gm.WorldMapState != null
            && !gm.WorldMapState.isNewGame
            && gm.WorldMapState.cellLayout.Count > 0)
        {
            foreach (var entry in gm.WorldMapState.cellLayout)
                cellTypeLayout[(entry.x, entry.z)] = entry.type;
            return;
        }

        cellTypeLayout[(0, 0)] = MapCellType.WorldMap_Boss;
        cellTypeLayout[(birthCoord.x, birthCoord.y)] = MapCellType.WorldMap_Birth;
        PreGenerateAllCellTypes();
    }

    // 将当前格子类型布局写入 WorldMapState 以便跨场景持久化
    private void SaveCellLayoutToState()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null || gm.WorldMapState == null) return;

        gm.WorldMapState.cellLayout.Clear();
        foreach (var kvp in cellTypeLayout)
            gm.WorldMapState.cellLayout.Add(new CellLayoutEntry(kvp.Key.Item1, kvp.Key.Item2, kvp.Value));
    }

    // 获取指定坐标的格子类型（所有类型已在 BuildCellTypeLayout 中预生成）
    public MapCellType GetCellType(int x, int z)
    {
        if (cellTypeLayout.TryGetValue((x, z), out var type))
            return type;

        Debug.LogWarning($"[WorldMapGrid] 坐标 ({x}, {z}) 未在布局中找到，返回 WorldMap_Empty");
        return MapCellType.WorldMap_Empty;
    }

    private static readonly (int dx, int dz)[] HexOffsets =
        { (1, 0), (-1, 0), (0, 1), (-1, 1), (1, -1), (0, -1) };

    private int CountAdjacentOfType(int x, int z, MapCellType type)
    {
        int count = 0;
        foreach (var (dx, dz) in HexOffsets)
        {
            if (cellTypeLayout.TryGetValue((x + dx, z + dz), out var t) && t == type)
                count++;
        }
        return count;
    }

    private void ShuffleList<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    // 最大余数法：根据 randomPool 权重将 available 个格子精确分配给各类型
    private Dictionary<MapCellType, int> CalculateTypeCounts(int available)
    {
        Dictionary<MapCellType, int> counts = new();
        if (randomPool == null || randomPool.Length == 0) return counts;

        int totalWeight = 0;
        foreach (var entry in randomPool)
            totalWeight += Mathf.Max(0, entry.weight);
        if (totalWeight <= 0) return counts;

        float[] exactCounts = new float[randomPool.Length];
        int[] floorCounts = new int[randomPool.Length];
        int floorSum = 0;

        for (int i = 0; i < randomPool.Length; i++)
        {
            exactCounts[i] = (float)Mathf.Max(0, randomPool[i].weight) / totalWeight * available;
            floorCounts[i] = Mathf.FloorToInt(exactCounts[i]);
            floorSum += floorCounts[i];
        }

        int remainder = available - floorSum;
        List<int> indices = new();
        for (int i = 0; i < randomPool.Length; i++) indices.Add(i);
        indices.Sort((a, b) =>
            (exactCounts[b] - floorCounts[b]).CompareTo(exactCounts[a] - floorCounts[a]));

        for (int i = 0; i < remainder && i < indices.Count; i++)
            floorCounts[indices[i]]++;

        for (int i = 0; i < randomPool.Length; i++)
        {
            if (floorCounts[i] > 0)
            {
                if (counts.ContainsKey(randomPool[i].cellType))
                    counts[randomPool[i].cellType] += floorCounts[i];
                else
                    counts[randomPool[i].cellType] = floorCounts[i];
            }
        }

        return counts;
    }

    // 预生成所有格子类型：按权重计算各类型数量，受约束类型优先放置，不满足约束时降级为 Encounter
    private void PreGenerateAllCellTypes()
    {
        List<(int x, int z)> allCoords = new();
        for (int z = -mapRadius; z <= mapRadius; z++)
            for (int x = -mapRadius - Mathf.Min(z, 0); x <= mapRadius - Mathf.Max(z, 0); x++)
                if (!cellTypeLayout.ContainsKey((x, z)))
                    allCoords.Add((x, z));

        int available = allCoords.Count;
        if (available <= 0) return;

        Dictionary<MapCellType, int> targetCounts = CalculateTypeCounts(available);
        if (targetCounts.Count == 0)
        {
            foreach (var (cx, cz) in allCoords)
                cellTypeLayout[(cx, cz)] = MapCellType.WorldMap_Empty;
            return;
        }

        ShuffleList(allCoords);

        Dictionary<MapCellType, int> constrainedTargets = new();
        int encounterTarget = 0;
        int eliteTarget = 0;

        foreach (var kvp in targetCounts)
        {
            if (kvp.Key == MapCellType.WorldMap_Encounter)
                encounterTarget = kvp.Value;
            else if (kvp.Key == MapCellType.WorldMap_Elite)
                eliteTarget = kvp.Value;
            else
                constrainedTargets[kvp.Key] = kvp.Value;
        }

        HashSet<int> assigned = new();
        int degradedToEncounter = 0;

        foreach (var kvp in constrainedTargets)
        {
            MapCellType type = kvp.Key;
            int remaining = kvp.Value;

            for (int i = 0; i < allCoords.Count && remaining > 0; i++)
            {
                if (assigned.Contains(i)) continue;
                var (cx, cz) = allCoords[i];
                if (CountAdjacentOfType(cx, cz, type) == 0)
                {
                    cellTypeLayout[(cx, cz)] = type;
                    assigned.Add(i);
                    remaining--;
                }
            }

            if (remaining > 0)
            {
                Debug.Log($"[WorldMapGrid] {type} 有 {remaining} 个因邻居约束无法放置，降级为 Encounter");
                degradedToEncounter += remaining;
            }
        }

        List<int> unassigned = new();
        for (int i = 0; i < allCoords.Count; i++)
            if (!assigned.Contains(i))
                unassigned.Add(i);

        List<MapCellType> fillPool = new();
        for (int i = 0; i < eliteTarget; i++)
            fillPool.Add(MapCellType.WorldMap_Elite);
        for (int i = 0; i < encounterTarget + degradedToEncounter; i++)
            fillPool.Add(MapCellType.WorldMap_Encounter);

        while (fillPool.Count < unassigned.Count)
            fillPool.Add(MapCellType.WorldMap_Encounter);

        ShuffleList(fillPool);

        for (int i = 0; i < unassigned.Count; i++)
        {
            var (cx, cz) = allCoords[unassigned[i]];
            cellTypeLayout[(cx, cz)] = fillPool[i];
        }
    }

    // 根据坐标返回对应格子类型的 prefab
    protected override GameObject GetPrefabForCell(int x, int z)
    {
        MapCellType type = GetCellType(x, z);
        if (prefabLookup.TryGetValue(type, out var prefab))
            return prefab;

        // 找不到映射时 fallback 到基类的 hexPrefab
        Debug.LogWarning($"[WorldMapGrid] 类型 {type} 无对应 prefab，使用默认 hexPrefab");
        return hexPrefab;
    }

    // 创建格子后设置其 cellType
    protected override void CreateHexCell(Vector3 position, int x, int z)
    {
        base.CreateHexCell(position, x, z);
        if (cellDict.TryGetValue((x, z), out HexCell cell))
        {
            cell.cellType = GetCellType(x, z);
        }
    }

    // 放置玩家：优先从 GameManager 恢复位置与生命值，否则使用出生格与默认属性
    private void PositionPlayer()
    {
        GameManager gm = GameManager.Instance;
        int posX = birthCoord.x;
        int posZ = birthCoord.y;
        int maxHp = playerMaxHealth;
        int curHp = playerMaxHealth;

        if (gm != null)
        {
            var state = gm.WorldMapState;
            if (state != null && !state.isNewGame && HexGrid.ContainsCell(state.playerPosX, state.playerPosZ))
            {
                posX = state.playerPosX;
                posZ = state.playerPosZ;
            }
            if (state != null)
            {
                state.isNewGame = false;
                if (state.maxHealth > 0)
                {
                    maxHp = state.maxHealth;
                    curHp = state.currentHealth;
                }
            }
        }

        if (WorldMapPlayerSystem.Instance != null)
        {
            Vector2Int spawnCoord = new(posX, posZ);
            WorldMapPlayerSystem.Instance.Setup(spawnCoord, maxHp, curHp);
        }

        if (healthBarPanel != null)
            healthBarPanel.SetupWorldMap(maxHp, curHp);
    }
}
