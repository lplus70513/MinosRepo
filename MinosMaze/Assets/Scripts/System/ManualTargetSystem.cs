using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ManualTargetSystem : Singleton<ManualTargetSystem>
{
    [SerializeField] private ArrowView arrowView;

    private Camera camera3D;

    void Start()
    {
        var camObj = GameObject.FindGameObjectWithTag("3D Camera");
        if (camObj != null) camera3D = camObj.GetComponent<Camera>();
    }

    public void StartTargeting(Vector3 startPosition)
    {
        arrowView.gameObject.SetActive(true);
        arrowView.SetupArrow(startPosition);
    }

    public EnemyView EndTargeting()
    {
        if (camera3D == null) return null;

        Vector2 mouseScreen = Input.mousePosition;
        EnemyView best = null;
        float bestDist = 100f;
        foreach (var enemy in EnemySystem.Instance.Enemies)
        {
            if (enemy == null) continue;
            Vector3 screenPos = camera3D.WorldToScreenPoint(enemy.transform.position);
            Vector2 enemyScreen = new(screenPos.x, screenPos.y);
            float dist = Vector2.Distance(mouseScreen, enemyScreen);
            if (dist < bestDist)
            {
                bestDist = dist;
                best = enemy;
            }
        }
        return bestDist < 100f ? best : null;
    }

    public void StopTargeting()
    {
        arrowView.gameObject.SetActive(false);
    }
}
