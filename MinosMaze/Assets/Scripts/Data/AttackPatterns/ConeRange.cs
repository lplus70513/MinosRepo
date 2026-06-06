using System.Collections.Generic;
using UnityEngine;

public class ConeRange : HexRangePattern
{
    [SerializeField] private int range = 2;

    [SerializeField] private float angle = 60f;

    public override List<Vector2Int> GetAffectedCells(Vector2Int origin, Vector2Int target)
    {
        var result = new List<Vector2Int>();
        Vector3 originCube = AxialToCube(origin);
        Vector3 targetDir = (AxialToCube(target) - originCube).normalized;

        float halfAngleCos = Mathf.Cos(angle * 0.5f * Mathf.Deg2Rad);

        var coords = HexGrid.GetCoordsInRange(origin.x, origin.y, range);
        foreach (var (x, z) in coords)
        {
            if (x == origin.x && z == origin.y) continue;
            Vector3 cellDir = (AxialToCube(new Vector2Int(x, z)) - originCube).normalized;
            if (Vector3.Dot(targetDir, cellDir) >= halfAngleCos)
                result.Add(new Vector2Int(x, z));
        }
        return result;
    }
}
