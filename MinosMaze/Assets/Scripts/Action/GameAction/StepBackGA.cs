public class StepBackGA : GameAction
{
    public CombatantView Mover { get; private set; }

    public StepBackGA(CombatantView mover)
    {
        Mover = mover;
    }
}
