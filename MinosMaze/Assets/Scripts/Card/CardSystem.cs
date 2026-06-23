using System;
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
    private CardSelectOverlay cardSelectOverlay;

    public int FreePlayRemaining => freePlayRemaining;
    public bool IsSelectingCardFromHand => pendingSelectGA != null;

    public void ConsumeFreePlay()
    {
        if (freePlayRemaining > 0)
        {
            freePlayRemaining--;
            handView?.RefreshAllCostDisplays();
        }
    }

    void Awake()
    {
        base.Awake();
        GameObject overlayGO = new GameObject("CardSelectOverlay");
        overlayGO.transform.SetParent(transform);
        cardSelectOverlay = overlayGO.AddComponent<CardSelectOverlay>();
    }

    void OnEnable()
    {
        if (subscriptionsActive)
        {
            Debug.LogWarning("[CardSystem] OnEnable 被重复调用但订阅已激活，跳过重复注册");
            return;
        }
        subscriptionsActive = true;
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

    public void SetUp(List<DeckCardEntry> deckData)
    {
        foreach (var entry in deckData)
        {
            if (entry == null || entry.CardData == null) continue;
            Card card = new(entry.CardData, entry.IsUpgraded);
            drawPile.Add(card);
        }

        var innateCards = drawPile.FindAll(c => c.IsInnate);
        foreach (var card in innateCards)
        {
            drawPile.Remove(card);
            hand.Add(card);
            handView.AddCard(card, drawPilePoint.position, drawPilePoint.rotation);
        }
        // 固有牌已直接加入手牌
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

        // 消耗费用（免费出牌跳过）
        if (!playCardGA.IsFreePlay)
        {
            SpendCostGA spendCostGA = new(playCardGA.Card.Cost);
            ActionSystem.Instance.AddReaction(spendCostGA);
        }

        // 如果是手选目标攻击牌，立即开始英雄攻击动画
        if(playCardGA.Card.ManualTargetEffect != null)
        {
            HeroView hero = HeroSystem.Instance.HeroView;
            hero.SetFacing(playCardGA.ManualTarget.HexCoordX, playCardGA.ManualTarget.HexCoordZ);
            AudioManager.Instance?.PlaySFX(AudioManager.Instance?.Config?.playerAttackSFX);
            yield return hero.PlayAttackAnimation();

            List<CombatantView> targets = ResolveManualTargets(playCardGA);
            PerformEffectGA performEffectGA = new(playCardGA.Card.ManualTargetEffect, targets);
            ActionSystem.Instance.AddReaction(performEffectGA);
        }

        // 执行卡牌效果
        foreach (var effectWrapper  in playCardGA.Card.OtherEffects)
        {
            List<CombatantView> targets = effectWrapper.TargetMode is ManualTargetTM
                ? ResolveManualTargets(playCardGA)
                : effectWrapper.TargetMode.GetTargets();
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
        // 视觉移除（动画 + Destroy）已由 CardSelectOverlay 在确认时处理
        drawPile.Add(ga.Card);
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
        Card confirmedCard = null;
        bool done = false;

        Action<Card> onConfirmedHandler = (card) => { confirmedCard = card; done = true; };
        Action onCancelledHandler = () => { done = true; };
        cardSelectOverlay.OnConfirmed += onConfirmedHandler;
        cardSelectOverlay.OnCancelled += onCancelledHandler;
        cardSelectOverlay.StartSelection(drawPilePoint, handView);

        while (!done && pendingSelectGA == ga)
            yield return null;

        pendingSelectGA = null;
        cardSelectOverlay.OnConfirmed -= onConfirmedHandler;
        cardSelectOverlay.OnCancelled -= onCancelledHandler;

        if (confirmedCard == null) yield break;

        ga.SelectedCard = confirmedCard;

        if (ga.OnSelectAction is ReturnToDrawPileGA)
        {
            ReturnToDrawPileGA returnGA = new(confirmedCard);
            ActionSystem.Instance.AddReaction(returnGA);
        }
    }

    public void OnHandCardSelected(CardView cardView)
    {
        if (pendingSelectGA == null) return;
        if (!hand.Contains(cardView.Card)) return;

        cardSelectOverlay.OnCardLeftClicked(cardView);
    }

    private IEnumerator FreePlayPerformer(FreePlayGA ga)
    {
        freePlayRemaining += ga.Amount;
        handView?.RefreshAllCostDisplays();
        yield return null;
    }

    private IEnumerator RandomPlayFromHandPerformer(RandomPlayFromHandGA ga)
    {
        var nonTargeted = new List<Card>();
        var targeted = new List<Card>();
        var enemies = EnemySystem.Instance?.Enemies;
        var hero = HeroSystem.Instance?.HeroView;

        foreach (var card in hand)
        {
            if (card.ManualTargetEffect != null)
            {
                // 仅在有有效目标时才加入目标牌池
                bool hasValidTarget = false;
                if (enemies != null && hero != null)
                {
                    foreach (var e in enemies)
                    {
                        if (e == null) continue;
                        if (card.HasAttackRange && HexGrid.HexDistance(hero.HexCoordX, hero.HexCoordZ, e.HexCoordX, e.HexCoordZ) > card.AttackRange)
                            continue;
                        if (!card.CanHitFlying && e.EnemyType == EnemyType.Flying)
                            continue;
                        hasValidTarget = true;
                        break;
                    }
                }
                if (hasValidTarget)
                    targeted.Add(card);
            }
            else
            {
                nonTargeted.Add(card);
            }
        }

        int played = 0;
        for (int pass = 0; pass < 2 && played < ga.Amount; pass++)
        {
            var pool = pass == 0 ? nonTargeted : targeted;
            while (pool.Count > 0 && played < ga.Amount)
            {
                int idx = UnityEngine.Random.Range(0, pool.Count);
                Card card = pool[idx];
                pool.RemoveAt(idx);

                EnemyView manualTarget = null;
                if (card.ManualTargetEffect != null && enemies != null && enemies.Count > 0 && hero != null)
                {
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
                        manualTarget = valid[UnityEngine.Random.Range(0, valid.Count)];
                    else
                        continue;
                }

                // 模拟拖出并打出的动画
                CardView cardView = handView.GetCardView(card);
                if (cardView != null)
                {
                    cardView.BringToFront();
                    Vector3 origPos = cardView.transform.position;

                    // 阶段1：悬停抬起
                    cardView.transform.DOMoveY(origPos.y + 1.2f, 0.2f).SetEase(Ease.OutBack);
                    cardView.transform.DOScale(1.1f, 0.2f).SetEase(Ease.OutBack);
                    yield return new WaitForSeconds(0.25f);

                    // 阶段2：移动到打出位置
                    Vector3 playCenter = handView.GetHandCenterPosition();
                    cardView.transform.DOMove(playCenter, 0.25f).SetEase(Ease.InOutQuad);
                    yield return new WaitForSeconds(0.3f);
                }

                PlayCardGA playGA = manualTarget != null
                    ? new(card, manualTarget, isFreePlay: true)
                    : new(card, isFreePlay: true);
                ActionSystem.Instance.AddReaction(playGA);
                played++;
                yield return null;
            }
        }
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
        bonusDrawNextTurn = 0;
        DrawCardsGA drawCardsGA = new(drawAmount);
        ActionSystem.Instance.AddReaction(drawCardsGA);
    }

    // Helpers

    private List<CombatantView> ResolveManualTargets(PlayCardGA playCardGA)
    {
        var pattern = playCardGA.Card.AttackRangePattern;
        var manualTarget = playCardGA.ManualTarget;

        if (pattern == null)
            return new List<CombatantView> { manualTarget };

        var hero = HeroSystem.Instance.HeroView;
        var origin = new Vector2Int(hero.HexCoordX, hero.HexCoordZ);
        var targetCoord = new Vector2Int(manualTarget.HexCoordX, manualTarget.HexCoordZ);
        var affectedCells = pattern.GetAffectedCells(origin, targetCoord);

        var targets = new List<CombatantView>();
        var added = new HashSet<EnemyView>();
        var enemySystem = EnemySystem.Instance;

        foreach (var cell in affectedCells)
        {
            var enemy = enemySystem.GetEnemyAt(cell.x, cell.y);
            if (enemy == null || added.Contains(enemy)) continue;
            if (enemy != manualTarget && !playCardGA.Card.CanHitFlying && enemy.EnemyType == EnemyType.Flying)
                continue;
            added.Add(enemy);
            targets.Add(enemy);
        }

        if (!added.Contains(manualTarget))
        {
            added.Add(manualTarget);
            targets.Add(manualTarget);
        }

        return targets;
    }

    private IEnumerator DrawCard()
    {
        Card drawnCard = drawPile.Draw();

        if (drawnCard == null)
        {
            Debug.LogWarning("bug");
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