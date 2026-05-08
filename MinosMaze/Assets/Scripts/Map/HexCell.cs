using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HexCell : MonoBehaviour
{
    public int hexCoordX = 0;

    public int hexCoordZ = 0;

     public void SetCoord(int x, int z)
    {
        hexCoordX = x;
        hexCoordZ = z;
    }
}
