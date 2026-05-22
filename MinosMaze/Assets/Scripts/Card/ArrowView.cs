using System.Collections.Generic;
using UnityEngine;

public class ArrowView : MonoBehaviour
{
    [Header("控制点")]
    [SerializeField] private float reverseDistance = 40f;
    [SerializeField] private float forwardDistance = 20f;
    [SerializeField] private float curveIntensity = 1f;

    [Header("样式")]
    [SerializeField] private GameObject arrowHead;
    [SerializeField] private int rectCount = 12;
    [SerializeField] private float segmentWidth = 0.1f;
    [SerializeField] private float segmentHeight = 0.03f;
    [SerializeField] private Color arrowColor = Color.white;
    [SerializeField] private Sprite customSprite;

    [Header("行为")]
    [SerializeField] private float minDrawDistance = 10f;

    private Vector3 startPosition;
    private Camera mainCamera;
    private List<GameObject> rects = new List<GameObject>();

    private void Awake()
    {
        mainCamera = Camera.main;
        CreateRectPool();
    }

    private void CreateRectPool()
    {
        Sprite sprite = customSprite;
        if (sprite == null)
        {
            int texSize = 8;
            Texture2D tex = new Texture2D(texSize, texSize, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            Color[] pixels = new Color[texSize * texSize];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = Color.white;
            tex.SetPixels(pixels);
            tex.Apply();
            sprite = Sprite.Create(tex, new Rect(0, 0, texSize, texSize), new Vector2(0.5f, 0.5f), texSize);
        }

        for (int i = 0; i < rectCount; i++)
        {
            GameObject rect = new GameObject("Seg_" + i);
            rect.transform.SetParent(transform, false);
            SpriteRenderer sr = rect.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = arrowColor;
            rect.transform.localScale = new Vector3(segmentWidth, segmentHeight, 1f);
            rect.SetActive(false);
            rects.Add(rect);
        }
    }

    private void Update()
    {
        Vector3 endPosition = MouseUtil.GetMousePositionInWorldSpace(startPosition.z);

        Vector3 p0Screen = mainCamera.WorldToScreenPoint(startPosition);
        Vector3 p3Screen = mainCamera.WorldToScreenPoint(endPosition);
        float screenDist = Vector2.Distance(
            new Vector2(p0Screen.x, p0Screen.y),
            new Vector2(p3Screen.x, p3Screen.y));

        bool shouldShow = screenDist >= minDrawDistance;

        foreach (var r in rects)
            r.SetActive(shouldShow);
        if (arrowHead != null)
            arrowHead.SetActive(shouldShow);

        if (!shouldShow) return;

        DrawBezier(startPosition, endPosition);
        UpdateArrowHead(endPosition);
    }

    public void SetupArrow(Vector3 startPosition)
    {
        this.startPosition = startPosition;
    }

    public void Hide()
    {
        foreach (var r in rects)
            r.SetActive(false);
        if (arrowHead != null)
            arrowHead.SetActive(false);
    }

    private void DrawBezier(Vector3 p0, Vector3 p3)
    {
        CalculateControlPoints(p0, p3, out Vector3 p1, out Vector3 p2);

        for (int i = 0; i < rectCount; i++)
        {
            float t = (i + 0.5f) / rectCount;
            Vector3 pos = CubicBezier(p0, p1, p2, p3, t);
            Vector3 tangent = CubicBezierTangent(p0, p1, p2, p3, t);

            rects[i].transform.position = pos;
            if (tangent.sqrMagnitude > 1e-8f)
                rects[i].transform.up = tangent.normalized;
        }
    }

    private void UpdateArrowHead(Vector3 endPosition)
    {
        arrowHead.transform.position = endPosition;

        CalculateControlPoints(startPosition, endPosition, out Vector3 p1, out Vector3 p2);
        Vector3 tangentEnd = CubicBezierTangent(startPosition, p1, p2, endPosition, 1f);

        if (tangentEnd.sqrMagnitude > 1e-8f)
            arrowHead.transform.up = tangentEnd.normalized;
    }

    private void CalculateControlPoints(Vector3 p0, Vector3 p3, out Vector3 p1, out Vector3 p2)
    {
        Vector3 p0Screen = mainCamera.WorldToScreenPoint(p0);
        Vector3 p3Screen = mainCamera.WorldToScreenPoint(p3);

        float dx = p3Screen.x - p0Screen.x;
        float dy = p3Screen.y - p0Screen.y;
        float len = Mathf.Sqrt(dx * dx + dy * dy);
        if (len < 1f) { dx = 0f; dy = 1f; len = 1f; }
        float nx = dx / len;
        float ny = dy / len;

        p1 = mainCamera.ScreenToWorldPoint(new Vector3(
            p0Screen.x - nx * reverseDistance * curveIntensity,
            p0Screen.y - ny * reverseDistance * curveIntensity,
            p0Screen.z));

        p2 = mainCamera.ScreenToWorldPoint(new Vector3(
            p3Screen.x - nx * forwardDistance * curveIntensity,
            p3Screen.y - ny * forwardDistance * curveIntensity,
            p3Screen.z));
    }

    private static Vector3 CubicBezier(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
    {
        float u = 1f - t;
        float uu = u * u;
        float uuu = uu * u;
        float tt = t * t;
        float ttt = tt * t;
        return uuu * p0 + 3f * uu * t * p1 + 3f * u * tt * p2 + ttt * p3;
    }

    private static Vector3 CubicBezierTangent(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
    {
        float u = 1f - t;
        return 3f * u * u * (p1 - p0) + 6f * u * t * (p2 - p1) + 3f * t * t * (p3 - p2);
    }
}