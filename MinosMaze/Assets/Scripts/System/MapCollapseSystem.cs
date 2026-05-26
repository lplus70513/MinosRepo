using UnityEngine;

// 地图塌陷系统：当英雄向中心移动时，最外圈六角格自动关闭。
// 始终保留英雄所在环外一圈的可见范围，超出部分 SetActive(false)。
public class MapCollapseSystem : Singleton<MapCollapseSystem>
{
    // 当前地图可见的最大环数（距离中心的六边形步数）
    private int currentMaxRing;

    void Start()
    {
        // 根据已有六角格计算地图的最大环数作为初始可见范围
        currentMaxRing = 0;
        foreach (var cell in HexGrid.AllCells)
        {
            int dist = HexGrid.HexDistance(0, 0, cell.hexCoordX, cell.hexCoordZ);
            if (dist > currentMaxRing) currentMaxRing = dist;
        }
    }

    void OnEnable()
    {
        ActionSystem.SubscribeReaction<MoveGA>(OnMovePost, ReactionTiming.POST);
    }

    void OnDisable()
    {
        ActionSystem.UnsubscribeReaction<MoveGA>(OnMovePost, ReactionTiming.POST);
    }

    // 英雄移动完成后检查是否需要塌陷外圈
    void OnMovePost(MoveGA ga)
    {
        HeroView hero = HeroSystem.Instance.HeroView;
        if (ga.Mover != hero) return;

        int playerRing = HexGrid.HexDistance(0, 0, ga.ToX, ga.ToZ);

        // 英雄向内移动时，持续收缩直到剩余一圈可见边界
        while (playerRing < currentMaxRing - 1)
        {
            CollapseRing(currentMaxRing);
            currentMaxRing--;
        }
    }

    // 将指定环上的所有六角格 SetActive(false)
    void CollapseRing(int ring)
    {
        foreach (var cell in HexGrid.AllCells)
        {
            if (HexGrid.HexDistance(0, 0, cell.hexCoordX, cell.hexCoordZ) == ring)
            {
                cell.gameObject.SetActive(false);
            }
        }
    }
}
