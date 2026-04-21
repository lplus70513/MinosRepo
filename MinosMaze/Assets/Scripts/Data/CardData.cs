using UnityEngine;

[CreateAssetMenu(menuName = "Data/Card")]

public class CardData : ScriptableObject
{
    [field: SerializeField] public string Descripition { get; private set; }
    [field: SerializeField] public int Cost { get; private set; }
    [field: SerializeField] public Sprite Image { get; private set; }
}
