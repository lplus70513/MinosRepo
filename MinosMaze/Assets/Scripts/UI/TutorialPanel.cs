using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using DG.Tweening;
using TMPro;

/// <summary>
/// 单步教程面板。挂载在教程面板 Prefab 根节点上。
/// 支持同时显示图片、视频、文字说明三种媒介。
/// 关闭时通知 TutorialSystem 播放下一步。
/// </summary>
public class TutorialPanel : MonoBehaviour
{
    [Header("弹出动画")]
    [SerializeField] private Transform scaleRoot;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private float popDuration = 0.3f;
    [SerializeField] private float closeDuration = 0.15f;

    [Header("图片")]
    [SerializeField] private Image tutorialImage;
    [SerializeField] private Sprite imageSprite;

    [Header("视频")]
    [SerializeField] private RawImage videoRenderImage;
    [SerializeField] private VideoPlayer videoPlayer;
    [SerializeField] private VideoClip videoClip;
    [SerializeField] private bool videoLoop = true;

    [Header("标题")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private string title;

    [Header("文字说明")]
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField, TextArea(2, 8)] private string description;

    [Header("关闭")]
    [SerializeField] private Button closeButton;
    [SerializeField] private string closeButtonText = "继续";

    /// <summary>当前面板关闭时触发，供 TutorialSystem 衔接下一步</summary>
    public event Action OnClosed;

    private RenderTexture videoRenderTexture;

    void Awake()
    {
        if (scaleRoot == null)
            scaleRoot = transform;

        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        SetupMedia();
    }

    void OnEnable()
    {
        if (closeButton != null)
        {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(() => StartCoroutine(CloseCoroutine()));

            TMP_Text btnLabel = closeButton.GetComponentInChildren<TMP_Text>();
            if (btnLabel != null && !string.IsNullOrEmpty(closeButtonText))
                btnLabel.text = closeButtonText;
        }
    }

    void OnDisable()
    {
        StopVideo();
    }

    void OnDestroy()
    {
        CleanupVideoResources();
    }

    // ════════════════════════════════════════════════════════
    // 公开方法
    // ════════════════════════════════════════════════════════

    /// <summary>显示面板并播放弹出动画</summary>
    public virtual void Show()
    {
        gameObject.SetActive(true);
        canvasGroup.alpha = 0f;
        scaleRoot.localScale = Vector3.zero;

        scaleRoot.DOScale(1f, popDuration).SetEase(Ease.OutBack).SetUpdate(true);
        canvasGroup.DOFade(1f, popDuration).SetEase(Ease.OutQuad).SetUpdate(true);

        PlayVideo();
    }

    /// <summary>隐藏面板并播放淡出动画，动画完成后触发 OnClosed</summary>
    public virtual void Hide()
    {
        StartCoroutine(CloseCoroutine());
    }

    // ════════════════════════════════════════════════════════
    // 动画与回调
    // ════════════════════════════════════════════════════════

    private IEnumerator CloseCoroutine()
    {
        // 禁用按钮防止重复点击
        if (closeButton != null)
            closeButton.interactable = false;

        StopVideo();

        canvasGroup.DOFade(0f, closeDuration).SetEase(Ease.OutQuad).SetUpdate(true);
        yield return new WaitForSecondsRealtime(closeDuration);

        gameObject.SetActive(false);
        OnClosed?.Invoke();
    }

    // ════════════════════════════════════════════════════════
    // 媒介内容设置
    // ════════════════════════════════════════════════════════

    private void SetupMedia()
    {
        // 标题
        if (titleText != null && !string.IsNullOrEmpty(title))
        {
            titleText.text = title;
            titleText.gameObject.SetActive(true);
        }
        else if (titleText != null)
        {
            titleText.gameObject.SetActive(false);
        }

        // 图片
        if (tutorialImage != null && imageSprite != null)
        {
            tutorialImage.sprite = imageSprite;
            tutorialImage.gameObject.SetActive(true);
        }
        else if (tutorialImage != null)
        {
            tutorialImage.gameObject.SetActive(false);
        }

        // 视频
        bool hasVideo = videoClip != null || videoPlayer != null;
        if (hasVideo)
            SetupVideo();
        else if (videoRenderImage != null)
            videoRenderImage.gameObject.SetActive(false);

        // 文字
        if (descriptionText != null && !string.IsNullOrEmpty(description))
        {
            descriptionText.text = description;
            descriptionText.gameObject.SetActive(true);
        }
        else if (descriptionText != null)
        {
            descriptionText.gameObject.SetActive(false);
        }
    }

    private void SetupVideo()
    {
        if (videoRenderImage == null) return;

        // Auto-create or find VideoPlayer
        if (videoPlayer == null)
        {
            videoPlayer = GetComponentInChildren<VideoPlayer>(true);
            if (videoPlayer == null)
                videoPlayer = videoRenderImage.gameObject.AddComponent<VideoPlayer>();
        }

        if (videoClip != null)
            videoPlayer.clip = videoClip;

        videoPlayer.isLooping = videoLoop;
        videoPlayer.playOnAwake = false;
        videoPlayer.renderMode = VideoRenderMode.RenderTexture;

        // Auto-create RenderTexture
        if (videoPlayer.targetTexture == null)
        {
            int res = videoClip != null ? (int)Mathf.Max(videoClip.width, videoClip.height) : 512;
            res = Mathf.Clamp(res, 256, 1080);
            videoRenderTexture = new RenderTexture(res, res, 0);
            videoPlayer.targetTexture = videoRenderTexture;
        }

        videoRenderImage.texture = videoPlayer.targetTexture;
        videoRenderImage.gameObject.SetActive(true);
    }

    private void PlayVideo()
    {
        if (videoPlayer != null && videoPlayer.clip != null)
            videoPlayer.Play();
    }

    private void StopVideo()
    {
        if (videoPlayer != null)
            videoPlayer.Stop();
    }

    private void CleanupVideoResources()
    {
        if (videoRenderTexture != null)
        {
            videoRenderTexture.Release();
            if (Application.isPlaying)
                Destroy(videoRenderTexture);
            else
                DestroyImmediate(videoRenderTexture);
            videoRenderTexture = null;
        }
    }

    // ════════════════════════════════════════════════════════
    // 扩展点：子类可重写以下方法实现自定义行为
    // ════════════════════════════════════════════════════════

    /// <summary>Show() 之后、弹出动画完成时调用（子类可重写）</summary>
    protected virtual void OnShowComplete() { }

    /// <summary>Hide() / 关闭按钮触发后、淡出动画开始前调用（子类可重写）</summary>
    protected virtual void OnBeforeHide() { }
}
