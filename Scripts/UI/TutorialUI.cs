using UnityEngine;
using UnityEngine.UI;

public class TutorialUI : MonoBehaviour
{
    [Header("Buttons")]
    public Button closeButton;

    [Header("References")]
    public GameObject playerHUD;
    public GameObject interactionUI;

    void OnEnable()
    {
        // try to find the HUD like the original did (optional)
        if (playerHUD == null)
            playerHUD = GameObject.Find("PlayerHUD(Clone)");

        if (closeButton != null)
        {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(ClosePanel);
        }
    }

    void OnDisable()
    {
        if (closeButton != null)
            closeButton.onClick.RemoveAllListeners();
    }

    // Keep the same close behaviour as before: re-enable interaction hint and hide this panel
    public void ClosePanel()
    {
        if (interactionUI != null)
            interactionUI.SetActive(true);

        gameObject.SetActive(false);
    }
}
