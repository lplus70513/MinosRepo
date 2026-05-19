using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 单个六角格的 MonoBehaviour 组件。
/// 挂载在每个六角格预制体上，记录该格在六边形地图上的轴向坐标，
/// 以及提供站立点和高亮指示器的控制。
/// </summary>
public class HexCell : MonoBehaviour
{
    public int hexCoordX = 0;

    public int hexCoordZ = 0;

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
