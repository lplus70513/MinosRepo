using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public abstract class HexRangePattern
{
    public abstract List<Vector2Int> GetAffectedCells(Vector2Int origin, Vector2Int target);

    protected static Vector3 AxialToCube(Vector2Int axial)
    {
        int q = axial.x + axial.y;
        int r = -axial.y;
        int s = -axial.x;
        return new Vector3(q, r, s);
    }

    protected static Vector2Int CubeToAxial(Vector3 cube)
    {
        int sq = Mathf.RoundToInt(cube.x);
        int sr = Mathf.RoundToInt(cube.y);
        int ss = Mathf.RoundToInt(cube.z);
        float dq = Mathf.Abs(sq - cube.x);
        float dr = Mathf.Abs(sr - cube.y);
        float ds = Mathf.Abs(ss - cube.z);
        if (dq > dr && dq > ds)
            sq = -sr - ss;
        else if (dr > ds)
            sr = -sq - ss;
        else
            ss = -sq - sr;
        int x = -ss;
        int z = -sr;
        return new Vector2Int(x, z);
    }

    protected static List<Vector2Int> HexLine(Vector2Int a, Vector2Int b)
    {
        int N = HexGrid.HexDistance(a.x, a.y, b.x, b.y);
        var results = new List<Vector2Int>();
        Vector3 ca = AxialToCube(a);
        Vector3 cb = AxialToCube(b);
        float step = 1f / Mathf.Max(N, 1);
        for (int i = 0; i <= N; i++)
        {
            float t = step * i;
            Vector3 lerped = Vector3.Lerp(ca, cb, t);
            Vector2Int hex = CubeToAxial(lerped);
            if (!results.Contains(hex))
                results.Add(hex);
        }
        return results;
    }
}
