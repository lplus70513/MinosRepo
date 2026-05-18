using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ManualTargetSystem : Singleton<ManualTargetSystem>
{
    [SerializeField] private ArrowView arrowView;

    public void StartTargeting(Vector3 startPosition)
    {
        arrowView.gameObject.SetActive(true);
        arrowView.SetupArrow(startPosition);
    }

    public EnemyView EndTargeting(Vector3 endPosition)
    {
        var hits = Physics.RaycastAll(endPosition, Vector3.forward, 10f);
        foreach (var hit in hits)
        {
            if (hit.collider != null && hit.transform.TryGetComponent(out EnemyView enemyView))
            {
                return enemyView;
            }
        }
        return null;
    }

    public void StopTargeting()
    {
        arrowView.gameObject.SetActive(false);
    }
}
