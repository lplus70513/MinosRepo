using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class NameInputDialog : MonoBehaviour
{
    [SerializeField] private TMP_InputField nameInput;
    [SerializeField] private Button confirmButton;

    private System.Action _onConfirmed;

    void Awake()
    {
        if (confirmButton != null)
            confirmButton.onClick.AddListener(OnConfirm);
    }

    public void Show(System.Action onConfirmed)
    {
        _onConfirmed = onConfirmed;
        gameObject.SetActive(true);
        if (nameInput != null)
        {
            nameInput.text = "";
            nameInput.Select();
            nameInput.ActivateInputField();
        }
    }

    private void OnConfirm()
    {
        string name = nameInput != null ? nameInput.text.Trim() : "";
        if (string.IsNullOrEmpty(name))
            name = "冒险者";

        var gm = GameManager.Instance;
        if (gm != null && gm.WorldMapState != null)
            gm.WorldMapState.playerName = name;

        gameObject.SetActive(false);
        _onConfirmed?.Invoke();
        _onConfirmed = null;
    }
}
