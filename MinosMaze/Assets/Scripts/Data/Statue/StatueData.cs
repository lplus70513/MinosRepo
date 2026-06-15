using System.Collections.Generic;
using UnityEngine;

public enum StatueGender { Male, Female }

[CreateAssetMenu(menuName = "Data/StatueData")]
public class StatueData : ScriptableObject
{
    public string statueName;
    public StatueGender gender;
    public List<BlessingEntry> blessings;
}
