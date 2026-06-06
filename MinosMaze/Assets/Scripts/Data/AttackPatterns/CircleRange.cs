using System.Collections.Generic;
using UnityEngine;

public class CircleRange : HexRangePattern
{
    [SerializeField] private int radius = 1;

    public override List<Vector2Int> GetAffectedCells(Vector2Int origin, Vector2Int target)
    {
        var result = new List<Vector2Int>();
        var coords = HexGrid.GetCoordsInRange(target.x, target.y, radius);
        foreach (var (x, z) in coords)
            result.Add(new Vector2Int(x, z));
        return result;
    }
}
