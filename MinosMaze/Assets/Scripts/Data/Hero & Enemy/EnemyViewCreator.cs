using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class EnemyViewCreator : Singleton<EnemyViewCreator>
{
    [SerializeField] private EnemyView enemyViewPrefab;

    public EnemyView CreateEnemyView(EnemyData enemyData, Vector3 position, Quaternion rotation, int hexX, int hexZ)
    {
        if (enemyViewPrefab == null)
        {
            Debug.LogError("[EnemyViewCreator] enemyViewPrefab 未设置");
            return null;
        }
        EnemyView enemyView = Instantiate(enemyViewPrefab, position, rotation);
        SceneManager.MoveGameObjectToScene(enemyView.gameObject, gameObject.scene);
        enemyView.Setup(enemyData, hexX, hexZ);
        return enemyView;
    }
    
}
