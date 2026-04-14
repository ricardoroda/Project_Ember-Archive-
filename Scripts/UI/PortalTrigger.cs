using UnityEngine;
using UnityEngine.InputSystem;

public class PortalTrigger : MonoBehaviour
{
    [Header("UI Panels (assign in Inspector or use defaults)")]
    public GameObject interactionUI;
    public GameObject levelSelectionUI;
    private GameObject playerHUD;

    bool playerNearby = false;
    bool levelSelectionOpen = false;

    void Start()
    {
        // start state: interaction and level selection hidden, HUD visible
        HudFinder();
        if (interactionUI != null) interactionUI.SetActive(false);
        if (levelSelectionUI != null) levelSelectionUI.SetActive(false);
    }

    void Update()
    {
        // only open with E when player is near and level selection is not already open
        if (!levelSelectionOpen && playerNearby && Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
        {
            OpenLevelSelection();
        }

        // close level selection with Escape
        if (levelSelectionOpen && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            CloseLevelSelection();
        }

        // close level selection with button (external close)
        if (levelSelectionOpen && levelSelectionUI != null && !levelSelectionUI.activeSelf)
        {
            CloseLevelSelection();
        }
    }

    void HudFinder()
    {
        playerHUD = GameObject.Find("PlayerHUD(Clone)");
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerNearby = true;
            // show the 'press E' interaction only if level selection is closed
            if (!levelSelectionOpen && interactionUI != null) interactionUI.SetActive(true);
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerNearby = false;
            if (interactionUI != null) interactionUI.SetActive(false);
        }
    }

    void OpenLevelSelection()
    {
        HudFinder();
        levelSelectionOpen = true;
        if (interactionUI != null) interactionUI.SetActive(false);
        if (playerHUD != null) playerHUD.SetActive(false);
        if (levelSelectionUI != null) levelSelectionUI.SetActive(true);

        // pause game time to prevent player movement
        Time.timeScale = 0f;

        Debug.Log("PortalTrigger: Opened level selection and paused time.");
    }

    public void CloseLevelSelection()
    {
        levelSelectionOpen = false;
        if (levelSelectionUI != null) levelSelectionUI.SetActive(false);
        if (playerHUD != null) playerHUD.SetActive(true);

        // if player is still near, re-show interaction UI
        if (playerNearby && interactionUI != null) interactionUI.SetActive(true);

        // resume game time
        Time.timeScale = 1f;

        Debug.Log("PortalTrigger: Closed level selection and resumed time.");
    }

    void OnDestroy()
    {
        // ensure timeScale restored if script is destroyed while paused
        if (Time.timeScale == 0f) Time.timeScale = 1f;
    }
}
