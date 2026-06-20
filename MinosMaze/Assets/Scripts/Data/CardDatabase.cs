using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Data/CardDatabase")]
public class CardDatabase : ScriptableObject
{
    [SerializeField] private List<CardData> allCards = new();

    private Dictionary<string, CardData> _lookup;

    public CardData GetCardByName(string cardName)
    {
        if (_lookup == null)
        {
            _lookup = new Dictionary<string, CardData>();
            foreach (var card in allCards)
            {
                if (card != null && !_lookup.ContainsKey(card.name))
                    _lookup[card.name] = card;
            }
        }

        _lookup.TryGetValue(cardName, out var result);
        return result;
    }

    public List<CardData> GetAllCards() => allCards;

#if UNITY_EDITOR
    [ContextMenu("自动填充所有卡牌")]
    private void AutoFill()
    {
        allCards.Clear();
        string[] guids = UnityEditor.AssetDatabase.FindAssets("t:CardData");
        foreach (string guid in guids)
        {
            string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
            CardData card = UnityEditor.AssetDatabase.LoadAssetAtPath<CardData>(path);
            if (card != null)
                allCards.Add(card);
        }
        UnityEditor.EditorUtility.SetDirty(this);
        Debug.Log($"[CardDatabase] 已自动填充 {allCards.Count} 张卡牌");
    }
#endif
}
