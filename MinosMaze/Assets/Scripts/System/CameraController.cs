using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraController : MonoBehaviour
{
    [Header("移动设置")]
    [SerializeField] private float moveSpeed = 25f;
    [SerializeField] private float zoomSpeed = 30f;

    [Header("边界钳制")]
    [SerializeField] private bool enableClamp = true;
    [SerializeField] private Vector2 xClamp = new Vector2(-80f, 45f);
    [SerializeField] private Vector2 zClamp = new Vector2(-35f, 35f);
    [SerializeField] private Vector2 yClamp = new Vector2(15f, 80f);

    private Camera cam;

    private void Start()
    {
        cam = GetComponent<Camera>();
    }

    private void Update()
    {
        HandleMovement();
        HandleZoom();
    }

    private void HandleMovement()
    {
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        if (Mathf.Approximately(horizontal, 0f) && Mathf.Approximately(vertical, 0f))
            return;

        Vector3 screenUp = Vector3.ProjectOnPlane(transform.up, Vector3.up).normalized;
        Vector3 screenRight = Vector3.ProjectOnPlane(transform.right, Vector3.up).normalized;

        Vector3 moveDir = screenUp * vertical + screenRight * horizontal;
        Vector3 newPos = transform.position + moveDir * (moveSpeed * Time.deltaTime);

        transform.position = ClampPosition(newPos);
    }

    private void HandleZoom()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Approximately(scroll, 0f))
            return;

        Vector3 newPos = transform.position + transform.forward * (scroll * zoomSpeed);
        transform.position = ClampPosition(newPos);
    }

    private Vector3 ClampPosition(Vector3 pos)
    {
        if (!enableClamp)
            return pos;

        pos.x = Mathf.Clamp(pos.x, xClamp.x, xClamp.y);
        pos.y = Mathf.Clamp(pos.y, yClamp.x, yClamp.y);
        pos.z = Mathf.Clamp(pos.z, zClamp.x, zClamp.y);
        return pos;
    }
}
