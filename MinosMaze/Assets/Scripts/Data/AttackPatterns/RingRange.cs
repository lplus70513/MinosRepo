using System.Collections.Generic;
using UnityEngine;

public class RingRange : HexRangePattern
{
    [SerializeField] private int maxRadius = 3;
    [SerializeField] private int minRadius = 1;

    public int MaxRadius => maxRadius;
    public int MinRadius => minRadius;

    public override List<Vector2Int> GetAffectedCells(Vector2Int origin, Vector2Int target)
    {
        var result = new List<Vector2Int>();
        var coords = HexGrid.GetCoordsInRange(origin.x, origin.y, maxRadius);
        foreach (var (x, z) in coords)
        {
            int dist = HexGrid.HexDistance(origin.x, origin.y, x, z);
            if (dist >= minRadius && dist <= maxRadius)
                result.Add(new Vector2Int(x, z));
        }
        return result;
    }
}
