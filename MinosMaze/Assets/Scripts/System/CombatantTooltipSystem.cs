using UnityEngine;

public class CombatantTooltipSystem : Singleton<CombatantTooltipSystem>
{
    [SerializeField] private CombatantTooltipUI tooltipPrefab;

    [SerializeField] private float hoverScreenDistance = 120f;

    private CombatantTooltipUI tooltipInstance;
    private Camera camera3D;
    private CombatantView lastHovered;

    protected override void Awake()
    {
        base.Awake();
        if (tooltipPrefab != null)
        {
            tooltipInstance = Instantiate(tooltipPrefab, transform);
            tooltipInstance.gameObject.SetActive(false);
            Debug.Log($"[CombatantTooltipSystem] 已实例化 tooltip: {tooltipPrefab.name}");
        }
        else
        {
            Debug.LogError("[CombatantTooltipSystem] tooltipPrefab 未设置！请在 Inspector 中将 CombatantTooltipUI 预制体拖入 Tooltip Prefab 字段");
        }
    }

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
        if (tooltipInstance == null || camera3D == null)
            return;

        if (!Interactions.Instance.PlayerCanHover())
        {
            HideLastHovered();
            return;
        }

        CombatantView hovered = GetHoveredCombatant();

        if (hovered != null && hovered.GetStatusEffects().Count > 0)
        {
            if (hovered != lastHovered)
            {
                tooltipInstance.Populate(hovered);
                lastHovered = hovered;
            }
            tooltipInstance.Show();
            Vector3 worldPos = hovered.transform.position;
            Vector3 screenPos = camera3D.WorldToScreenPoint(worldPos);
            tooltipInstance.UpdatePosition(screenPos);
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
            tooltipInstance.Hide();
            lastHovered = null;
        }
    }
}
