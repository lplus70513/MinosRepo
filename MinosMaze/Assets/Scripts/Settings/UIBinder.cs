using UnityEngine;
using UnityEngine.UI;

public class UIBinder : MonoBehaviour
{
    // 在 Inspector 中将按钮拖拽到这里
    public Button settingsButton;

    void Start()
    {
        // 检查引用是否已正确赋值
        if (settingsButton != null)
        {
            // 为按钮的 onClick 事件动态添加监听器
            // 这里使用 Lambda 表达式来调用 GameManager 的方法
            settingsButton.onClick.AddListener(() =>
            {
                // 确保 GameManager 实例存在
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.OpenSettings();
                }
                else
                {
                    Debug.LogError("GameManager 实例未找到！");
                }
            });
        }
    }
}