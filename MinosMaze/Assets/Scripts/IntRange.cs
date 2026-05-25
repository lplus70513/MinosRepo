using UnityEngine;

[System.Serializable]
public struct IntRange
{
    [field: SerializeField] public int Min { get; private set; }
    [field: SerializeField] public int Max { get; private set; }

    public IntRange(int min, int max)
    {
        Min = min;
        Max = max;
    }

    public int RandomValue => Random.Range(Min, Max + 1);
}
