using UnityEngine;
using UnityEngine.UI;

public class SettingManager : MonoBehaviour
{
    [Header("按钮引用")]
    [SerializeField] private Button continueButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button abandonButton;
    [SerializeField] private Button saveAndExitButton;

    [Header("设置子面板")]
    [SerializeField] private GameObject settingsSubPanel;

    [Header("音量滑条")]
    [SerializeField] private Slider bgmSlider;
    [SerializeField] private Slider sfxSlider;

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

        if (bgmSlider != null)
        {
            bgmSlider.value = AudioManager.Instance != null ? AudioManager.Instance.BGMVolume : 1f;
            bgmSlider.onValueChanged.AddListener(v => { if (AudioManager.Instance != null) AudioManager.Instance.SetBGMVolume(v); });
        }
        if (sfxSlider != null)
        {
            sfxSlider.value = AudioManager.Instance != null ? AudioManager.Instance.SFXVolume : 1f;
            sfxSlider.onValueChanged.AddListener(v => { if (AudioManager.Instance != null) AudioManager.Instance.SetSFXVolume(v); });
        }
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
