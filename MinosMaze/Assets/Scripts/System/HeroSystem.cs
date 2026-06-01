using UnityEngine;

public class HeroSystem : Singleton<HeroSystem>
{
    [SerializeField] private HeroView heroViewPrefab;

    public HeroView HeroView { get; private set; }

    public void Setup(HeroData heroData, Vector2Int spawnCoord)
    {
        if (heroViewPrefab == null)
        {
            Debug.LogError("[HeroSystem] heroViewPrefab 未设置，请在战斗场景中配置 HeroSystem 的引用");
            return;
        }
        Vector3 pos = HexGrid.GetStandingPoint(spawnCoord.x, spawnCoord.y);
        HeroView = Instantiate(heroViewPrefab, pos, Quaternion.identity);
        HeroView.Setup(heroData, spawnCoord.x, spawnCoord.y);
    }
}
