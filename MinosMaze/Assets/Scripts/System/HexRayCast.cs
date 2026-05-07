using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class HexRayCast : MonoBehaviour
{
    Ray screenRay;

    private RaycastHit hitinfo;
    void Update()
    {
        screenRay = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(screenRay,out hitinfo))
        {
            //执行光标移动相关函数
            if (Input.GetMouseButtonDown(0))
            {
                ClickObject();
            }
        }
   
    }

    void ClickObject()
    {
        
    }
}
