using System.Collections.Generic;
using UnityEngine;

// 游戏行动数据容器：

public abstract class GameAction
{
    // 1、行动前置反应
    public List<GameAction> PreReactions { get; private set; } = new();

    // 2、行动执行中反应
    public List<GameAction> PerformReactions { get; private set; } = new();

    // 3、行动结束反应
    public List<GameAction> PostReactions { get; private set; } = new();
}
