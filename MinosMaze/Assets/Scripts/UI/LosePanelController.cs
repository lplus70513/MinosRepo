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
        Debug.Log("[LosePanel] 返回主菜单");
        GameManager.Instance.ReturnToMainMenu();
    }
}
