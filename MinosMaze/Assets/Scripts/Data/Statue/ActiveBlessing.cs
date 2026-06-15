[System.Serializable]
public class ActiveBlessing
{
    public string blessingId;
    public string statueName;
    public BlessingEffectType effectType;
    public int effectValue;
    public int count;

    public ActiveBlessing(string blessingId, string statueName, BlessingEffectType effectType, int effectValue)
    {
        this.blessingId = blessingId;
        this.statueName = statueName;
        this.effectType = effectType;
        this.effectValue = effectValue;
        this.count = 1;
    }
}
