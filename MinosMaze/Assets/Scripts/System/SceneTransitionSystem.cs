using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class SceneTransitionSystem : MonoBehaviour
{
    public static SceneTransitionSystem Instance { get; private set; }

    private float fadeOutDuration = 1f;
    private float fadeInDuration = 1f;
    private float blackScreenDuration = 0.5f;
    private int overlaySortingOrder = 100;
    private string dontCoverTag = "NoFade";

    private CanvasGroup canvasGroup;
    private Canvas overlayCanvas;
    private readonly Dictionary<Canvas, int> savedSortingOrders = new();
    public bool IsTransitioning { get; private set; }

    public void SetConfig(float fadeOut, float fadeIn, float blackScreen, int sortingOrder, string tag)
    {
        fadeOutDuration = fadeOut;
        fadeInDuration = fadeIn;
        blackScreenDuration = blackScreen;
        overlaySortingOrder = sortingOrder;
        dontCoverTag = tag;
        if (overlayCanvas != null)
            overlayCanvas.sortingOrder = sortingOrder;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        CreateOverlay();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void CreateOverlay()
    {
        GameObject overlayGo = new GameObject("FadeOverlay");
        overlayGo.transform.SetParent(transform, false);

        Canvas canvas = overlayGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = overlaySortingOrder;
        overlayCanvas = canvas;

        canvasGroup = overlayGo.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false;

        GameObject imageObj = new GameObject("BlackImage");
        imageObj.transform.SetParent(overlayGo.transform, false);
        Image image = imageObj.AddComponent<Image>();
        image.color = Color.black;
        RectTransform rect = image.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.sizeDelta = Vector2.zero;
    }

    public IEnumerator FadeOut(float duration = -1f)
    {
        if (duration < 0f) duration = fadeOutDuration;
        IsTransitioning = true;
        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = true;

        savedSortingOrders.Clear();
        if (!string.IsNullOrEmpty(dontCoverTag))
        {
            var allCanvases = FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var c in allCanvases)
            {
                if (c != null && c.gameObject.tag == dontCoverTag && c.sortingOrder <= overlaySortingOrder)
                {
                    savedSortingOrders[c] = c.sortingOrder;
                    c.sortingOrder = overlaySortingOrder + 1;
                }
            }
        }

        Tween tween = canvasGroup.DOFade(1f, duration).SetEase(Ease.Linear);
        yield return tween.WaitForCompletion();
    }

    public IEnumerator FadeIn(float duration = -1f)
    {
        if (duration < 0f) duration = fadeInDuration;
        canvasGroup.alpha = 1f;
        Tween tween = canvasGroup.DOFade(0f, duration).SetEase(Ease.Linear);
        yield return tween.WaitForCompletion();
        canvasGroup.blocksRaycasts = false;

        foreach (var kvp in savedSortingOrders)
        {
            if (kvp.Key != null)
                kvp.Key.sortingOrder = kvp.Value;
        }
        savedSortingOrders.Clear();

        IsTransitioning = false;
    }

    public IEnumerator BlackScreenWait()
    {
        yield return new WaitForSeconds(blackScreenDuration);
    }
}
