using UnityEngine;
using UnityEngine.UI;

public class GameWinPanelController : MonoBehaviour
{
    [SerializeField] private Button returnButton;

    void Awake()
    {
        if (returnButton == null)
            returnButton = GetComponentInChildren<Button>();
        if (returnButton != null)
            returnButton.onClick.AddListener(OnReturnMainMenu);
    }

    private void OnReturnMainMenu()
    {
        Debug.Log("[GameWinPanel] 返回主界面");
        GameManager.Instance.ReturnToMainMenu();
    }
}
