using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class CardSystem : Singleton<CardSystem>
{
    [SerializeField] private HandView handView;
    [SerializeField] private Transform drawPilePoint;
    [SerializeField] private Transform discardPilePoint;

    // [����] ��ȡ�ƶ����ݵĹ�������
    // ���ظ����Ա����ڲ����ݲ����ⲿ�޸�
    public List<Card> GetDrawPileCopy() => new List<Card>(drawPile);
    public List<Card> GetDiscardPileCopy() => new List<Card>(discardPile);
    public List<Card> GetHandCopy() => new List<Card>(hand);

    // [����] ��ȡ������ʼ���� (�����Ҫ�鿴���׿��鹹��)
    // �������� SetUp ʱ�����˳�ʼ���ݣ���������Ա��������������
    // ������ṩһ����ȡ��ǰ���п��������ĸ�������
    public int GetTotalCardCount() => drawPile.Count + discardPile.Count + hand.Count;

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

        // ���ķ��� 
        SpendCostGA spendCostGA = new(playCardGA.Card.Cost);
        ActionSystem.Instance.AddReaction(spendCostGA);

        // ����Ƿ�ѡ�����ֶ�Ŀ��
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
        // �������ƶ�
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