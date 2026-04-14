using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class ShopTrigger : MonoBehaviour
{
    [Header("UI Panels")]
    public GameObject interactionUIShop; 
    public GameObject shopUI;

    private Button inventoryButton; 
    private GameObject inventoryUI; 

    bool playerNearby = false;
    bool shopOpen = false;

    void Start()
    {


        // start UI state
        if (interactionUIShop != null) interactionUIShop.SetActive(false);
        if (shopUI != null) shopUI.SetActive(false);
    }

    void Update()
    {
                // find inventory button by name
        GameObject btnObj = GameObject.Find("InventoryButton");
        if (btnObj != null) inventoryButton = btnObj.GetComponent<Button>();
        // open shop with E
        if (!shopOpen && playerNearby && Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
        {
            OpenShop();
        }

        // close shop when pressing I
        if (shopOpen && Keyboard.current != null && Keyboard.current.iKey.wasPressedThisFrame)
        {
            CloseShop();
        }

        // if shop was closed externally, sync state
        if (shopOpen && shopUI != null && !shopUI.activeSelf)
        {
            CloseShop();
        }

        if (shopOpen && inventoryUI == null)
        {
            inventoryUI = GameObject.Find("Inventory");
        }

        if (shopOpen && inventoryUI != null && !inventoryUI.activeSelf)
        {
            Debug.Log("ShopTrigger: Inventory became inactive -> closing shop.");
            CloseShop();
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerNearby = true;
            if (!shopOpen && interactionUIShop != null) interactionUIShop.SetActive(true);
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerNearby = false;
            if (interactionUIShop != null) interactionUIShop.SetActive(false);
        }
    }

    void OpenShop()
    {
        shopOpen = true;
        if (interactionUIShop != null) interactionUIShop.SetActive(false);
        if (shopUI != null) shopUI.SetActive(true);

        // press inventory button so inventory opens too
        if (inventoryButton != null)
        {
            inventoryButton.onClick.Invoke();
            inventoryUI = GameObject.Find("Inventory");
        }

        // pause game to prevent player movement
        Time.timeScale = 0f;

        Debug.Log("ShopTrigger: Opened shop and paused time.");
    }

    public void CloseShop()
    {
        shopOpen = false;
        if (shopUI != null) shopUI.SetActive(false);

        if (playerNearby && interactionUIShop != null) interactionUIShop.SetActive(true);

        // resume game
        Time.timeScale = 1f;

        inventoryUI = null;

        Debug.Log("ShopTrigger: Closed shop and resumed time.");
    }

    void OnDestroy()
    {
        if (Time.timeScale == 0f) Time.timeScale = 1f;
    }
}
