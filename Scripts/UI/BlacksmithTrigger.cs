using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class BlacksmithTrigger : MonoBehaviour
{
    [Header("UI Panels")]
    public GameObject interactionUIBlacksmith; // "Press E" hint
    public GameObject blacksmithUI;            // blacksmith window

    private Button inventoryButton;      // Inventory button on HUD
    private GameObject inventoryUI;      // will point to GameObject named "Inventory" (only searched when needed)

    bool playerNearby = false;
    bool blacksmithOpen = false;

    void Start()
    {
        // start UI state
        if (interactionUIBlacksmith != null) interactionUIBlacksmith.SetActive(false);
        if (blacksmithUI != null) blacksmithUI.SetActive(false);
    }

    void Update()
    {
        // find inventory button by name
        GameObject btnObj = GameObject.Find("InventoryButton");
        if (btnObj != null) inventoryButton = btnObj.GetComponent<Button>();
        // open blacksmith with E
        if (!blacksmithOpen && playerNearby && Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
        {
            OpenBlacksmith();
        }

        // close blacksmith when pressing I
        if (blacksmithOpen && Keyboard.current != null && Keyboard.current.iKey.wasPressedThisFrame)
        {
            CloseBlacksmith();
        }

        // if blacksmith was closed externally, sync state
        if (blacksmithOpen && blacksmithUI != null && !blacksmithUI.activeSelf)
        {
            CloseBlacksmith();
        }

        // only try to find Inventory while blacksmith is open (it should be active then)
        if (blacksmithOpen && inventoryUI == null)
        {
            inventoryUI = GameObject.Find("Inventory"); // only finds active objects
        }

        // if we have a reference and Inventory becomes inactive, close blacksmith
        if (blacksmithOpen && inventoryUI != null && !inventoryUI.activeSelf)
        {
            Debug.Log("BlacksmithTrigger: Inventory became inactive -> closing blacksmith.");
            CloseBlacksmith();
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerNearby = true;
            if (!blacksmithOpen && interactionUIBlacksmith != null) interactionUIBlacksmith.SetActive(true);
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerNearby = false;
            if (interactionUIBlacksmith != null) interactionUIBlacksmith.SetActive(false);
        }
    }

    public void OpenBlacksmith()
    {
        blacksmithOpen = true;
        if (interactionUIBlacksmith != null) interactionUIBlacksmith.SetActive(false);
        if (blacksmithUI != null) blacksmithUI.SetActive(true);

        // press inventory button so inventory opens too (if button exists)
        if (inventoryButton != null)
        {
            inventoryButton.onClick.Invoke();
            // now inventory should be active — capture it
            inventoryUI = GameObject.Find("Inventory");
        }

        // pause game to prevent player movement
        Time.timeScale = 0f;

        Debug.Log("BlacksmithTrigger: Opened blacksmith and paused time.");
    }

    public void CloseBlacksmith()
    {
        blacksmithOpen = false;
        if (blacksmithUI != null) blacksmithUI.SetActive(false);

        if (playerNearby && interactionUIBlacksmith != null) interactionUIBlacksmith.SetActive(true);

        // resume game
        Time.timeScale = 1f;

        // clear cached inventory reference so we will re-find it next time if needed
        inventoryUI = null;

        Debug.Log("BlacksmithTrigger: Closed blacksmith and resumed time.");
    }

    void OnDestroy()
    {
        if (Time.timeScale == 0f) Time.timeScale = 1f;
    }
}
