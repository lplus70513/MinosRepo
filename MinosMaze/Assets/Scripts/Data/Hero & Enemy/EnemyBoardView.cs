using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class EnemyBoardView : MonoBehaviour
{
    [SerializeField] private Transform enemyParent;

    public List<EnemyView> EnemyViews { get; private set; } = new();

    public void AddEnemy(EnemyData enemyData, Vector3 position, Quaternion rotation)
    {
        EnemyView enemyView = EnemyViewCreator.Instance.CreateEnemyView(enemyData, position, rotation);
        enemyView.transform.parent = enemyParent;
        EnemyViews.Add(enemyView);
    }

    public IEnumerator RemoveEnemy(EnemyView enemyView)
    {
        EnemyViews.Remove(enemyView);
        Tween tween = enemyView.transform.DOScale(Vector3.zero, 0.25f);
        yield return tween.WaitForCompletion();
        Destroy(enemyView.gameObject);
    }
}
