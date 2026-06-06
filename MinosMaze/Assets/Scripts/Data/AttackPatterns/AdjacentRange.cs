using System.Collections.Generic;
using UnityEngine;

public class AdjacentRange : HexRangePattern
{
    private static readonly (int dx, int dz)[] Neighbors =
    {
        (1, 0), (-1, 0), (0, 1), (-1, 1), (1, -1), (0, -1)
    };

    public override List<Vector2Int> GetAffectedCells(Vector2Int origin, Vector2Int target)
    {
        var result = new List<Vector2Int> { target };
        foreach (var (dx, dz) in Neighbors)
        {
            int x = target.x + dx;
            int z = target.y + dz;
            if (HexGrid.ContainsCell(x, z))
                result.Add(new Vector2Int(x, z));
        }
        return result;
    }
}
