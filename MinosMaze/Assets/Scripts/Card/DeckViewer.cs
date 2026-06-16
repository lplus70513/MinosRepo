using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering;
using TMPro;

public enum PileType
{
    DrawPile,
    DiscardPile,
    ExhaustPile,
    FullDeck
}

public class DeckViewer : MonoBehaviour
{
    [Header("Panel")]
    [SerializeField] private GameObject viewerPanel;
    [SerializeField] private GameObject closeButton;
    [SerializeField] private Image panelBackgroundImage;

    [Header("Tabs (Canvas 按钮)")]
    [SerializeField] private Button drawPileTab;
    [SerializeField] private Button discardPileTab;
    [SerializeField] private Button exhaustPileTab;
    [SerializeField] private Button fullDeckTab;

    [Header("Card Grid")]
    [SerializeField] private Transform cardContainer;
    [SerializeField] private GameObject cardPrefab;
    [SerializeField] private float cardScale = 0.5f;
    [SerializeField] private int columns = 5;
    [SerializeField] private Vector2 spacing = new Vector2(3.5f, 5f);
    [SerializeField] private float hitboxWidth = 2.5f;
    [SerializeField] private float hitboxHeight = 3.5f;
    [SerializeField] private int cardSortingOrder = 1;
    [SerializeField] private int handCardSortingOrder = -1;

    [Header("Overlay")]
    [SerializeField] private float overlayAlpha = 0.7f;
    [SerializeField] private int overlaySortingOrder = 5;

    [Header("Scroll")]
    [SerializeField] private float scrollSpeed = 3f;

    [Header("Blocked Buttons (打开面板时禁用)")]
    [SerializeField] private List<Button> blockedButtons;

    [Header("Selection Mode")]
    [SerializeField] private float previewCardScale = 1.0f;

    [Header("升级晃动动画")]
    [SerializeField] private float shakeAngle = 15f;
    [SerializeField] private float shakeInterval = 0.1f;

    private PileType currentPile = PileType.DrawPile;
    private Dictionary<PileType, Button> tabButtons;
    private float scrollOffset;
    private float maxScrollOffset;
    private float previousTimeScale;
    private Vector3 cardContainerBasePos;
    private GameObject overlayObject;
    private Sprite overlaySprite;
    private SortingGroup cardContainerSortingGroup;
    private readonly Dictionary<CardView, int> handSortingOrderBackup = new();

    private bool isSelectionMode;
    private bool isPreviewState;
    private bool isUpgradePreview;
    private DeckCardEntry previewedEntry;
    private List<DeckCardEntry> selectionEntries;
    private Action<DeckCardEntry> selectionCallback;
    private Action cancelCallback;

