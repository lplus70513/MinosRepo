using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class CardSystem : Singleton<CardSystem>
{
    [SerializeField] private HandView handView;
    [SerializeField] private Transform drawPilePoint;
    [SerializeField] private Transform discardPilePoint;

    private readonly List<Card> drawPile = new();
    private readonly List<Card> discardPile = new();
    private readonly List<Card> hand = new();

    void OnEnable()
    {
        ActionSystem.AttachPerformer<DrawCardsGA>(DrawCardsPerformer);
        ActionSystem.AttachPerformer<DiscardAllCardsGA>(DiscardAllCardsPerformer);
        ActionSystem.AttachPerformer<PlayCardGA>(PlayCardPerformer);
        ActionSystem.SubscribeReaction<EnemyTurnGA>(EnemyTurnPreReaction, ReactionTiming.PRE);
        ActionSystem.SubscribeReaction<EnemyTurnGA>(EnemyTurnPostReaction, ReactionTiming.POST);
    }

    void OnDisable()
    {
        ActionSystem.DetachPerformer<DrawCardsGA>();
        ActionSystem.DetachPerformer<DiscardAllCardsGA>();
        ActionSystem.DetachPerformer<PlayCardGA>();
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
        yield return DiscardCard(cardView);

        // 执行卡牌效果
        foreach (var effect in playCardGA.Card.Effects)
        {
            PerformEffectGA performEffectGA = new(effect);
            ActionSystem.Instance.AddReaction(performEffectGA);
        }
        // 加入弃牌堆
    }

    private IEnumerator DiscardAllCardsPerformer(DiscardAllCardsGA discardAllCardsGA)
    {
        // 注意：遍历时修改集合会报错，建议创建副本或反向遍历。
        // 这里假设 handView.RemoveCard 会安全地从 handCards 中移除对应项。
        // 但如果 handView 仅处理视觉，逻辑层仍需清空 hand。
        // 为安全起见，我们先收集所有要丢弃的卡牌，再依次处理。
        List<Card> cardsToDiscard = new List<Card>(hand);
        foreach (var card in cardsToDiscard)
        {
            // 从逻辑手牌中移除
            hand.Remove(card);

            // 从视觉手牌中移除并获取视图
            CardView cardView = handView.RemoveCard(card);
            if (cardView != null)
            {
                // 丢弃卡牌的视觉效果
                yield return DiscardCard(cardView);

                // 将卡牌移动到弃牌堆逻辑列表
                discardPile.Add(card);
            }
        }
        // 最后确保逻辑手牌列表被清空
        // hand.Clear(); // 因为我们已经在循环开始前就清除了，这里可以注释掉
    }


    // Reactions

    private void EnemyTurnPreReaction(EnemyTurnGA enemyTurnGA)
    {
        DiscardAllCardsGA discardAllCardsGA = new();
        ActionSystem.Instance.AddReaction(discardAllCardsGA);
    }

    private void EnemyTurnPostReaction(EnemyTurnGA enemyTurnGA)
    {
        DrawCardsGA drawCardsGA = new(5);
        ActionSystem.Instance.AddReaction(drawCardsGA);
    }

    // Helpers

    private IEnumerator DrawCard()
    {
        // 1. 从牌堆抽一张牌（使用你提供的 ListExtensions.Draw 方法）
        Card drawnCard = drawPile.Draw();

        // 2. 如果牌堆为空，Draw 方法会返回 null，此时不应继续
        if (drawnCard == null)
        {
            Debug.LogWarning("试图从空牌堆抽牌！");
            yield break; // 退出协程
        }

        // 3. 将抽到的牌加入手牌逻辑列表
        hand.Add(drawnCard);

        // 4. 请求 HandView 创建并添加卡牌的视觉对象
        //    传入卡牌模型和初始生成位置/旋转
        CardView cardView = handView.AddCard(drawnCard, drawPilePoint.position, drawPilePoint.rotation);

        // 5. 如果需要等待视觉效果完成（比如卡牌飞入手牌的动画），可以在此处 yield
        //    但目前 HandView.AddCard 是同步的，所以这里不需要额外的 yield。
        //    如果 HandView.AddCard 返回了协程（比如动画），则应写成：
        //    if(cardView != null) yield return StartCoroutine(handView.PlayDrawAnimation(cardView)); 
        //    但现在我们假设它是即时完成的。
    }

    private void RefillDeck()
    {
        // 将弃牌堆洗牌后放入抽牌堆
        drawPile.AddRange(discardPile);
        // 清空弃牌堆
        discardPile.Clear();
    }

    private IEnumerator DiscardCard(CardView cardView)
    {
        if (cardView == null || cardView.gameObject == null)
            yield break;

        Transform t = cardView.transform;

        // 关键：先 Kill 所有正在运行的动画，避免后续访问
        t.DOKill();

        // 执行动画
        t.DOScale(Vector3.zero, 0.15f);
        Tween moveTween = t.DOMove(discardPilePoint.position, 0.15f);

        // 等待动画完成（安全）
        yield return moveTween.WaitForCompletion();

        // 确保动画结束后再销毁
        Destroy(cardView.gameObject);
    }
}