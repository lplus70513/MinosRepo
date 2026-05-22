using UnityEngine;

// 大地图玩家实例管理器
public class WorldMapPlayerSystem : Singleton<WorldMapPlayerSystem>
{
    [SerializeField] private WorldMapPlayerView playerPrefab;

    public WorldMapPlayerView PlayerView { get; private set; }

    public void Setup(Vector2Int coord, int maxHealth, int currentHealth)
    {
        Vector3 pos = HexGrid.GetStandingPoint(coord.x, coord.y);
        PlayerView = Instantiate(playerPrefab, pos, Quaternion.identity);
        PlayerView.Setup(coord.x, coord.y, maxHealth, currentHealth);
    }
}