    private void Start()
    {
        if (viewerPanel != null) viewerPanel.SetActive(false);
        if (cardContainer != null)
        {
            cardContainerBasePos = cardContainer.localPosition;
            cardContainer.gameObject.SetActive(false);

            cardContainerSortingGroup = cardContainer.GetComponent<SortingGroup>();
            if (cardContainerSortingGroup == null)
                cardContainerSortingGroup = cardContainer.gameObject.AddComponent<SortingGroup>();
        }

        ValidateSetup();

        tabButtons = new Dictionary<PileType, Button>
        {
            { PileType.DrawPile, drawPileTab },
            { PileType.DiscardPile, discardPileTab },
            { PileType.ExhaustPile, exhaustPileTab },
            { PileType.FullDeck, fullDeckTab }
        };

        if (drawPileTab != null) drawPileTab.onClick.AddListener(() => OpenViewer(PileType.DrawPile));
        if (discardPileTab != null) discardPileTab.onClick.AddListener(() => OpenViewer(PileType.DiscardPile));
        if (exhaustPileTab != null) exhaustPileTab.onClick.AddListener(() => OpenViewer(PileType.ExhaustPile));
        if (fullDeckTab != null) fullDeckTab.onClick.AddListener(() => OpenViewer(PileType.FullDeck));

        if (closeButton != null)
        {
            Button btn = closeButton.GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick.AddListener(CloseViewer);
                Debug.Log("[DeckViewer] 关闭按钮已绑定 (Canvas Button)");
            }
            else
            {
                Collider col = closeButton.GetComponent<Collider>();
                if (col == null)
                {
                    BoxCollider boxCol = closeButton.AddComponent<BoxCollider>();
                    boxCol.size = new Vector3(2f, 1f, 1f);
                    Debug.Log("[DeckViewer] 关闭按钮自动添加了 BoxCollider");
                }
                Clickable3D clickable = closeButton.GetComponent<Clickable3D>();
                if (clickable == null) clickable = closeButton.AddComponent<Clickable3D>();
                clickable.onClick.AddListener(CloseViewer);
                Debug.Log("[DeckViewer] 关闭按钮已绑定 (3D Clickable3D)");
            }
        }
    }

    private void ValidateSetup()
    {
        if (viewerPanel == null)
            Debug.LogError("[DeckViewer] viewerPanel 未配置！");
        if (cardContainer == null)
            Debug.LogError("[DeckViewer] cardContainer 未配置！");
        if (cardPrefab == null)
            Debug.LogError("[DeckViewer] cardPrefab 未配置！");
        if (closeButton == null)
            Debug.LogWarning("[DeckViewer] closeButton 未配置，关闭按钮将不可用。");
        if (drawPileTab == null && discardPileTab == null && exhaustPileTab == null && fullDeckTab == null)
            Debug.LogWarning("[DeckViewer] 所有标签按钮均未配置。");
        if (cardContainer != null && cardContainer is RectTransform)
            Debug.LogError("[DeckViewer] cardContainer 是 RectTransform（Canvas 子物体）！请移到 Canvas 外部。");
    }

    public void OpenViewer(PileType type)
    {
        if (CardSystem.Instance == null)
        {
            Debug.LogError("[DeckViewer] CardSystem.Instance 为 null。");
            return;
        }

        Debug.Log($"[DeckViewer] 打开检视面板，牌堆类型: {type}");

        previousTimeScale = Time.timeScale;
        Time.timeScale = 0f;
        currentPile = type;

        if (Interactions.Instance != null) Interactions.Instance.IsViewingDeck = true;

        CreateOverlay();
        SetSortingGroups();
        BlockButtons();
        if (panelBackgroundImage != null) panelBackgroundImage.enabled = false;

        UpdateView(type);
        HighlightTab(type);
        if (viewerPanel != null) viewerPanel.SetActive(true);
        if (cardContainer != null) cardContainer.gameObject.SetActive(true);
    }

    public void CloseViewer()
    {
        if (isSelectionMode)
        {
            if (isPreviewState)
            {
                ShowSelectionGrid();
                return;
            }
            var cb = cancelCallback;
            CloseSelectionMode();
            cb?.Invoke();
            return;
        }

        if (Interactions.Instance != null) Interactions.Instance.IsViewingDeck = false;

        DestroyOverlay();
        RestoreSortingGroups();
        RestoreButtons();
        if (panelBackgroundImage != null) panelBackgroundImage.enabled = true;

        ResetAllTabButtons();

        Time.timeScale = previousTimeScale;
        if (viewerPanel != null) viewerPanel.SetActive(false);
        if (cardContainer != null) cardContainer.gameObject.SetActive(false);
    }

    private void Update()
    {
        if (viewerPanel == null || !viewerPanel.activeSelf) return;
        if (cardContainer == null) return;

        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.001f)
        {
            scrollOffset += scroll * scrollSpeed;
            scrollOffset = Mathf.Clamp(scrollOffset, 0f, maxScrollOffset);
            cardContainer.localPosition = cardContainerBasePos + new Vector3(0, scrollOffset, 0);
        }
    }

    private void OnDestroy()
    {
        if (Time.timeScale == 0f) Time.timeScale = 1f;
        if (Interactions.Instance != null) Interactions.Instance.IsViewingDeck = false;
        DestroyOverlay();
        RestoreSortingGroups();
        RestoreButtons();
    }

    private void UpdateView(PileType type)
    {
        if (CardSystem.Instance == null) return;

        foreach (Transform child in cardContainer)
        {
            Destroy(child.gameObject);
        }

        List<Card> snapshot = GetSnapshot(type);
        Debug.Log($"[DeckViewer] 快照获取到 {snapshot.Count} 张卡牌");

        if (snapshot.Count == 0)
        {
            Debug.LogWarning("[DeckViewer] 快照为空！");
            maxScrollOffset = 0f;
            return;
        }

        if (type == PileType.DrawPile)
            Shuffle(snapshot);

        int totalRows = Mathf.CeilToInt((float)snapshot.Count / columns);
        maxScrollOffset = Mathf.Max(0, (totalRows - 1) * spacing.y);
        scrollOffset = 0f;
        cardContainer.localPosition = cardContainerBasePos;

        Vector3 cardScaleVec = Vector3.one * cardScale;

        for (int i = 0; i < snapshot.Count; i++)
        {
            int col = i % columns;
            int row = i / columns;
            Vector3 pos = new Vector3(col * spacing.x, -row * spacing.y, 0f);
            BuildCardEntry(snapshot[i], pos, cardScaleVec);
        }
    }

    private void BuildCardEntry(Card card, Vector3 localPos, Vector3 scale)
    {
        GameObject cardObj = Instantiate(cardPrefab, cardContainer);
        cardObj.transform.localPosition = localPos;
        cardObj.transform.localScale = scale;

        CardView cardView = cardObj.GetComponent<CardView>();
        if (cardView != null)
        {
            cardView.SetUp(card);
        }

        foreach (Collider col in cardObj.GetComponentsInChildren<Collider>(true))
        {
            col.enabled = false;
        }

        GameObject hitbox = new GameObject("Hitbox");
        hitbox.transform.SetParent(cardObj.transform, false);
        hitbox.transform.localPosition = Vector3.zero;
        hitbox.transform.localScale = Vector3.one;
        hitbox.layer = cardObj.layer;

        BoxCollider boxCol = hitbox.AddComponent<BoxCollider>();
        boxCol.size = new Vector3(hitboxWidth, hitboxHeight, 1f);

        CardGridHoverTrigger trigger = hitbox.AddComponent<CardGridHoverTrigger>();
        trigger.Init(card);
    }

    private void SetSortingGroups()
    {
        if (handSortingOrderBackup.Count > 0) return;

        if (cardContainerSortingGroup != null)
        {
            cardContainerSortingGroup.sortingOrder = cardSortingOrder;
            Debug.Log($"[DeckViewer] cardContainer SortingGroup => {cardSortingOrder}");
        }   

        CardView[] allCardViews = FindObjectsByType<CardView>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (CardView cv in allCardViews)
        {
            if (cv == null) continue;
            if (cardContainer != null && cv.transform.IsChildOf(cardContainer)) continue;

            SortingGroup sg = cv.GetComponent<SortingGroup>();
            if (sg != null)
            {
                handSortingOrderBackup[cv] = sg.sortingOrder;
                sg.sortingOrder = handCardSortingOrder;
            }
        }

        Debug.Log($"[DeckViewer] 已调整 {handSortingOrderBackup.Count} 张手牌 SortingGroup => {handCardSortingOrder}");
    }

    private void RestoreSortingGroups()
    {
        foreach (var kvp in handSortingOrderBackup)
        {
            if (kvp.Key == null) continue;
            SortingGroup sg = kvp.Key.GetComponent<SortingGroup>();
            if (sg != null) sg.sortingOrder = kvp.Value;
        }
        handSortingOrderBackup.Clear();

        if (cardContainerSortingGroup != null) cardContainerSortingGroup.sortingOrder = 0;
    }

    private void CreateOverlay()
    {
        if (overlayObject != null) return;

        if (overlaySprite == null)
        {
            Texture2D tex = new Texture2D(1, 1);
            tex.SetPixel(0, 0, Color.white);
            tex.Apply();
            overlaySprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f));
        }

        overlayObject = new GameObject("DeckViewerOverlay");
        SpriteRenderer sr = overlayObject.AddComponent<SpriteRenderer>();
        sr.sprite = overlaySprite;
        sr.color = new Color(0, 0, 0, overlayAlpha);
        sr.sortingOrder = overlaySortingOrder;

        if (Camera.main != null)
        {
            float camHeight = Camera.main.orthographicSize * 2f;
            float camWidth = camHeight * Camera.main.aspect;
            Vector3 camPos = Camera.main.transform.position;
            overlayObject.transform.position = new Vector3(camPos.x, camPos.y, 0f);
            overlayObject.transform.localScale = new Vector3(camWidth, camHeight, 1f);
        }

        Debug.Log("[DeckViewer] 全屏遮罩已创建");
    }

    private void DestroyOverlay()
    {
        if (overlayObject != null)
        {
            Destroy(overlayObject);
            overlayObject = null;
        }
    }

    private void BlockButtons()
    {
        if (blockedButtons == null) return;
        foreach (var btn in blockedButtons)
        {
            if (btn != null) btn.interactable = false;
        }
    }

    private void RestoreButtons()
    {
        if (blockedButtons == null) return;
        foreach (var btn in blockedButtons)
        {
            if (btn != null) btn.interactable = true;
        }
    }

    private void ResetAllTabButtons()
    {
        foreach (var kvp in tabButtons)
        {
            if (kvp.Value != null) kvp.Value.interactable = true;
        }
    }

    private List<Card> GetSnapshot(PileType type)
    {
        return type switch
        {
            PileType.DrawPile => CardSystem.Instance.GetDrawPileCopy(),
            PileType.DiscardPile => CardSystem.Instance.GetDiscardPileCopy(),
            PileType.ExhaustPile => CardSystem.Instance.GetExhaustPileCopy(),
            PileType.FullDeck => CardSystem.Instance.GetFullDeckCopy(),
            _ => new List<Card>()
        };
    }

    private void HighlightTab(PileType active)
    {
        foreach (var kvp in tabButtons)
        {
            if (kvp.Value != null)
                kvp.Value.interactable = (kvp.Key != active);
        }
    }

    private void Shuffle(List<Card> list)
    {
        System.Random rng = new System.Random();
        int n = list.Count;
        while (n > 1)
        {
            n--;
            int k = rng.Next(n + 1);
            (list[k], list[n]) = (list[n], list[k]);
        }
    }

    // ========== 选择模式 ==========

    public void OpenForSelection(List<DeckCardEntry> entries, Action<DeckCardEntry> onSelected, Action onCancelled, bool upgradePreview = false)
    {
        isSelectionMode = true;
        isPreviewState = false;
        isUpgradePreview = upgradePreview;
        previewedEntry = null;
        selectionEntries = entries;
        selectionCallback = onSelected;
        cancelCallback = onCancelled;

        previousTimeScale = Time.timeScale;
        Time.timeScale = 0f;

        CreateOverlay();
        SetSortingGroups();
        if (panelBackgroundImage != null) panelBackgroundImage.enabled = false;
        if (viewerPanel != null) viewerPanel.SetActive(true);
        if (cardContainer != null) cardContainer.gameObject.SetActive(true);

        ShowSelectionGrid();
    }

    private void ShowSelectionGrid()
    {
        isPreviewState = false;
        previewedEntry = null;

        foreach (Transform child in cardContainer)
        {
            Destroy(child.gameObject);
        }

        if (selectionEntries == null || selectionEntries.Count == 0)
        {
            maxScrollOffset = 0f;
            return;
        }

        int totalRows = Mathf.CeilToInt((float)selectionEntries.Count / columns);
        maxScrollOffset = Mathf.Max(0, (totalRows - 1) * spacing.y);
        scrollOffset = 0f;
        cardContainer.localPosition = cardContainerBasePos;

        Vector3 cardScaleVec = Vector3.one * cardScale;

        for (int i = 0; i < selectionEntries.Count; i++)
        {
            int col = i % columns;
            int row = i / columns;
            Vector3 pos = new Vector3(col * spacing.x, -row * spacing.y, 0f);
            DeckCardEntry entry = selectionEntries[i];
            Card card = new Card(entry.CardData, entry.IsUpgraded);
            BuildSelectionCardEntry(entry, card, pos, cardScaleVec);
        }
    }

    private void BuildSelectionCardEntry(DeckCardEntry entry, Card card, Vector3 localPos, Vector3 scale)
    {
        GameObject cardObj = Instantiate(cardPrefab, cardContainer);
        cardObj.transform.localPosition = localPos;
        cardObj.transform.localScale = scale;

        CardView cardView = cardObj.GetComponent<CardView>();
        if (cardView != null)
            cardView.SetUp(card);

        foreach (Collider col in cardObj.GetComponentsInChildren<Collider>(true))
            col.enabled = false;

        GameObject hitbox = new GameObject("Hitbox");
        hitbox.transform.SetParent(cardObj.transform, false);
        hitbox.transform.localPosition = Vector3.zero;
        hitbox.transform.localScale = Vector3.one;
        hitbox.layer = cardObj.layer;

        BoxCollider boxCol = hitbox.AddComponent<BoxCollider>();
        boxCol.size = new Vector3(hitboxWidth, hitboxHeight, 1f);

        CardGridHoverTrigger hoverTrigger = hitbox.AddComponent<CardGridHoverTrigger>();
        hoverTrigger.Init(card);

        CardGridClickHandler clickHandler = hitbox.AddComponent<CardGridClickHandler>();
        DeckCardEntry capturedEntry = entry;
        clickHandler.Init(() => EnterPreview(capturedEntry));
    }

    private void EnterPreview(DeckCardEntry entry)
    {
        isPreviewState = true;
        previewedEntry = entry;

        if (CardViewHoverSystem.Instance != null)
            CardViewHoverSystem.Instance.Hide();

        foreach (Transform child in cardContainer)
        {
            Destroy(child.gameObject);
        }

        Card previewCard = isUpgradePreview
            ? new Card(entry.CardData, isUpgraded: true)
            : new Card(entry.CardData, entry.IsUpgraded);

        Vector3 worldCenter = new Vector3(
            Camera.main.transform.position.x,
            Camera.main.transform.position.y, 0f);
        Vector3 centerPos = cardContainer.InverseTransformPoint(worldCenter);
        Vector3 previewScale = Vector3.one * previewCardScale;

        GameObject cardObj = Instantiate(cardPrefab, cardContainer);
        cardObj.transform.localPosition = centerPos;
        cardObj.transform.localScale = previewScale;

        CardView cardView = cardObj.GetComponent<CardView>();
        if (cardView != null)
            cardView.SetUp(previewCard);

        foreach (Collider col in cardObj.GetComponentsInChildren<Collider>(true))
            col.enabled = false;

        GameObject hitbox = new GameObject("Hitbox");
        hitbox.transform.SetParent(cardObj.transform, false);
        hitbox.transform.localPosition = Vector3.zero;
        hitbox.transform.localScale = Vector3.one;
        hitbox.layer = cardObj.layer;

        BoxCollider boxCol = hitbox.AddComponent<BoxCollider>();
        boxCol.size = new Vector3(hitboxWidth, hitboxHeight, 1f);

        CardGridClickHandler clickHandler = hitbox.AddComponent<CardGridClickHandler>();
        clickHandler.Init(ConfirmSelection);

        SpriteRenderer bg = cardView != null ? cardView.Background : null;
        if (bg != null)
        {
            HoverEffect3D hoverEffect = hitbox.AddComponent<HoverEffect3D>();
            hoverEffect.Init(cardObj.transform, new SpriteRenderer[] { bg });
        }

        scrollOffset = 0f;
        maxScrollOffset = 0f;
        cardContainer.localPosition = cardContainerBasePos;
    }

    private void ConfirmSelection()
    {
        var cb = selectionCallback;
        var entry = previewedEntry;

        if (isUpgradePreview && cardContainer != null && cardContainer.childCount > 0)
        {
            Transform previewCard = cardContainer.GetChild(0);
            StartCoroutine(UpgradeShakeCoroutine(previewCard, () =>
            {
                CloseSelectionMode();
                cb?.Invoke(entry);
            }));
            return;
        }

        CloseSelectionMode();
        cb?.Invoke(entry);
    }

    private IEnumerator UpgradeShakeCoroutine(Transform card, Action onComplete)
    {
        card.localRotation = Quaternion.Euler(0, 0, shakeAngle);
        yield return new WaitForSecondsRealtime(shakeInterval);
        card.localRotation = Quaternion.Euler(0, 0, -shakeAngle);
        yield return new WaitForSecondsRealtime(shakeInterval);
        card.localRotation = Quaternion.Euler(0, 0, shakeAngle);
        yield return new WaitForSecondsRealtime(shakeInterval);
        card.localRotation = Quaternion.Euler(0, 0, 0);
        onComplete?.Invoke();
    }

    private void CloseSelectionMode()
    {
        isSelectionMode = false;
        isPreviewState = false;
        isUpgradePreview = false;
        previewedEntry = null;
        selectionEntries = null;
        selectionCallback = null;
        cancelCallback = null;

        if (CardViewHoverSystem.Instance != null)
            CardViewHoverSystem.Instance.Hide();

        DestroyOverlay();
        RestoreSortingGroups();
        if (panelBackgroundImage != null) panelBackgroundImage.enabled = true;

        Time.timeScale = previousTimeScale;
        if (viewerPanel != null) viewerPanel.SetActive(false);
        if (cardContainer != null) cardContainer.gameObject.SetActive(false);
    }

}

public class CardGridClickHandler : MonoBehaviour
{
    private Action onClick;

    public void Init(Action callback)
    {
        onClick = callback;
    }

    void OnMouseDown()
    {
        onClick?.Invoke();
    }
}
