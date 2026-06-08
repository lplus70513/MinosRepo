using UnityEngine;

public static class MouseUtil
{
    public static Vector3 GetMousePositionInWorldSpace(float zValue = 0f)
    {
        Camera cam = Camera.main;
        if (cam == null) return Vector3.zero;
        Plane dragPlane = new(cam.transform.forward, new Vector3(0, 0, zValue));
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        if (dragPlane.Raycast(ray, out float distance))
        {
            return ray.GetPoint(distance);
        }
        return Vector3.zero;
    }
}
