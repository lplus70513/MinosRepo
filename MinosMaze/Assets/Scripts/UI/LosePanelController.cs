using UnityEngine;
using UnityEngine.UI;

public class LosePanelController : MonoBehaviour
{
    [SerializeField] private Button returnButton;

    void Awake()
    {
        if (returnButton == null)
            returnButton = GetComponentInChildren<Button>();
        if (returnButton != null)
            returnButton.onClick.AddListener(OnReturn);
    }

    private void OnReturn()
    {
        GameManager.Instance.ReturnToMainMenu();
    }
}
