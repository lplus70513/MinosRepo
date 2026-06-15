using UnityEngine;

[System.Serializable]
public class BlessingEntry
{
    public string blessingId;
    [TextArea] public string description;
    public BlessingEffectType effectType;
    public int effectValue;
    public BlessingCostType costType;
    public int costAmount;
    public bool repeatable;
}
