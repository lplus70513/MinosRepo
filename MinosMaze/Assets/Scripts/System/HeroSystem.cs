using UnityEngine;

public class HeroSystem : Singleton<HeroSystem>
{
    [SerializeField] private HeroView heroViewPrefab;

    public HeroView HeroView { get; private set; }

    public void Setup(HeroData heroData, Vector2Int spawnCoord)
    {
        Vector3 pos = HexGrid.GetStandingPoint(spawnCoord.x, spawnCoord.y);
        HeroView = Instantiate(heroViewPrefab, pos, Quaternion.identity);
        HeroView.Setup(heroData, spawnCoord.x, spawnCoord.y);
    }
}
