public class AddPerkGA : GameAction
{
    public PerkData PerkData { get; private set; }

    public AddPerkGA(PerkData perkData)
    {
        PerkData = perkData;
    }
}
