using System.Collections.Generic;
using UnityEngine;

public class HexGrid : MonoBehaviour
{
    public int mapRadius = 2;

    public GameObject hexPrefab;

    private static Dictionary<(int x, int z), HexCell> cellDict = new();

    void Awake()
    {
        cellDict.Clear();
        CreateHexagonMap();
    }

    void CreateHexagonMap()
    {
        Vector3 center = Vector3.zero;

        for (int z = -mapRadius; z <= mapRadius; z++)
        {
            for (int x = -mapRadius - Mathf.Min(z,0) ; x <= mapRadius - Mathf.Max(z,0); x++)
            {
                Vector3 position = center + GetHexWorldPosition(x, z);
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
        hexCell.SetCoord(x, z);
        cellDict[(x, z)] = hexCell;
    }

    public static Vector3 GetStandingPoint(int x, int z)
    {
        if (cellDict.TryGetValue((x, z), out HexCell cell) && cell.standingPoint != null)
            return cell.standingPoint.position;
        Debug.LogWarning($"[HexGrid] 未找到六角格 ({x}, {z}) 或 standingPoint 为空");
        return Vector3.zero;
    }
}
