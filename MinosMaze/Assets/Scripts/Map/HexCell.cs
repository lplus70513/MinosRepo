using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HexCell : MonoBehaviour
{
    public int hexCoordX = 0;

    public int hexCoordZ = 0;

    public Transform standingPoint;

    public void SetCoord(int x, int z)
    {
        hexCoordX = x;
        hexCoordZ = z;
    }

    public (int x, int z) GetCoord()
    {
        return (hexCoordX, hexCoordZ);
    }
}
