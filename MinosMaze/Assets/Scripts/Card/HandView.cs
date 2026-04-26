using UnityEngine;
using UnityEngine.Splines;
using System.Collections.Generic;
using DG.Tweening;

public class HandView : MonoBehaviour
{
    [Header("配置参数")]
    [SerializeField] private int maxHandSize = 5;
    [SerializeField] private GameObject cardPrefab;
    [SerializeField] private SplineContainer splineContainer;
    [SerializeField] private Transform spawnPoint;

    [Header("测试数据 (在编辑器拖入CardData)")]
    // 这是一个用于测试的列表，你可以在Inspector里拖入多个CardData资产
    [SerializeField] private List<CardData> dataForTesting = new List<CardData>();

    // 存储当前手牌的视觉对象和原始Z轴
    private List<(GameObject card, float originalZ)> handCards = new List<(GameObject, float)>();

    // 用于追踪当前使用了测试列表中的第几个数据
    private int testDataIndex = 0;

    private Camera mainCamera;

    private void Awake()
    {
        mainCamera = Camera.main;
    }

    private void Update()
    {
        // 按下空格发牌
        if (Input.GetKeyDown(KeyCode.Space))
        {
            DrawCard();
        }
    }

    private void DrawCard()
    {
        // 1. 检查手牌上限
        if (handCards.Count >= maxHandSize)
        {
            Debug.Log("手牌已满！");
            return;
        }

        // 2. 检查是否有测试数据
        if (dataForTesting == null || dataForTesting.Count == 0)
        {
            Debug.LogWarning("请在 Inspector 中为 HandView 添加 CardData 测试数据！");
            return;
        }

        // 3. 获取当前要生成的数据 (循环使用测试数据)
        CardData currentData = dataForTesting[testDataIndex % dataForTesting.Count];
        testDataIndex++;

        // 4. 创建卡牌视觉对象并注入数据
        GameObject newCardGO = CreateCardVisual(currentData);

        if (newCardGO != null)
        {
            // 5. 记录数据
            float initialZ = newCardGO.transform.position.z;
            handCards.Add((newCardGO, initialZ));

            // 6. 刷新层级 (Z轴排序)
            UpdateCardZOrder();

            // 7. 刷新布局动画
            UpdateCardPositions();
        }
    }

    // 实例化预制体
    private GameObject CreateCardVisual(CardData data)
    {
        GameObject newCardGO = Instantiate(cardPrefab, spawnPoint.position, spawnPoint.rotation, transform);

        CardView cardView = newCardGO.GetComponent<CardView>();
        if (cardView != null)
        {
            Card cardModel = new Card(data); 
            cardView.SetUp(cardModel);       
        }
        return newCardGO;
    }

    private void UpdateCardZOrder()
    {
        for (int i = 0; i < handCards.Count; i++)
        {
            var (cardGO, originalZ) = handCards[i];

            float newZ = originalZ - i * 0.01f;

            cardGO.transform.DOKill(); 
            cardGO.transform.DOMoveZ(newZ, 0.3f);
        }
    }

    private void UpdateCardPositions()
    {
        if (handCards.Count == 0) return;
        if (splineContainer == null) return;

        float cardSpacing = 1f / maxHandSize;

        float firstCardPosition = 0.5f - (handCards.Count - 1) * cardSpacing / 2;

        Spline spline = splineContainer.Splines[0];
        Vector3 cameraForward = mainCamera.transform.forward;

        for (int i = 0; i < handCards.Count; i++)
        {
            var (cardGO, originalZ) = handCards[i];
            float p = firstCardPosition + (i * cardSpacing);

            Vector3 position = spline.EvaluatePosition(p);
            position.z = originalZ - i * 0.01f;

            Vector3 tangent = spline.EvaluateTangent(p);
            if (tangent.sqrMagnitude < 1e-8f) tangent = Vector3.right;

            Quaternion rotation = Quaternion.LookRotation(cameraForward, Vector3.Cross(cameraForward, tangent));

            cardGO.transform.DOKill();
            cardGO.transform.DOMove(position, 0.5f).SetEase(Ease.OutBack, 0.45f);
            cardGO.transform.DORotateQuaternion(rotation, 0.5f);
        }
    }
}