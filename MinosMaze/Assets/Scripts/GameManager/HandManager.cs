using UnityEngine;
using UnityEngine.Splines;
using System.Collections.Generic;
using DG.Tweening;

public class HandManager : MonoBehaviour
{
    [SerializeField] private int maxHandSize;

    [SerializeField] private GameObject cardPrefab;

    [SerializeField] private SplineContainer splineContainer;

    [SerializeField] private Transform spawnPoint;

    private List<(GameObject card, float originalZ)> handCards = new();

    private Camera mainCamera;

    private void Awake()
    {
        mainCamera = Camera.main;
        if (mainCamera == null)
            Debug.LogError("No main camera found! Please set a camera as 'MainCamera'.");
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
            DrawCard();
    }

    private void DrawCard()
    {
        if (handCards.Count >= maxHandSize)
            return;

        // 1. 创建新卡牌
        GameObject newCardGO = Instantiate(cardPrefab, spawnPoint.position, spawnPoint.rotation);

        // 2. 获取其初始 Z 坐标（用于后续恢复）
        float initialZ = newCardGO.transform.position.z;

        // 3. 添加到列表
        handCards.Add((newCardGO, initialZ));

        // 4. 更新所有卡牌的 Z 轴层级
        UpdateCardZOrder();

        // 5. 触发位置更新动画
        UpdateCardPositions();
    }

    private void UpdateCardZOrder()
    {
        // 遍历所有卡牌，根据其在列表中的顺序设置 Z 轴
        for (int i = 0; i < handCards.Count; i++)
        {
            var (cardGO, originalZ) = handCards[i];
            // Z 轴从 0 开始，越后面的卡牌 Z 越小（离屏幕更近）
            float newZ = originalZ - i * 0.01f; // 例如：第1张 Z=0, 第2张 Z=-0.01, 第3张 Z=-0.02...

            // 使用 DOTween 移动 Z 轴（可选：平滑过渡）
            cardGO.transform.DOKill(); // 停止之前的移动动画，避免冲突
            cardGO.transform.DOMoveZ(newZ, 0.1f); 
        }
    }

    private void UpdateCardPositions()
    {
        if (handCards.Count == 0) return;

        float cardSpacing = 1f / maxHandSize;
        float firstCardPosition = 0.5f - (handCards.Count - 1) * cardSpacing / 2;

        Spline spline = splineContainer.Splines[0];
        Vector3 cameraForward = mainCamera.transform.forward;

        for (int i = 0; i < handCards.Count; i++)
        {
            var (cardGO, originalZ) = handCards[i]; 
            float p = firstCardPosition + (i * cardSpacing);

            Vector3 position = spline.EvaluatePosition(p);
            // 保持原有的 Z 轴层级，而不是用 spline 上的 Z
            position.z = originalZ - i * 0.01f;

            Vector3 tangent = spline.EvaluateTangent(p);

            if (tangent.sqrMagnitude < 1e-8f)
            {
                tangent = Vector3.right;
            }

            // 使用 LookRotation 计算旋转
            Quaternion rotation = Quaternion.LookRotation(cameraForward, Vector3.Cross(cameraForward, tangent));

            // 应用动画（位置和旋转）
            cardGO.transform.DOKill(true); // 停止并完成之前的动画
            cardGO.transform.DOMove(position, 0.25f);
            cardGO.transform.DORotateQuaternion(rotation, 0.25f);
        }
    }
}