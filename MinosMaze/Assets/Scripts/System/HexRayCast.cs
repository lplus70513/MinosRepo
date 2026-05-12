using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class HexRayCast : MonoBehaviour
{
    Ray screenRay;

    private RaycastHit hitInfo;

    private int mapLayerMask;

    public int HexCoordX;

    public int HexCoordZ;

    Camera myCamera;

    void Start()
    {
        Debug.Log($"成功加载HexRayCast");

        GameObject cameraObject = GameObject.FindGameObjectWithTag("3D Camera");

        myCamera = cameraObject.GetComponent<Camera>();

        int layerIndex = LayerMask.NameToLayer("MapLayer");

        mapLayerMask = 1 << layerIndex;
    }

    void Update()
    {
        screenRay = myCamera.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(screenRay,out hitInfo,1000f,mapLayerMask))
        {
            //执行光标悬停相关函数
            HoverObject();

            if (Input.GetMouseButtonDown(0))
            {
                ClickObject();
            }
        }
   
    }

    void HoverObject()
    {
        //Debug.Log($"成功执行HoverObject");
    }

    void ClickObject()
    {
        Debug.Log($"成功执行ClickObject");
        if (Physics.Raycast(screenRay,out hitInfo,1000f,mapLayerMask))
        {
            HexCell cell = hitInfo.collider.GetComponent<HexCell>();

            var (x, z) = cell.GetCoord();

            Debug.Log($"点击了六角格，坐标为: X={x}, Z={z}");

            HexCoordX = x;

            HexCoordZ = z;
        }
    }
}
