public class PullTargetGA : GameAction
{
    public CombatantView Target { get; private set; }
    public CombatantView Puller { get; private set; }

    public PullTargetGA(CombatantView target, CombatantView puller)
    {
        Target = target;
        Puller = puller;
    }
}
