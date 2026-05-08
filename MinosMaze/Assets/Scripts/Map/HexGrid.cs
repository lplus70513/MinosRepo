using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;

public class HexGrid : MonoBehaviour
{
    public int mapRadius = 2;

    public GameObject hexPrefab;

    void Awake()
    {
        CreateHexagonMap();
    }

    void CreateHexagonMap()
    {
        Vector3 center = Vector3.zero;

        for (int z = -mapRadius; z <= mapRadius; z++)
        {
            // x 的起始和结束位置取决于当前的 z 值，以保证形状是六边形
            for (int x = -mapRadius - Mathf.Min(z,0) ; x <= mapRadius - Mathf.Max(z,0); x++)
            {
                // 计算该六边形在世界空间中的位置
                Vector3 position = center + GetHexWorldPosition(x, z);
                
                // 实例化单个六边形格
                CreateHexCell(position, x, z);
            }
        }
    }

    Vector3 GetHexWorldPosition(int x, int z)
    {
        Vector3 position;
        position.x = (2*x + z) * HexMetrics.innerRadius;
        position.y = 0f;
        position.z = -z * HexMetrics.outerRadius * 1.5f ;
        return position;
    }

      void CreateHexCell(Vector3 position, int x, int z)
    {
        GameObject hexCellObject = Instantiate(hexPrefab, position, Quaternion.Euler(0, 90, 0), transform);
        HexCell hexCell = hexCellObject.GetComponent<HexCell>();

        // 传递轴向坐标系中的坐标
        hexCell.SetCoord(x, z);
    }

}
