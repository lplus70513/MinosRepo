using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class CardSystem : Singleton<CardSystem>
{
    [SerializeField] private HandView handView;
    [SerializeField] private Transform drawPilePoint;
    [SerializeField] private Transform discardPilePoint;

    public List<Card> GetDrawPileCopy() => new List<Card>(drawPile);
    public List<Card> GetDiscardPileCopy() => new List<Card>(discardPile);
    public List<Card> GetHandCopy() => new List<Card>(hand);
    public List<Card> GetExhaustPileCopy() => new List<Card>(exhaustPile);

    public List<Card> GetFullDeckCopy()
    {
        List<Card> full = new(drawPile.Count + discardPile.Count + exhaustPile.Count + hand.Count);
        full.AddRange(drawPile);
        full.AddRange(discardPile);
        full.AddRange(exhaustPile);
        full.AddRange(hand);
        full.Sort((a, b) => string.Compare(a.Name, b.Name));
        return full;
    }

    public void ExhaustCard(Card card) => exhaustPile.Add(card);

    public int GetTotalCardCount() => drawPile.Count + discardPile.Count + hand.Count + exhaustPile.Count;

    private readonly List<Card> drawPile = new();
    private readonly List<Card> discardPile = new();
    private readonly List<Card> hand = new();
    private readonly List<Card> exhaustPile = new();

    private int bonusDrawNextTurn = 0;
    private int freePlayRemaining = 0;
    private SelectCardFromHandGA pendingSelectGA;
    private bool subscriptionsActive = false;

    public int FreePlayRemaining => freePlayRemaining;
    public bool IsSelectingCardFromHand => pendingSelectGA != null;

    public void ConsumeFreePlay()
    {
        if (freePlayRemaining > 0) freePlayRemaining--;
    }

    void OnEnable()
    {
        if (subscriptionsActive)
        {
            Debug.LogWarning("[CardSystem] OnEnable 被重复调用但订阅已激活，跳过重复注册");
            return;
        }
        subscriptionsActive = true;
        Debug.Log("[CardSystem] OnEnable — 注册所有 Performer 和 SubscribeReaction");

        ActionSystem.AttachPerformer<DrawCardsGA>(DrawCardsPerformer);
        ActionSystem.AttachPerformer<DiscardAllCardsGA>(DiscardAllCardsPerformer);
        ActionSystem.AttachPerformer<PlayCardGA>(PlayCardPerformer);
        ActionSystem.AttachPerformer<BonusDrawGA>(BonusDrawPerformer);
        ActionSystem.AttachPerformer<AddCardToHandGA>(AddCardToHandPerformer);
        ActionSystem.AttachPerformer<ReturnToDrawPileGA>(ReturnToDrawPilePerformer);
        ActionSystem.AttachPerformer<SelectCardFromHandGA>(SelectCardFromHandPerformer);
        ActionSystem.AttachPerformer<FreePlayGA>(FreePlayPerformer);
        ActionSystem.AttachPerformer<RandomPlayFromHandGA>(RandomPlayFromHandPerformer);
        ActionSystem.SubscribeReaction<EnemyTurnGA>(EnemyTurnPreReaction, ReactionTiming.PRE);
        ActionSystem.SubscribeReaction<EnemyTurnGA>(EnemyTurnPostReaction, ReactionTiming.POST);
    }

    void OnDisable()
    {
        if (!subscriptionsActive) return;
        subscriptionsActive = false;
        Debug.Log("[CardSystem] OnDisable — 解除所有 Performer 和 SubscribeReaction");

        ActionSystem.DetachPerformer<DrawCardsGA>();
        ActionSystem.DetachPerformer<DiscardAllCardsGA>();
        ActionSystem.DetachPerformer<PlayCardGA>();
        ActionSystem.DetachPerformer<BonusDrawGA>();
        ActionSystem.DetachPerformer<AddCardToHandGA>();
        ActionSystem.DetachPerformer<ReturnToDrawPileGA>();
        ActionSystem.DetachPerformer<SelectCardFromHandGA>();
        ActionSystem.DetachPerformer<FreePlayGA>();
        ActionSystem.DetachPerformer<RandomPlayFromHandGA>();
        ActionSystem.UnsubscribeReaction<EnemyTurnGA>(EnemyTurnPreReaction, ReactionTiming.PRE);
        ActionSystem.UnsubscribeReaction<EnemyTurnGA>(EnemyTurnPostReaction, ReactionTiming.POST);
    }

    // publics

    public void SetUp(List<CardData> deckData)
    {
        foreach (var cardData in deckData)
        {
            Card card = new(cardData);
            drawPile.Add(card);
        }

        var innateCards = drawPile.FindAll(c => c.IsInnate);
        foreach (var card in innateCards)
        {
            drawPile.Remove(card);
            hand.Add(card);
            handView.AddCard(card, drawPilePoint.position, drawPilePoint.rotation);
        }
        if (innateCards.Count > 0)
            Debug.Log($"[CardSystem] 固有牌 {innateCards.Count} 张直接加入手牌");
    }

    private IEnumerator DrawCardsPerformer(DrawCardsGA drawCardsGA)
    {
        int actualAmount = Mathf.Min(drawCardsGA.Amount, drawPile.Count);
        int notDrawnAmount = drawCardsGA.Amount - actualAmount;

        for (int i = 0; i < actualAmount; i++)
        {
            yield return DrawCard();
        }
        if (notDrawnAmount > 0)
        {
            RefillDeck();
            for (int i = 0; i < notDrawnAmount; i++)
            {
                yield return DrawCard();
            }
        }
    }

    private IEnumerator PlayCardPerformer(PlayCardGA playCardGA)
    {
        hand.Remove(playCardGA.Card);
        CardView cardView = handView.RemoveCard(playCardGA.Card);

        // 启动卡牌出牌+弃置动画协程（与英雄攻击并发）
        Coroutine cardDiscardCoroutine = null;
        if (cardView != null && cardView.gameObject != null)
        {
            cardDiscardCoroutine = StartCoroutine(PlayCardDiscardSequence(cardView));
        }

        // ���ķ��� 
        SpendCostGA spendCostGA = new(playCardGA.Card.Cost);
        ActionSystem.Instance.AddReaction(spendCostGA);

        // 如果是手选目标攻击牌，立即开始英雄攻击动画
        if(playCardGA.Card.ManualTargetEffect != null)
        {
            HeroView hero = HeroSystem.Instance.HeroView;
            hero.SetFacing(playCardGA.ManualTarget.HexCoordX, playCardGA.ManualTarget.HexCoordZ);
            yield return hero.PlayAttackAnimation();

            PerformEffectGA performEffectGA = new(playCardGA.Card.ManualTargetEffect, new() { playCardGA.ManualTarget });
            ActionSystem.Instance.AddReaction(performEffectGA);
        }

        // 执行卡牌效果
        foreach (var effectWrapper  in playCardGA.Card.OtherEffects)
        {
            List<CombatantView> targets = effectWrapper.TargetMode.GetTargets();
            if (targets != null && !playCardGA.Card.CanHitFlying)
                targets = targets.FindAll(t => !(t is EnemyView ev && ev.EnemyType == EnemyType.Flying));
            if (playCardGA.Card.HasAttackRange && targets != null)
            {
                HeroView hero = HeroSystem.Instance.HeroView;
                targets = targets.FindAll(t => HexGrid.HexDistance(hero.HexCoordX, hero.HexCoordZ, t.HexCoordX, t.HexCoordZ) <= playCardGA.Card.AttackRange);
            }
            PerformEffectGA performEffectGA = new(effectWrapper.Effect, targets);
            ActionSystem.Instance.AddReaction(performEffectGA);
        }

        // 等待卡牌弃置动画完成
        if (cardDiscardCoroutine != null)
        {
            yield return cardDiscardCoroutine;
        }
    }

    private IEnumerator PlayCardDiscardSequence(CardView cardView)
    {
        Transform t = cardView.transform;
        t.DOKill();
        Tween scaleTween = t.DOScale(Vector3.one * 0.9f, 0.15f).SetEase(Ease.OutQuad);
        yield return scaleTween.WaitForCompletion();
        yield return new WaitForSeconds(0.2f);
        if (cardView.Card.IsExhaust)
            yield return ExhaustCardSequence(cardView);
        else
            yield return DiscardCard(cardView);
    }

    private IEnumerator DiscardAllCardsPerformer(DiscardAllCardsGA discardAllCardsGA)
    {
        List<Card> cardsToDiscard = new List<Card>(hand);

        foreach (var card in cardsToDiscard)
        {
            if (card.IsRetain) continue;

            hand.Remove(card);
            CardView cardView = handView.RemoveCard(card);
            if (cardView != null)
            {
                yield return DiscardCard(cardView);
            }
        }
    }


    private IEnumerator BonusDrawPerformer(BonusDrawGA ga)
    {
        bonusDrawNextTurn += ga.Amount;
        Debug.Log($"[CardSystem] 下回合额外抽牌 +{ga.Amount}，累计: {bonusDrawNextTurn}");
        yield return null;
    }

    private IEnumerator AddCardToHandPerformer(AddCardToHandGA ga)
    {
        if (ga.CardData == null)
        {
            Debug.LogWarning("[CardSystem] AddCardToHand 引用的 CardData 为空");
            yield break;
        }
        Card card = new(ga.CardData);
        hand.Add(card);
        CardView cardView = handView.AddCard(card, drawPilePoint.position, drawPilePoint.rotation);
        Debug.Log($"[CardSystem] 将 {card.Name} 加入手牌");
        yield return null;
    }

    private IEnumerator ReturnToDrawPilePerformer(ReturnToDrawPileGA ga)
    {
        if (ga.Card == null)
        {
            Debug.LogWarning("[CardSystem] ReturnToDrawPile 的卡牌为空");
            yield break;
        }
        hand.Remove(ga.Card);
        handView.RemoveCard(ga.Card);
        drawPile.Add(ga.Card);
        Debug.Log($"[CardSystem] 将 {ga.Card.Name} 放回抽牌堆");
        yield return null;
    }

    private IEnumerator SelectCardFromHandPerformer(SelectCardFromHandGA ga)
    {
        if (hand.Count == 0)
        {
            Debug.LogWarning("[CardSystem] 手牌为空，无法选择");
            yield break;
        }

        pendingSelectGA = ga;
        Debug.Log($"[CardSystem] 等待玩家从手牌中选择一张牌 (共 {hand.Count} 张)");

        while (ga.SelectedCard == null && pendingSelectGA == ga)
            yield return null;

        pendingSelectGA = null;

        if (ga.SelectedCard == null) yield break;

        if (ga.OnSelectAction is ReturnToDrawPileGA)
        {
            ReturnToDrawPileGA returnGA = new(ga.SelectedCard);
            ActionSystem.Instance.AddReaction(returnGA);
        }
    }

    public void OnHandCardSelected(Card card)
    {
        if (pendingSelectGA == null) return;
        if (!hand.Contains(card)) return;

        Debug.Log($"[CardSystem] 玩家选择了 {card.Name}");
        pendingSelectGA.SelectedCard = card;
    }

    private IEnumerator FreePlayPerformer(FreePlayGA ga)
    {
        freePlayRemaining += ga.Amount;
        Debug.Log($"[CardSystem] 免费出牌次数 +{ga.Amount}，剩余: {freePlayRemaining}");
        yield return null;
    }

    private IEnumerator RandomPlayFromHandPerformer(RandomPlayFromHandGA ga)
    {
        var nonTargeted = new List<Card>();
        var targeted = new List<Card>();
        var enemies = EnemySystem.Instance?.Enemies;

        foreach (var card in hand)
        {
            if (card.ManualTargetEffect != null)
                targeted.Add(card);
            else
                nonTargeted.Add(card);
        }

        int played = 0;
        for (int pass = 0; pass < 2 && played < ga.Amount; pass++)
        {
            var pool = pass == 0 ? nonTargeted : targeted;
            while (pool.Count > 0 && played < ga.Amount)
            {
                int idx = Random.Range(0, pool.Count);
                Card card = pool[idx];
                pool.RemoveAt(idx);

                EnemyView manualTarget = null;
                if (card.ManualTargetEffect != null && enemies != null && enemies.Count > 0)
                {
                    var hero = HeroSystem.Instance.HeroView;
                    var valid = new List<EnemyView>();
                    foreach (var e in enemies)
                    {
                        if (e == null) continue;
                        if (card.HasAttackRange && HexGrid.HexDistance(hero.HexCoordX, hero.HexCoordZ, e.HexCoordX, e.HexCoordZ) > card.AttackRange)
                            continue;
                        if (!card.CanHitFlying && e.EnemyType == EnemyType.Flying)
                            continue;
                        valid.Add(e);
                    }
                    if (valid.Count > 0)
                        manualTarget = valid[Random.Range(0, valid.Count)];
                    else
                        continue;
                }

                PlayCardGA playGA = manualTarget != null ? new(card, manualTarget) : new(card);
                ActionSystem.Instance.AddReaction(playGA);
                played++;
                Debug.Log($"[CardSystem] 随机自动打出: {card.Name}");
                yield return null;
            }
        }
        Debug.Log($"[CardSystem] 随机自动打出完成，共 {played} 张");
    }

    // Reactions

    private void EnemyTurnPreReaction(EnemyTurnGA enemyTurnGA)
    {
        DiscardAllCardsGA discardAllCardsGA = new();
        ActionSystem.Instance.AddReaction(discardAllCardsGA);
    }

    private void EnemyTurnPostReaction(EnemyTurnGA enemyTurnGA)
    {
        int drawAmount = 5 + bonusDrawNextTurn;
        Debug.Log($"[CardSystem] EnemyTurnPostReaction 触发 — bonusDraw={bonusDrawNextTurn}, 将抽 {drawAmount} 张");
        bonusDrawNextTurn = 0;
        DrawCardsGA drawCardsGA = new(drawAmount);
        ActionSystem.Instance.AddReaction(drawCardsGA);
    }

    // Helpers

    private IEnumerator DrawCard()
    {
        Card drawnCard = drawPile.Draw();

        if (drawnCard == null)
        {
            Debug.LogWarning("��ͼ�ӿ��ƶѳ��ƣ�");
            yield break;
        }

        hand.Add(drawnCard);

        CardView cardView = handView.AddCard(drawnCard, drawPilePoint.position, drawPilePoint.rotation);

        ActionSystem.Instance.AddReaction(new SingleDrawGA(drawnCard));
    }

    private void RefillDeck()
    {
        // �����ƶ�ϴ�ƺ������ƶ�
        drawPile.AddRange(discardPile);
        // ������ƶ�
        discardPile.Clear();
    }

    private IEnumerator DiscardCard(CardView cardView)
    {
        discardPile.Add(cardView.Card);

        if (cardView == null || cardView.gameObject == null)
            yield break;

        Transform t = cardView.transform;

        t.DOKill();

        t.DOScale(Vector3.zero, 0.15f);
        Tween moveTween = t.DOMove(discardPilePoint.position, 0.15f);

        yield return moveTween.WaitForCompletion();

        Destroy(cardView.gameObject);
    }

    private IEnumerator ExhaustCardSequence(CardView cardView)
    {
        exhaustPile.Add(cardView.Card);
        Debug.Log($"[CardSystem] {cardView.Card.Name} 被消耗");

        if (cardView == null || cardView.gameObject == null)
            yield break;

        Transform t = cardView.transform;
        t.DOKill();
        t.DOScale(Vector3.zero, 0.15f);
        Tween moveTween = t.DOMove(drawPilePoint.position, 0.15f);
        yield return moveTween.WaitForCompletion();
        Destroy(cardView.gameObject);
    }
}