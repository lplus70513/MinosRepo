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

    void Start()
    {
        gameObject.SetActive(false);
    }

    private void OnReturnMainMenu()
    {
        GameManager.Instance.ReturnToMainMenu();
    }
}
