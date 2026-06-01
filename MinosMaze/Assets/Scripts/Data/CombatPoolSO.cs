using System.Collections.Generic;
using UnityEngine;

// 战斗遭遇配置池 ScriptableObject，按 weight + minFloor/maxFloor 配置多条 CombatConfig。
// 在战斗场景的 MatchSetupSystem 中引用，Start 时按层数过滤后加权随机选中一条
[CreateAssetMenu(menuName = "MinosMaze/Combat Pool")]
public class CombatPoolSO : ScriptableObject
{
    public List<CombatConfig> configs;
}
