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

    /// <summary>
    /// 负责实例化预制体并注入数据
    /// </summary>
    private GameObject CreateCardVisual(CardData data)
    {
        if (cardPrefab == null)
        {
            Debug.LogError("Card Prefab 未分配！");
            return null;
        }

        // 实例化
        GameObject newCardGO = Instantiate(cardPrefab, spawnPoint.position, spawnPoint.rotation, transform);

        // 获取 CardView 组件并注入数据
        CardView cardView = newCardGO.GetComponent<CardView>();
        if (cardView != null)
        {
            Card cardModel = new Card(data); // 创建逻辑层数据
            cardView.SetUp(cardModel);       // 注入到视觉层
        }
        else
        {
            Debug.LogError("Card Prefab 上缺少 CardView 组件！");
        }

        return newCardGO;
    }

    private void UpdateCardZOrder()
    {
        // 遍历所有卡牌，根据其在列表中的顺序设置 Z 轴
        for (int i = 0; i < handCards.Count; i++)
        {
            var (cardGO, originalZ) = handCards[i];
            // Z 轴层级：索引越大，Z越小（越靠前）
            float newZ = originalZ - i * 0.01f;

            // 立即更新Z或者使用动画
            cardGO.transform.DOKill(); // 停止之前的移动动画，避免冲突
            cardGO.transform.DOMoveZ(newZ, 0.3f);
        }
    }

    private void UpdateCardPositions()
    {
        if (handCards.Count == 0) return;
        if (splineContainer == null) return;

        // 计算排布参数
        float cardSpacing = 1f / maxHandSize;
        // 居中算法：(总宽 - 当前占用宽) / 2
        float firstCardPosition = 0.5f - (handCards.Count - 1) * cardSpacing / 2;

        Spline spline = splineContainer.Splines[0];
        Vector3 cameraForward = mainCamera.transform.forward;

        for (int i = 0; i < handCards.Count; i++)
        {
            var (cardGO, originalZ) = handCards[i];
            float p = firstCardPosition + (i * cardSpacing);

            // 计算目标位置
            Vector3 position = spline.EvaluatePosition(p);
            // 保持原有的 Z 轴层级，而不是用 spline 上的 Z
            position.z = originalZ - i * 0.01f;

            // 计算旋转（面向摄像机，但根据曲线切线倾斜）
            Vector3 tangent = spline.EvaluateTangent(p);
            if (tangent.sqrMagnitude < 1e-8f) tangent = Vector3.right;

            Quaternion rotation = Quaternion.LookRotation(cameraForward, Vector3.Cross(cameraForward, tangent));

            // 应用动画
            cardGO.transform.DOKill();
            cardGO.transform.DOMove(position, 0.25f).SetEase(Ease.OutBack);
            cardGO.transform.DORotateQuaternion(rotation, 0.25f);
        }
    }
}