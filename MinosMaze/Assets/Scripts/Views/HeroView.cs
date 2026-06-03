using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HeroView : CombatantView
{
    public HashSet<EnemyView> AttackedThisTurn { get; private set; } = new();

    void OnEnable()
    {
        ActionSystem.SubscribeReaction<DealDamageGA>(OnDealDamagePost, ReactionTiming.POST);
        ActionSystem.SubscribeReaction<EnemyTurnGA>(OnEnemyTurnPost, ReactionTiming.POST);
    }

    void OnDisable()
    {
        ActionSystem.UnsubscribeReaction<DealDamageGA>(OnDealDamagePost, ReactionTiming.POST);
        ActionSystem.UnsubscribeReaction<EnemyTurnGA>(OnEnemyTurnPost, ReactionTiming.POST);
    }

    private void OnDealDamagePost(DealDamageGA ga)
    {
        if (ga.Caster == this)
        {
            foreach (var target in ga.Targets)
                if (target is EnemyView ev)
                    AttackedThisTurn.Add(ev);
        }
    }

    private void OnEnemyTurnPost(EnemyTurnGA ga)
    {
        AttackedThisTurn.Clear();
    }

    public void Setup(HeroData heroData, int hexX, int hexZ)
    {
        HexCoordX = hexX;
        HexCoordZ = hexZ;
        SetupBase(heroData.Health, heroData.Image);
    }
}