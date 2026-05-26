using UnityEngine;
using UnityEngine.Events;

public class Clickable3D : MonoBehaviour
{
    public UnityEvent onClick;

    void OnMouseDown()
    {
        onClick?.Invoke();
    }
}
