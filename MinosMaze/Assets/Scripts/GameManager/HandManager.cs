using UnityEngine.Splines;
using UnityEngine;
using DG.Tweening;
using System.Collections.Generic;

public class HandManager : MonoBehaviour
{
    [SerializeField] private int maxHandSize;

    [SerializeField] private GameObject cardPrefab;

    [SerializeField] private SplineContainer splineContainer;

    [SerializeField] private Transform spawnPoint;

    private List<GameObject> handCards = new();
    private Camera mainCamera;

    private void Awake()
    {
        mainCamera = Camera.main;
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
        GameObject g = Instantiate(cardPrefab, spawnPoint.position, spawnPoint.rotation);
        handCards.Add(g);
        UpdateCardPositions();
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
            float p = firstCardPosition + (i * cardSpacing);

            Vector3 position = spline.EvaluatePosition(p);
            Vector3 tangent = spline.EvaluateTangent(p); 

            if (tangent.sqrMagnitude < 1e-8f)
            {
                tangent = Vector3.right;
            }

            Quaternion rotation = Quaternion.LookRotation(cameraForward, Vector3.Cross(cameraForward, tangent));

            handCards[i].transform.DOMove(position, 0.25f);
            handCards[i].transform.DORotateQuaternion(rotation, 0.25f);
        }
    }
}