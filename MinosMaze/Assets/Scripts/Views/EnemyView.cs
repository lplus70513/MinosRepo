using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class EnemyView : CombatantView
{
    [SerializeField] private TMP_Text attackText;

    public int AttackPower { get; set; }

    public string DisplayName { get; private set; }

    public EnemyType EnemyType { get; private set; }

    public void Setup(EnemyData enemyData, int hexX, int hexZ)
    {
        HexCoordX = hexX;
        HexCoordZ = hexZ;
        DisplayName = enemyData.DisplayName;
        EnemyType = enemyData.Type;
        AttackPower = enemyData.AttackPower;
        UpdateAttackText();
        SetupBase(enemyData.Health, enemyData.Image);
    }

    private void UpdateAttackText()
    {
        attackText.text = "ATK: " + AttackPower;
    }
}
