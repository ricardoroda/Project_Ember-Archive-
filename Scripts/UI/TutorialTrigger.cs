using UnityEngine;
using UnityEngine.InputSystem;

public class TutorialTrigger : MonoBehaviour
{
    [Header("UI Panels (assign in Inspector or use defaults)")]
    public GameObject interactionUI;   // small "press E" hint shown when player is nearby
    public GameObject tutorialUI;      // panel opened with E (was levelSelectionUI)
    private GameObject playerHUD;

    bool playerNearby = false;
    bool tutorialOpen = false;

    void Start()
    {
        HudFinder();
        if (interactionUI != null) interactionUI.SetActive(false);
        if (tutorialUI != null) tutorialUI.SetActive(false);
    }

    void Update()
    {
        // open tutorial with E when near
        if (!tutorialOpen && playerNearby && Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
        {
            OpenTutorial();
        }

        // close tutorial with Escape
        if (tutorialOpen && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            CloseTutorial();
        }

        // if tutorial was disabled externally, close properly
        if (tutorialOpen && tutorialUI != null && !tutorialUI.activeSelf)
        {
            CloseTutorial();
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
            if (!tutorialOpen && interactionUI != null) interactionUI.SetActive(true);
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

    void OpenTutorial()
    {
        HudFinder();
        tutorialOpen = true;
        if (interactionUI != null) interactionUI.SetActive(false);
        if (playerHUD != null) playerHUD.SetActive(false);
        if (tutorialUI != null) tutorialUI.SetActive(true);

        Time.timeScale = 0f; // pause game
        Debug.Log("TutorialTrigger: Opened tutorial and paused time.");
    }

    public void CloseTutorial()
    {
        tutorialOpen = false;
        if (tutorialUI != null) tutorialUI.SetActive(false);
        if (playerHUD != null) playerHUD.SetActive(true);

        if (playerNearby && interactionUI != null) interactionUI.SetActive(true);

        Time.timeScale = 1f; // resume game
        Debug.Log("TutorialTrigger: Closed tutorial and resumed time.");
    }

    void OnDestroy()
    {
        if (Time.timeScale == 0f) Time.timeScale = 1f;
    }
}
