using UnityEngine;

public class Interactions : Singleton<Interactions>
{
    public bool PlayerIsDragging { get; set; } = false;
    public bool PlayerIsTargeting { get; set; } = false;
    public bool IsViewingDeck { get; set; } = false;

    public bool PlayerCanInteract()
    {
        if (IsViewingDeck) return false;
        if (!ActionSystem.Instance.IsPerforming) return true;
        else return false;
    }

    public bool PlayerCanHover()
    {
        if (IsViewingDeck) return false;
        if (PlayerIsDragging || PlayerIsTargeting) return false;
        return true;
    }
}
