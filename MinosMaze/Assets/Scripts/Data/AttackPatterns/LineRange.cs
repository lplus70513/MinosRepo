using System.Collections.Generic;
using UnityEngine;

public class LineRange : HexRangePattern
{
    [SerializeField] private int length = 2;

    public override List<Vector2Int> GetAffectedCells(Vector2Int origin, Vector2Int target)
    {
        var result = new List<Vector2Int>();
        var lineCells = HexLine(origin, target);
        for (int i = 1; i < lineCells.Count && i <= length; i++)
            result.Add(lineCells[i]);
        return result;
    }
}
