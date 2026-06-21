using System;
using System.Collections;
using UnityEngine;
using DG.Tweening;

public class CardSelectOverlay : MonoBehaviour
{
    [Header("遮罩")]
    [SerializeField] private float overlayAlpha = 0.5f;
    [SerializeField] private int overlaySortingOrder = 100;

    [Header("选中动画")]
    [SerializeField] private float selectAnimDuration = 0.4f;
    [SerializeField] private float selectedScale = 1.3f;
    [SerializeField] private Color outlineColor = Color.white;
    [SerializeField] private float outlineWidth = 3f;

    [Header("确认动画")]
    [SerializeField] private float confirmAnimDuration = 0.2f;

    private GameObject overlayObject;
    private Sprite overlaySprite;

    private bool isSelecting;
    private CardView selectedCardView;
    private Card selectedCard;
    private Card confirmedCard;
    private bool completed;

    private Transform drawPilePoint;
    private HandView handView;

    private Material[] selectedOriginalRenderMaterials;
    private Material[] selectedOutlineMaterials;
    private Renderer[] selectedRenderers;

    public bool IsSelecting => isSelecting;
    public Card ConfirmedCard => confirmedCard;
    public bool IsCompleted => completed;

    public event Action<Card> OnConfirmed;
    public event Action OnCancelled;

    void Awake()
    {
        CreateOverlaySprite();
    }

    void Update()
    {
        if (!isSelecting) return;

        if (Input.GetMouseButtonDown(1))
        {
            if (selectedCardView != null)
                DeselectCurrentCard();
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            AbortSelection();
        }
    }

    void OnDestroy()
    {
        DestroyOverlay();
        if (overlaySprite != null)
            Destroy(overlaySprite);
        CleanupOutlineMaterials();
    }

