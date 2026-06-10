using System.Collections.Generic;
using UnityEngine;

// 格子类型 → Prefab 映射条目
[System.Serializable]
public struct CellPrefabMapping
{
    public MapCellType cellType;
    public GameObject prefab;
}

// 随机类型条目（权重分配，后续补充具体规则）
[System.Serializable]
public struct RandomCellTypeEntry
{
    public MapCellType cellType;
    [Range(0, 100)] public int weight;
}

// 大地图六边形网格生成器，继承 HexGrid。
// 中心 (0,0) 固定为 BOSS 格，birthCoord 为出生格，其余按 randomPool 权重随机生成。
public class WorldMapGrid : HexGrid
{
    [Header("大地图配置")]
    [SerializeField] private Vector2Int birthCoord = new(2, 0);
    [SerializeField] private int initialMovePoints = 10;
    [SerializeField] private int playerMaxHealth = 100;
    [SerializeField] private HealthBarPanel healthBarPanel;

    [Header("格子类型-Prefab映射")]
    [SerializeField] private CellPrefabMapping[] cellPrefabMap;

    [Header("随机类型池（权重分配，暂留空）")]
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
        base.Awake(); // 生成格子，过程中会调用 GetPrefabForCell / CreateHexCell
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
        if (cellPrefabMap != null)
        {
            foreach (var entry in cellPrefabMap)
            {
                if (entry.prefab != null)
                    prefabLookup[entry.cellType] = entry.prefab;
            }
        }
    }

    // 构建格子类型布局：中心 BOSS，出生格 Birth，其余随机
    private void BuildCellTypeLayout()
    {
        cellTypeLayout.Clear();
        cellTypeLayout[(0, 0)] = MapCellType.WorldMap_Boss;
        cellTypeLayout[(birthCoord.x, birthCoord.y)] = MapCellType.WorldMap_Birth;
    }

    // 获取指定坐标的格子类型（BOSS/出生格优先，其次随机）
    public MapCellType GetCellType(int x, int z)
    {
        if (cellTypeLayout.TryGetValue((x, z), out var type))
            return type;

        // 随机生成并缓存
        MapCellType randomType = GetRandomCellType();
        cellTypeLayout[(x, z)] = randomType;
        return randomType;
    }

    // 从 randomPool 按权重随机选取类型，池为空时返回 WorldMap_Empty
    private MapCellType GetRandomCellType()
    {
        if (randomPool == null || randomPool.Length == 0)
            return MapCellType.WorldMap_Empty;

        int totalWeight = 0;
        foreach (var entry in randomPool)
            totalWeight += Mathf.Max(0, entry.weight);

        if (totalWeight <= 0)
            return MapCellType.WorldMap_Empty;

        int roll = Random.Range(0, totalWeight);
        int cumulative = 0;
        foreach (var entry in randomPool)
        {
            cumulative += Mathf.Max(0, entry.weight);
            if (roll < cumulative)
                return entry.cellType;
        }
        return randomPool[randomPool.Length - 1].cellType;
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
