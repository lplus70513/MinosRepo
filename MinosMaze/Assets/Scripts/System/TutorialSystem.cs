using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

/// <summary>
/// 新手教程管理器。每个场景放置一个实例，配置该场景专属的教程面板序列。
/// 支持场景进入淡入、面板顺序播放、面板间平滑过渡。
/// </summary>
public class TutorialSystem : MonoBehaviour
{
    [Header("教程步骤（Prefab 列表，按顺序播放）")]
    [SerializeField] private List<TutorialPanel> tutorialSteps;

    [Header("过渡动画")]
    [SerializeField] private float sceneFadeInDelay = 0.3f;
    [SerializeField] private float sceneFadeInDuration = 0.3f;
    [SerializeField] private float stepInterval = 0.1f;    // 前一步关闭 → 后一步弹出之间的间隔

    /// <summary>全部教程播放完毕时触发</summary>
    public event Action OnTutorialComplete;

    /// <summary>当前播放到第几步（0-based，-1 表示尚未开始或已完成）</summary>
    public int CurrentStepIndex { get; private set; } = -1;

    /// <summary>是否正在播放教程</summary>
    public bool IsPlaying { get; private set; }

    /// <summary>是否已完成全部教程</summary>
    public bool IsComplete { get; private set; }

    private TutorialPanel currentPanel;
    private CanvasGroup rootCanvasGroup;

    /// <summary>本局已播放过教程的场景名集合（跨场景持久）</summary>
    private static readonly HashSet<string> playedScenes = new HashSet<string>();

    void Start()
    {
        if (tutorialSteps == null) tutorialSteps = new List<TutorialPanel>();

        rootCanvasGroup = GetComponent<CanvasGroup>();
        if (rootCanvasGroup == null)
            rootCanvasGroup = gameObject.AddComponent<CanvasGroup>();

        Play();
    }

    // ════════════════════════════════════════════════════════
    // 播放控制
    // ════════════════════════════════════════════════════════

    /// <summary>开始播放教程序列</summary>
    public void Play()
    {
        if (IsPlaying || IsComplete) return;

        string sceneName = gameObject.scene.name;
        if (playedScenes.Contains(sceneName))
        {
            Debug.Log($"[TutorialSystem] 场景 {sceneName} 教程本局已播放过，跳过。");
            Complete();
            return;
        }

        if (tutorialSteps != null)
            tutorialSteps.RemoveAll(s => s == null);
        if (tutorialSteps == null || tutorialSteps.Count == 0)
        {
            Debug.LogWarning("[TutorialSystem] 教程步骤列表为空，跳过教程。");
            Complete();
            return;
        }

        IsPlaying = true;
        CurrentStepIndex = -1;
        StartCoroutine(PlaySequence());
    }

    /// <summary>跳过当前教程面板</summary>
    public void SkipCurrentStep()
    {
        if (currentPanel != null)
            currentPanel.Hide();
    }

    /// <summary>跳过全部教程</summary>
    public void SkipAll()
    {
        if (currentPanel != null)
        {
            currentPanel.OnClosed -= OnStepClosed;
            Destroy(currentPanel.gameObject);
            currentPanel = null;
        }
        StopAllCoroutines();
        Complete();
    }

    /// <summary>重新开始教程（从头播放）</summary>
    public void Restart()
    {
        playedScenes.Remove(gameObject.scene.name);
        IsComplete = false;
        IsPlaying = false;
        CurrentStepIndex = -1;

        if (currentPanel != null)
        {
            currentPanel.OnClosed -= OnStepClosed;
            Destroy(currentPanel.gameObject);
            currentPanel = null;
        }

        Play();
    }

    // ════════════════════════════════════════════════════════
    // 时序控制
    // ════════════════════════════════════════════════════════

    private IEnumerator PlaySequence()
    {
        // 场景进入淡入：整个 TutorialSystem 从透明渐入
        rootCanvasGroup.alpha = 0f;
        yield return new WaitForSecondsRealtime(sceneFadeInDelay);
        rootCanvasGroup.DOFade(1f, sceneFadeInDuration).SetEase(Ease.OutQuad).SetUpdate(true);
        yield return new WaitForSecondsRealtime(sceneFadeInDuration);

        // 展示第一个教程面板
        yield return new WaitForSecondsRealtime(stepInterval);
        ShowNextStep();
    }

    private void ShowNextStep()
    {
        CurrentStepIndex++;

        if (CurrentStepIndex >= tutorialSteps.Count)
        {
            Complete();
            return;
        }

        TutorialPanel prefab = tutorialSteps[CurrentStepIndex];
        if (prefab == null)
        {
            Debug.LogError($"[TutorialSystem] 第 {CurrentStepIndex + 1} 个教程步骤 Prefab 为空，跳过。");
            ShowNextStep();
            return;
        }

        // 实例化并启动当前步骤
        currentPanel = Instantiate(prefab, transform);
        currentPanel.OnClosed += OnStepClosed;
        currentPanel.Show(CurrentStepIndex == 0);
    }

    private void OnStepClosed()
    {
        if (currentPanel != null)
        {
            currentPanel.OnClosed -= OnStepClosed;
            currentPanel = null;
        }

        StartCoroutine(WaitAndShowNext());
    }

    private IEnumerator WaitAndShowNext()
    {
        yield return new WaitForSecondsRealtime(stepInterval);
        ShowNextStep();
    }

    private void Complete()
    {
        bool wasPlaying = IsPlaying;
        IsPlaying = false;
        IsComplete = true;
        CurrentStepIndex = -1;

        if (wasPlaying && gameObject != null)
            playedScenes.Add(gameObject.scene.name);

        OnTutorialComplete?.Invoke();
    }

    // ════════════════════════════════════════════════════════
    // 公开调试/运行时查询
    // ════════════════════════════════════════════════════════

    /// <summary>获取剩余教程步数</summary>
    public int StepsRemaining
    {
        get
        {
            if (!IsPlaying || tutorialSteps == null) return 0;
            int remaining = tutorialSteps.Count - (CurrentStepIndex + 1);
            return Mathf.Max(0, remaining);
        }
    }

    /// <summary>总教程步数</summary>
    public int TotalSteps => tutorialSteps != null ? tutorialSteps.Count : 0;
}