    private void CreateOverlaySprite()
    {
        Texture2D tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();
        overlaySprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f));
    }

    public void StartSelection(Transform drawPilePoint, HandView handView)
    {
        this.drawPilePoint = drawPilePoint;
        this.handView = handView;

        isSelecting = true;
        completed = false;
        selectedCardView = null;
        selectedCard = null;
        confirmedCard = null;

        CreateOverlay();
    }

    public void OnCardLeftClicked(CardView cardView)
    {
        if (!isSelecting) return;

        if (selectedCardView == cardView)
        {
            ConfirmSelection();
        }
        else if (selectedCardView != null)
        {
            DeselectCurrentCard();
            SelectCard(cardView);
        }
        else
        {
            SelectCard(cardView);
        }
    }

    private void SelectCard(CardView cardView)
    {
        selectedCardView = cardView;
        selectedCard = cardView.Card;

        handView.OnCardDragStarted(cardView);
        cardView.BringToFront();
        ApplyOutline(cardView);

        Vector3 targetPos = GetScreenCenterPosition();
        Transform t = cardView.transform;
        t.DOKill();
        t.DOMove(targetPos, selectAnimDuration).SetEase(Ease.OutBack, 0.7f);
        t.DOScale(Vector3.one * selectedScale, selectAnimDuration);
    }

    private Vector3 GetScreenCenterPosition()
    {
        Camera cam = Camera.main;
        if (cam == null) return Vector3.zero;

        Vector3 worldCenter = cam.ViewportToWorldPoint(new Vector3(0.5f, 0.5f, 10f));
        if (handView != null)
        {
            Vector3 handCenter = handView.GetHandCenterPosition();
            worldCenter.z = handCenter.z;
        }
        return worldCenter;
    }

    private void ConfirmSelection()
    {
        if (selectedCardView == null) return;

        confirmedCard = selectedCard;
        isSelecting = false;
        completed = true;

        StartCoroutine(ConfirmAnimationSequence());
    }

    private IEnumerator ConfirmAnimationSequence()
    {
        CardView cardView = selectedCardView;
        Transform t = cardView.transform;
        t.DOKill();
        t.DOScale(Vector3.zero, confirmAnimDuration).SetEase(Ease.InQuad);

        Vector3 targetPos = drawPilePoint != null
            ? drawPilePoint.position
            : t.position + Vector3.down * 3f;
        Tween moveTween = t.DOMove(targetPos, confirmAnimDuration);

        yield return moveTween.WaitForCompletion();

        RemoveOutline(cardView);

        handView.RemoveCard(selectedCard);
        Destroy(cardView.gameObject);

        selectedCardView = null;
        selectedCard = null;

        DestroyOverlay();

        OnConfirmed?.Invoke(confirmedCard);
    }

    private void DeselectCurrentCard()
    {
        if (selectedCardView == null) return;

        RemoveOutline(selectedCardView);
        selectedCardView.RestoreSortingOrder();

        handView.OnCardDragEnded(selectedCardView);

        selectedCardView.transform.DOScale(Vector3.one, selectAnimDuration * 0.5f);

        selectedCardView = null;
        selectedCard = null;
    }

    public void CancelSelection()
    {
        if (!isSelecting) return;

        isSelecting = false;
        completed = true;

        if (selectedCardView != null)
        {
            RemoveOutline(selectedCardView);
            selectedCardView.RestoreSortingOrder();

            handView.OnCardDragEnded(selectedCardView);

            selectedCardView.transform.DOScale(Vector3.one, selectAnimDuration * 0.5f);

            selectedCardView = null;
            selectedCard = null;
        }

        DestroyOverlay();

        OnCancelled?.Invoke();
    }

    private void AbortSelection()
    {
        CancelSelection();
    }

    private void CreateOverlay()
    {
        if (overlayObject != null) return;

        overlayObject = new GameObject("CardSelectOverlaySprite");
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
    }

    private void DestroyOverlay()
    {
        if (overlayObject != null)
        {
            Destroy(overlayObject);
            overlayObject = null;
        }
    }

    private void ApplyOutline(CardView cardView)
    {
        Shader outlineShader = Shader.Find("Custom/SpriteOutline");
        if (outlineShader == null)
        {
            Debug.LogWarning("[CardSelectOverlay] 未找到 Custom/SpriteOutline Shader，跳过描边");
            return;
        }

        selectedRenderers = cardView.GetComponentsInChildren<Renderer>(true);
        selectedOriginalRenderMaterials = new Material[selectedRenderers.Length];
        selectedOutlineMaterials = new Material[selectedRenderers.Length];

        for (int i = 0; i < selectedRenderers.Length; i++)
        {
            selectedOriginalRenderMaterials[i] = selectedRenderers[i].sharedMaterial;
            Material mat = new Material(outlineShader);
            mat.CopyPropertiesFromMaterial(selectedOriginalRenderMaterials[i]);
            mat.SetColor("_OutlineColor", outlineColor);
            mat.SetFloat("_OutlineWidth", outlineWidth);
            mat.SetFloat("_EnableOutline", 1f);
            selectedOutlineMaterials[i] = mat;
            selectedRenderers[i].material = mat;
        }
    }

    private void RemoveOutline(CardView cardView)
    {
        if (selectedRenderers == null || selectedOriginalRenderMaterials == null) return;

        for (int i = 0; i < selectedRenderers.Length; i++)
        {
            if (selectedRenderers[i] != null)
                selectedRenderers[i].sharedMaterial = selectedOriginalRenderMaterials[i];
        }

        CleanupOutlineMaterials();
    }

    private void CleanupOutlineMaterials()
    {
        if (selectedOutlineMaterials != null)
        {
            for (int i = 0; i < selectedOutlineMaterials.Length; i++)
            {
                if (selectedOutlineMaterials[i] != null)
                    Destroy(selectedOutlineMaterials[i]);
            }
            selectedOutlineMaterials = null;
        }
        selectedRenderers = null;
        selectedOriginalRenderMaterials = null;
    }
}
