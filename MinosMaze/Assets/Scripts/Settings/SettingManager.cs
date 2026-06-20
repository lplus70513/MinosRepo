using UnityEngine;
using UnityEngine.UI;

public class SettingManager : MonoBehaviour
{
    [Header("按钮引用")]
    [SerializeField] private Button continueButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button abandonButton;
    [SerializeField] private Button saveAndExitButton;

    [Header("设置子面板（暂留空）")]
    [SerializeField] private GameObject settingsSubPanel;

    private void Start()
    {
        if (continueButton != null)
            continueButton.onClick.AddListener(OnContinue);
        if (settingsButton != null)
            settingsButton.onClick.AddListener(OnSettings);
        if (abandonButton != null)
            abandonButton.onClick.AddListener(OnAbandon);
        if (saveAndExitButton != null)
            saveAndExitButton.onClick.AddListener(OnSaveAndExit);
    }

    public void RefreshButtons(bool inGame)
    {
        if (abandonButton != null)
            abandonButton.gameObject.SetActive(inGame);
        if (saveAndExitButton != null)
            saveAndExitButton.gameObject.SetActive(inGame);
    }

    private void OnContinue()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.CloseSettings();
    }

    private void OnSettings()
    {
        if (settingsSubPanel != null)
            settingsSubPanel.SetActive(true);
    }

    private void OnAbandon()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.AbandonGame();
    }

    private void OnSaveAndExit()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.SaveAndExit();
    }
}
