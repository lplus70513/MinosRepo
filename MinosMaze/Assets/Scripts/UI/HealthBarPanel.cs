using System;
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
    private Dictionary<CombatantView, Action> statusActionMap = new();
    private List<TMP_Text> enemyNameTexts = new();

    public void SetupBattle(HeroView hero, List<EnemyView> enemies)
    {
        ClearAll();

        if (hero != null)
        {
            hero.OnHealthChanged += OnCombatantHealthChanged;
            playerBar.Initialize(hero.MaxHealth, hero.CurrentHealth);
            barMap[hero] = playerBar;

            Action onStatus = () => OnCombatantStatusChanged(hero);
            hero.OnStatusChanged += onStatus;
            statusActionMap[hero] = onStatus;
            OnCombatantStatusChanged(hero);
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

            Action onStatus = () => OnCombatantStatusChanged(enemy);
            enemy.OnStatusChanged += onStatus;
            statusActionMap[enemy] = onStatus;
            OnCombatantStatusChanged(enemy);
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
            {
                kvp.Key.OnHealthChanged -= OnCombatantHealthChanged;
                if (statusActionMap.TryGetValue(kvp.Key, out var act))
                    kvp.Key.OnStatusChanged -= act;
            }
        }
        barMap.Clear();
        statusActionMap.Clear();
    }

    private void OnCombatantHealthChanged(CombatantView combatant, int newHealth)
    {
        if (combatant == null) return;
        if (!barMap.TryGetValue(combatant, out var bar)) return;

        bar.SetHealth(newHealth);
        if (newHealth <= 0 && combatant is EnemyView)
        {
            combatant.OnHealthChanged -= OnCombatantHealthChanged;
            if (statusActionMap.TryGetValue(combatant, out var act))
            {
                combatant.OnStatusChanged -= act;
                statusActionMap.Remove(combatant);
            }
        }
    }

    private void OnCombatantStatusChanged(CombatantView combatant)
    {
        if (combatant == null) return;
        if (!barMap.TryGetValue(combatant, out var bar)) return;

        int armor = combatant.GetStatusEffectStacks(StatusEffectType.ARMOR);
        bar.SetArmor(armor);
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
