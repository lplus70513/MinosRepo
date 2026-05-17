public class MoveGA : GameAction
{
    public CombatantView Mover { get; private set; }
    public int ToX { get; private set; }
    public int ToZ { get; private set; }

    public MoveGA(CombatantView mover, int toX, int toZ)
    {
        Mover = mover;
        ToX = toX;
        ToZ = toZ;
    }
}
