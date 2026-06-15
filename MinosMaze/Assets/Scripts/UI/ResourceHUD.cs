using TMPro;
using UnityEngine;

public class ResourceHUD : MonoBehaviour
{
    [SerializeField] private TMP_Text goldText;
    [SerializeField] private TMP_Text stringText;

    void Update()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null || gm.WorldMapState == null) return;
        if (goldText != null) goldText.text = gm.WorldMapState.gold.ToString();
        if (stringText != null) stringText.text = gm.WorldMapState.stringCount.ToString();
    }
}
