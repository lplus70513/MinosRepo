using System.Collections.Generic;
using UnityEngine;

public class RewardSystem : Singleton<RewardSystem>
{
    [SerializeField] private RewardConfig rewardConfig;

    public void SetConfig(RewardConfig config)
    {
        rewardConfig = config;
    }

    public BattleReward GenerateReward(bool isElite = false)
    {
        BattleReward reward = new();
        if (rewardConfig == null)
        {
            Debug.LogError("[RewardSystem] RewardConfig 未配置！");
            reward.CardChoices = new List<CardData>();
            return reward;
        }

        var goldWeights = isElite && rewardConfig.eliteGoldWeights != null && rewardConfig.eliteGoldWeights.Count > 0
            ? rewardConfig.eliteGoldWeights
            : rewardConfig.normalGoldWeights;
        reward.GoldAmount = WeightedRandom(goldWeights);

        reward.CardChoices = new List<CardData>();
        if (rewardConfig.lootCardPool != null && rewardConfig.lootCardPool.Count > 0)
        {
            var pool = new List<CardData>(rewardConfig.lootCardPool);
            for (int i = 0; i < 3 && pool.Count > 0; i++)
            {
                int idx = Random.Range(0, pool.Count);
                reward.CardChoices.Add(pool[idx]);
                pool.RemoveAt(idx);
            }
        }

        reward.HasString = Random.value < rewardConfig.stringDropRate;

        return reward;
    }

    private int WeightedRandom(List<GoldWeight> weights)
    {
        if (weights == null || weights.Count == 0) return 0;
        float total = 0f;
        foreach (var w in weights) total += w.weight;
        if (total <= 0f) return weights[0].amount;
        float roll = Random.Range(0f, total);
        float cumulative = 0f;
        foreach (var w in weights)
        {
            cumulative += w.weight;
            if (roll < cumulative) return w.amount;
        }
        return weights[^1].amount;
    }
}
