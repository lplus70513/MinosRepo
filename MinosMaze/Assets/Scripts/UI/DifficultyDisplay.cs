using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 难度显示组件。挂在各场景 Canvas 顶部，实时显示当前难度等级与数值倍率。
/// 格式： [难度图标] 0 (x100%)
/// 放置在 WorldMap、RestSite、StatueScene、Treasure 四个场景的 UI Canvas 上即可工作。
/// </summary>
public class DifficultyDisplay : MonoBehaviour
{
    [SerializeField] private Image difficultyIcon;
    [SerializeField] private TMP_Text difficultyText;

    void Start()
    {
        Refresh();
    }

    void Update()
    {
        Refresh();
    }

    private void Refresh()
    {
        if (difficultyText != null)
            difficultyText.text = DifficultySystem.GetDisplayString();
    }
}
