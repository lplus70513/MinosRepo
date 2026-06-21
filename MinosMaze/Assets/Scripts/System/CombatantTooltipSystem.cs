using UnityEngine;

public class CombatantTooltipSystem : Singleton<CombatantTooltipSystem>
{
    [SerializeField] private float hoverScreenDistance = 120f;

    private Camera camera3D;
    private CombatantView lastHovered;

    void Start()
    {
        var camObj = GameObject.FindGameObjectWithTag("3D Camera");
        if (camObj != null)
        {
            camera3D = camObj.GetComponent<Camera>();
            Debug.Log("[CombatantTooltipSystem] 已找到 3D Camera");
        }
        else
        {
            Debug.LogError("[CombatantTooltipSystem] 未找到标记为 '3D Camera' 的对象！");
        }
    }

    void Update()
    {
        if (camera3D == null) return;

        if (!Interactions.Instance.PlayerCanHover())
        {
            HideLastHovered();
            return;
        }

        CombatantView hovered = GetHoveredCombatant();

        if (hovered != null)
        {
            if (hovered != lastHovered)
            {
                HideLastHovered();
                hovered.ShowTooltip();
                lastHovered = hovered;
            }
        }
        else
        {
            HideLastHovered();
        }
    }

    private CombatantView GetHoveredCombatant()
    {
        CombatantView best = null;
        float bestDist = hoverScreenDistance;
        Vector2 mouseScreen = Input.mousePosition;

        var enemies = EnemySystem.Instance?.Enemies;
        if (enemies != null)
        {
            foreach (var enemy in enemies)
            {
                if (enemy == null) continue;
                Vector3 screenPos = camera3D.WorldToScreenPoint(enemy.transform.position);
                float dist = Vector2.Distance(mouseScreen, new Vector2(screenPos.x, screenPos.y));
                if (dist < bestDist)
                {
                    bestDist = dist;
                    best = enemy;
                }
            }
        }

        var hero = HeroSystem.Instance?.HeroView;
        if (hero != null)
        {
            Vector3 screenPos = camera3D.WorldToScreenPoint(hero.transform.position);
            float dist = Vector2.Distance(mouseScreen, new Vector2(screenPos.x, screenPos.y));
            if (dist < bestDist)
            {
                bestDist = dist;
                best = hero;
            }
        }

        return best;
    }

    private void HideLastHovered()
    {
        if (lastHovered != null)
        {
            lastHovered.HideTooltip();
            lastHovered = null;
        }
    }
}
