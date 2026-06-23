using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class HealthBarPanel : MonoBehaviour
{
    [SerializeField] private HealthBarUI playerBar;
    [SerializeField] private HealthBarUI enemyBarPrefab;
    [SerializeField] private RectTransform enemyBarContainer;
    [SerializeField] private Vector3 enemyBarScale = Vector3.one;
    [SerializeField] private TMP_Text enemyNameTextPrefab;

    private Dictionary<CombatantView, HealthBarUI> barMap = new();
    private Dictionary<CombatantView, Action> statusActionMap = new();
    private Vector2? enemyBarBasePos;

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

            var bar = Instantiate(enemyBarPrefab, wrapper.transform);
            bar.Initialize(enemy.MaxHealth, enemy.CurrentHealth);
            bar.transform.localScale = enemyBarScale;
            barMap[enemy] = bar;

            Action onStatus = () => OnCombatantStatusChanged(enemy);
            enemy.OnStatusChanged += onStatus;
            statusActionMap[enemy] = onStatus;
            OnCombatantStatusChanged(enemy);
        }

        if (!enemyBarBasePos.HasValue)
            enemyBarBasePos = enemyBarContainer.anchoredPosition;

        enemyBarContainer.anchoredPosition = enemyBarBasePos.Value + new Vector2(0, 50);

        LayoutRebuilder.ForceRebuildLayoutImmediate(enemyBarContainer);
        AdjustEnemyNamePositions();
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
        if (newHealth <= 0 && combatant is EnemyView enemy)
        {
            bool willRevive = enemy.SourceData != null && enemy.SourceData.CanRevive && !enemy.HasRevived;
            if (willRevive) return;

            combatant.OnHealthChanged -= OnCombatantHealthChanged;
            if (statusActionMap.TryGetValue(combatant, out var act))
            {
                combatant.OnStatusChanged -= act;
                statusActionMap.Remove(combatant);
            }
            barMap.Remove(combatant);
            StartCoroutine(FadeOutAndRemoveEntry(bar));
        }
    }

    private void OnCombatantStatusChanged(CombatantView combatant)
    {
        if (combatant == null) return;
        if (!barMap.TryGetValue(combatant, out var bar)) return;

        int armor = combatant.GetStatusEffectStacks(StatusEffectType.ARMOR);
        bar.SetArmor(armor);
    }

    private void AdjustEnemyNamePositions()
    {
        foreach (Transform wrapper in enemyBarContainer)
        {
            if (wrapper.childCount >= 2)
            {
                var nameRT = wrapper.GetChild(0) as RectTransform;
                if (nameRT != null)
                    nameRT.anchoredPosition += new Vector2(0, -50);
            }
        }
    }

    private IEnumerator FadeOutAndRemoveEntry(HealthBarUI bar)
    {
        if (bar == null) yield break;

        Transform wrapper = bar.transform.parent;
        if (wrapper == null) yield break;

        CanvasGroup cg = wrapper.GetComponent<CanvasGroup>();
        if (cg == null) cg = wrapper.gameObject.AddComponent<CanvasGroup>();

        cg.DOFade(0f, 0.2f);
        yield return new WaitForSeconds(0.2f);

        if (wrapper == null) yield break;

        var siblings = new List<RectTransform>();
        var oldPositions = new List<Vector2>();
        foreach (Transform child in enemyBarContainer)
        {
            if (child == null || child == wrapper) continue;
            var rt = child as RectTransform;
            if (rt != null)
            {
                siblings.Add(rt);
                oldPositions.Add(rt.anchoredPosition);
            }
        }

        wrapper.SetParent(null);
        Destroy(wrapper.gameObject);

        LayoutRebuilder.ForceRebuildLayoutImmediate(enemyBarContainer);

        var newPositions = new List<Vector2>();
        for (int i = 0; i < siblings.Count; i++)
        {
            if (siblings[i] == null) continue;
            newPositions.Add(siblings[i].anchoredPosition);
        }

        var containerLayout = enemyBarContainer.GetComponent<VerticalLayoutGroup>();
        if (containerLayout != null)
            containerLayout.enabled = false;

        AdjustEnemyNamePositions();

        for (int i = 0; i < siblings.Count && i < newPositions.Count; i++)
        {
            if (siblings[i] == null) continue;
            siblings[i].anchoredPosition = oldPositions[i];
            siblings[i].DOAnchorPos(newPositions[i], 0.3f).SetEase(Ease.OutQuad);
        }

        yield return new WaitForSeconds(0.3f);
        if (containerLayout != null)
            containerLayout.enabled = true;
        AdjustEnemyNamePositions();
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
