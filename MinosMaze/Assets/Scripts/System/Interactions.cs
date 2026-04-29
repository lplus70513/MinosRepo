using UnityEngine;

// 用于禁用或开启玩家的交互

public class Interactions : Singleton<Interactions>
{
    public bool PlayerIsDragging { get; set; } = false;

    // 1、处理玩家在与卡牌交互时产生的，拖动与悬停之间的冲突
    public bool PlayerCanInteract()
    {
        if (!ActionSystem.Instance.IsPerforming) return true;
        else return false;
    }

    public bool PlayerCanHover()
    {
        if (PlayerIsDragging) return false;
        return true;
    }
}
