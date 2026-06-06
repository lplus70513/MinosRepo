using System.Collections.Generic;
using UnityEngine;

public class SingleCellRange : HexRangePattern
{
    public override List<Vector2Int> GetAffectedCells(Vector2Int origin, Vector2Int target)
    {
        return new List<Vector2Int> { target };
    }
}
