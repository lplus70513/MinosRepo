using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]

public abstract class TargetMode
{
    public abstract List<CombatantView> GetTargets();
}
