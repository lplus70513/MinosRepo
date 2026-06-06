using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// ��Ϸ��Ϊ������ 

public class ActionSystem : Singleton<ActionSystem>
{
    private List<GameAction> reactions = null;
    private bool cancelFlow = false;

    public bool IsPerforming { get; private set; } = false;

    private static Dictionary<Type, List<Action<GameAction>>> preSubs = new();
    private static Dictionary<Type, List<Action<GameAction>>> postSubs = new();
    private static Dictionary<Type, Dictionary<Delegate, Action<GameAction>>> preWrappers = new();
    private static Dictionary<Type, Dictionary<Delegate, Action<GameAction>>> postWrappers = new();
    private static Dictionary<Type, Func<GameAction,IEnumerator>> performers = new();

    // Perform执行核心
    public void Perform(GameAction action, System.Action OnPerformFinished = null)
    {
        // 防止重复进入，如果当前有正在执行的动作，忽略新动作
        if (IsPerforming) return;

        // 开始执行当前动作，阻止其他动作执行
        cancelFlow = false;
        IsPerforming = true;

        // 开始启动Flow处理该动作
        StartCoroutine(Flow(action, () =>
        {
            // 完结执行
            IsPerforming =  false;
            OnPerformFinished?.Invoke(); 
        }));
    }

    public void AddReaction(GameAction gameAction)
    {
        reactions?.Add(gameAction);
    }

    public void CancelCurrentFlow()
    {
        cancelFlow = true;
    }

    private IEnumerator Flow(GameAction action, Action OnFlowFinished = null)
    {
        if (cancelFlow)
        {
            OnFlowFinished?.Invoke();
            yield break;
        }

        reactions = action.PreReactions;
        PerformSubscribers(action, preSubs);
        yield return PerformReactions();
        if (cancelFlow) { OnFlowFinished?.Invoke(); yield break; }

        reactions = action.PerformReactions;
        yield return PerformPerformer(action);
        yield return PerformReactions();
        if (cancelFlow) { OnFlowFinished?.Invoke(); yield break; }

        reactions = action.PostReactions;
        int postSubCount = postSubs.ContainsKey(action.GetType()) ? postSubs[action.GetType()].Count : 0;
        Debug.Log($"[ActionSystem] POST phase for {action.GetType().Name}, subs count = {postSubCount}");
        PerformSubscribers(action, postSubs);
        yield return PerformReactions();

        OnFlowFinished?.Invoke();
    }

    private IEnumerator PerformPerformer(GameAction action)
    {
        if (cancelFlow) yield break;

        Type type = action.GetType();
        if (performers.ContainsKey(type))
        {
            yield return performers[type](action);
        }
    }

    private void PerformSubscribers(GameAction action, Dictionary<Type, List<Action<GameAction>>> subs)
    {
        if (cancelFlow) return;

        Type type = action.GetType();
        if (subs.ContainsKey(type))
        {
            foreach (var sub in subs[type])
            {
                if (cancelFlow) return;
                sub(action);
            }
        }
    }

    private IEnumerator PerformReactions()
    {
        foreach (var reaction in reactions)
        {
            if (cancelFlow) yield break;
            yield return Flow(reaction);
        }
    }

    public static void AttachPerformer<T>(Func<T, IEnumerator> performer) where T : GameAction
    {
        Type type = typeof(T);
        IEnumerator wrappedPerformer(GameAction action) => performer((T)action);
        if (performers.ContainsKey(type)) 
            performers[type] = wrappedPerformer;
        else 
            performers.Add(type, wrappedPerformer);
    }

    public static void DetachPerformer<T>() where T : GameAction
    {
        Type type = typeof(T);
        if (performers.ContainsKey(type)) performers.Remove(type);
    }

    public static void SubscribeReaction<T>(Action<T> reaction, ReactionTiming timing) where T : GameAction
    {
        Dictionary<Type, List<Action<GameAction>>> subs = timing == ReactionTiming.PRE ? preSubs : postSubs;
        Dictionary<Type, Dictionary<Delegate, Action<GameAction>>> wrapperMaps = timing == ReactionTiming.PRE ? preWrappers : postWrappers;
        Type type = typeof(T);

        if (!wrapperMaps.ContainsKey(type))
            wrapperMaps[type] = new();
        if (wrapperMaps[type].ContainsKey(reaction))
            return;

        void wrappedReaction(GameAction action) => reaction((T)action);
        wrapperMaps[type][reaction] = wrappedReaction;

        if (subs.ContainsKey(type))
        {
            subs[type].Add(wrappedReaction);
        }
        else
        {
            subs.Add(type, new());
            subs[type].Add(wrappedReaction);
        }
    }

    public static void UnsubscribeReaction<T>(Action<T> reaction, ReactionTiming timing) where T : GameAction
    {
        Dictionary<Type, List<Action<GameAction>>> subs = timing == ReactionTiming.PRE ? preSubs : postSubs;
        Dictionary<Type, Dictionary<Delegate, Action<GameAction>>> wrapperMaps = timing == ReactionTiming.PRE ? preWrappers : postWrappers;
        Type type = typeof(T);

        if (!wrapperMaps.TryGetValue(type, out var map))
            return;
        if (!map.TryGetValue(reaction, out var wrapper))
            return;

        map.Remove(reaction);
        if (subs.TryGetValue(type, out var list))
            list.Remove(wrapper);
    }
}