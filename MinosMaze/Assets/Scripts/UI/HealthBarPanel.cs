using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HealthBarPanel : MonoBehaviour
{
    [SerializeField] private HealthBarUI playerBar;
    [SerializeField] private HealthBarUI enemyBarPrefab;
    [SerializeField] private RectTransform enemyBarContainer;
    [SerializeField] private Vector3 enemyBarScale = Vector3.one;
    [SerializeField] private TMP_Text enemyNameTextPrefab;

    private Dictionary<CombatantView, HealthBarUI> barMap = new();
    private List<TMP_Text> enemyNameTexts = new();

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

            var wrapper = new GameObject("EnemyEntry", typeof(RectTransform), typeof(VerticalLayoutGroup));
            wrapper.transform.SetParent(enemyBarContainer, false);
            var layout = wrapper.GetComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.UpperRight;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            layout.spacing = 2;

            var nameText = Instantiate(enemyNameTextPrefab, wrapper.transform);
            nameText.text = enemy.DisplayName;
            enemyNameTexts.Add(nameText);

            var bar = Instantiate(enemyBarPrefab, wrapper.transform);
            bar.Initialize(enemy.MaxHealth, enemy.CurrentHealth);
            bar.transform.localScale = enemyBarScale;
            barMap[enemy] = bar;
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(enemyBarContainer);
        foreach (var nameText in enemyNameTexts)
        {
            nameText.transform.SetParent(playerBar.transform, true);
        }
    }

    private void ClearAll()
    {
        foreach (var nameText in enemyNameTexts)
        {
            if (nameText != null) Destroy(nameText.gameObject);
        }
        enemyNameTexts.Clear();

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

    public void SetupWorldMap(int maxHp, int curHp)
    {
        playerBar.Initialize(maxHp, curHp);
    }
}
