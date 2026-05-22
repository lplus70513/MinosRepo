using UnityEngine;

// 大地图玩家视图：仅包含图标与生命值，无战斗UI（血条/护甲/动画/buff等）
// 始终面向摄像机（billboarding）
public class WorldMapPlayerView : MonoBehaviour
{
    [SerializeField] private SpriteRenderer spriteRenderer;

    public int HexCoordX { get; set; }
    public int HexCoordZ { get; set; }
    public int MaxHealth { get; set; }
    public int CurrentHealth { get; set; }

    private static Camera camera3D;

    void Awake()
    {
        if (camera3D == null)
        {
            var camObj = GameObject.FindGameObjectWithTag("3D Camera");
            if (camObj != null) camera3D = camObj.GetComponent<Camera>();
        }
    }

    public void Setup(int hexX, int hexZ, int maxHealth, int currentHealth)
    {
        HexCoordX = hexX;
        HexCoordZ = hexZ;
        MaxHealth = maxHealth;
        CurrentHealth = currentHealth;
    }

    void LateUpdate()
    {
        if (camera3D != null)
        {
            transform.rotation = camera3D.transform.rotation;
        }
    }
}
