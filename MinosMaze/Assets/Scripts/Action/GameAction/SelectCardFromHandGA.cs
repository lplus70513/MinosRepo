public class SelectCardFromHandGA : GameAction
{
    public GameAction OnSelectAction { get; private set; }
    public Card SelectedCard { get; set; }

    public SelectCardFromHandGA(GameAction onSelectAction)
    {
        OnSelectAction = onSelectAction;
    }
}
