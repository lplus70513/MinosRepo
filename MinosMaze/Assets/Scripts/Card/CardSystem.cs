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

        // ִ�п���Ч��
        foreach (var effectWrapper  in playCardGA.Card.OtherEffects)
        {
            List<CombatantView> targets = effectWrapper.TargetMode.GetTargets();
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
        yield return DiscardCard(cardView);
    }

    private IEnumerator DiscardAllCardsPerformer(DiscardAllCardsGA discardAllCardsGA)
    {
        // ע�⣺����ʱ�޸ļ��ϻᱨ�������鴴���������������
        // ������� handView.RemoveCard �ᰲȫ�ش� handCards ���Ƴ���Ӧ�
        // ����� handView �������Ӿ����߼���������� hand��
        // Ϊ��ȫ������������ռ�����Ҫ�����Ŀ��ƣ������δ�����
        List<Card> cardsToDiscard = new List<Card>(hand);
        foreach (var card in cardsToDiscard)
        {
            // ���߼��������Ƴ�
            hand.Remove(card);

            // ���Ӿ��������Ƴ�����ȡ��ͼ
            CardView cardView = handView.RemoveCard(card);
            if (cardView != null)
            {
                // �������Ƶ��Ӿ�Ч��
                yield return DiscardCard(cardView);

                // �������ƶ������ƶ��߼��б�
                // discardPile.Add(card);
            }
        }
        // ���ȷ���߼������б������
        // hand.Clear(); // ��Ϊ�����Ѿ���ѭ����ʼǰ������ˣ��������ע�͵�
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
        // 1. ���ƶѳ�һ���ƣ�ʹ�����ṩ�� ListExtensions.Draw ������
        Card drawnCard = drawPile.Draw();

        // 2. ����ƶ�Ϊ�գ�Draw �����᷵�� null����ʱ��Ӧ����
        if (drawnCard == null)
        {
            Debug.LogWarning("��ͼ�ӿ��ƶѳ��ƣ�");
            yield break; // �˳�Э��
        }

        // 3. ���鵽���Ƽ��������߼��б�
        hand.Add(drawnCard);

        // 4. ���� HandView ���������ӿ��Ƶ��Ӿ�����
        //    ���뿨��ģ�ͺͳ�ʼ����λ��/��ת
        CardView cardView = handView.AddCard(drawnCard, drawPilePoint.position, drawPilePoint.rotation);

        // 5. �����Ҫ�ȴ��Ӿ�Ч����ɣ����翨�Ʒ������ƵĶ������������ڴ˴� yield
        //    ��Ŀǰ HandView.AddCard ��ͬ���ģ��������ﲻ��Ҫ����� yield��
        //    ��� HandView.AddCard ������Э�̣����綯��������Ӧд�ɣ�
        //    if(cardView != null) yield return StartCoroutine(handView.PlayDrawAnimation(cardView)); 
        //    ���������Ǽ������Ǽ�ʱ��ɵġ�
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

        // �ؼ����� Kill �����������еĶ����������������
        t.DOKill();

        // ִ�ж���
        t.DOScale(Vector3.zero, 0.15f);
        Tween moveTween = t.DOMove(discardPilePoint.position, 0.15f);

        // �ȴ�������ɣ���ȫ��
        yield return moveTween.WaitForCompletion();

        // ȷ������������������
        Destroy(cardView.gameObject);
    }
}