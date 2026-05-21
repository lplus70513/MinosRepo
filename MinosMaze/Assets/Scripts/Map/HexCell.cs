using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 地图格类型枚举（数值区间区分范畴：0-99 战斗格，100+ 大地图格）
public enum MapCellType
{
    // 战斗格 (0-99)
    Battle_Empty    = 0,    // 战斗空白格
    Battle_Trap     = 1,    // 战斗陷阱格

    // 大地图格 (100+)
    WorldMap_Empty      = 100,  // 大地图空白格（可通行，无事件，仅消耗移动点）
    WorldMap_Encounter  = 101,  // 大地图普通遭遇格
    WorldMap_Elite      = 102,  // 大地图精英遭遇格
    WorldMap_Birth      = 103,  // 大地图出生格（起点）
    WorldMap_Boss       = 104,  // 大地图BOSS格（本层终点）
    WorldMap_Statue     = 105,  // 大地图雕像格（事件/补给，暂留空）
}

/// <summary>
/// 单个六角格的 MonoBehaviour 组件。
/// 挂载在每个六角格预制体上，记录该格在六边形地图上的轴向坐标，
/// 以及提供站立点和高亮指示器的控制。
/// </summary>
public class HexCell : MonoBehaviour
{
    public int hexCoordX = 0;

    public int hexCoordZ = 0;

    // 该格子的地图格类型（默认战斗空白格，保证向后兼容）
    public MapCellType cellType = MapCellType.Battle_Empty;

    // 是否属于战斗范畴（0-99）
    public bool IsBattleCell => (int)cellType < 100;

    // 是否属于大地图范畴（100+）
    public bool IsWorldMapCell => (int)cellType >= 100;

    // 该格子上的单位站立位置 Transform（通常为子物体，标记角色应站的位置）
    public Transform standingPoint;

    // 攻击范围高亮指示器（子物体），激活时显示蓝色/红色高亮
    [SerializeField] private GameObject highlightIndicator;

    // 移动范围高亮指示器（子物体），激活时显示绿色高亮
    [SerializeField] private GameObject moveHighlightIndicator;

    // 设置该格的轴向坐标
    public void SetCoord(int x, int z)
    {
        hexCoordX = x;
        hexCoordZ = z;
    }

    // 获取该格的轴向坐标 (x, z)
    public (int x, int z) GetCoord()
    {
        return (hexCoordX, hexCoordZ);
    }

    // 控制攻击范围高亮指示器的显示/隐藏
    public void SetHighlight(bool active)
    {
        if (highlightIndicator != null)
            highlightIndicator.SetActive(active);
    }

    // 控制移动范围高亮指示器的显示/隐藏
    public void SetMoveHighlight(bool active)
    {
        if (moveHighlightIndicator != null)
            moveHighlightIndicator.SetActive(active);
    }
}
    