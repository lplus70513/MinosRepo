using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class ButtonClickSFX : MonoBehaviour
{
    private void Awake()
    {
        GetComponent<Button>().onClick.AddListener(OnClick);
    }

    private void OnClick()
    {
        if (AudioManager.Instance != null && AudioManager.Instance.Config != null)
            AudioManager.Instance.PlaySFX(AudioManager.Instance.Config.buttonClickSFX);
    }
}
