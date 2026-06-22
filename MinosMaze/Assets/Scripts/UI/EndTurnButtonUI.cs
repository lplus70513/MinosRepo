using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EndTurnButtonUI : MonoBehaviour
{
    // ������ǰ�غ�Button�����º������˻غ�
    public void OnClick()
    {
        if (!Interactions.Instance.PlayerCanInteract()) return;

        EnemyTurnGA enemyTurnGA = new();
        ActionSystem.Instance.Perform(enemyTurnGA, () =>
        {
            ActionSystem.Instance.Perform(new RefillCostGA());
        });
    }
}
