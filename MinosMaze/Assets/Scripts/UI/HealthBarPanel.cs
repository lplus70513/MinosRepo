using System.Collections.Generic;
using UnityEngine;

public class HealthBarPanel : MonoBehaviour
{
    [SerializeField] private HealthBarUI playerBar;
    [SerializeField] private HealthBarUI enemyBarPrefab;
    [SerializeField] private RectTransform enemyBarContainer;
    [SerializeField] private Vector3 enemyBarScale = Vector3.one;

    private Dictionary<CombatantView, HealthBarUI> barMap = new();

    public void SetupBattle(HeroView hero, List<EnemyView> enemies)
    {
        ClearAll();

        if (hero != null)
        {
            hero.OnHealthChanged += OnCombatantHealthChanged;
            playerBar.Initialize(hero.MaxHealth, hero.CurrentHealth);
            barMap[hero] = playerBar;
        }

        foreach (var enemy in enemies)
        {
            enemy.OnHealthChanged += OnCombatantHealthChanged;
            var bar = Instantiate(enemyBarPrefab, enemyBarContainer);
            bar.Initialize(enemy.MaxHealth, enemy.CurrentHealth);
            bar.transform.localScale = enemyBarScale;
            barMap[enemy] = bar;
        }
    }

    private void ClearAll()
    {
        foreach (Transform child in enemyBarContainer)
        {
            Destroy(child.gameObject);
        }
        foreach (var kvp in barMap)
        {
            if (kvp.Key != null)
                kvp.Key.OnHealthChanged -= OnCombatantHealthChanged;
        }
        barMap.Clear();
    }

    private void OnCombatantHealthChanged(CombatantView combatant, int newHealth)
    {
        if (combatant == null) return;
        if (!barMap.TryGetValue(combatant, out var bar)) return;

        bar.SetHealth(newHealth);
        if (newHealth <= 0 && combatant is EnemyView)
        {
            combatant.OnHealthChanged -= OnCombatantHealthChanged;
        }
    }

    private void OnDestroy()
    {
        ClearAll();
    }
}
