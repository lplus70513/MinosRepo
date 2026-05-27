using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Data/RewardConfig")]
public class RewardConfig : ScriptableObject
{
    [Header("卡池")]
    public List<CardData> lootCardPool;

    [Header("普通怪金币权重（8~12）")]
    public List<GoldWeight> normalGoldWeights;

    [Header("精英怪金币权重（21~30）")]
    public List<GoldWeight> eliteGoldWeights;

    [Header("线掉落")]
    [Range(0f, 1f)] public float stringDropRate = 0.2f;
}
