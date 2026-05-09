using SerializeReferenceEditor;
using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "Data/Card")]
public class CardData : ScriptableObject
{
    [field: SerializeField] public string Description { get; private set; }
    [field: SerializeField] public int Cost { get; private set; }
    [field: SerializeField] public Sprite Image { get; private set; }
    [field: SerializeField] public Sprite Background { get; private set; }

    [field: SerializeReference, SR] public List<Effect> Effects { get; private set; } // 创建卡牌效果列表 
}