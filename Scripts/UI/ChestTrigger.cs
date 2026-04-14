using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class ChestTrigger : MonoBehaviour
{
    [Header("UI Panels (assign in Inspector)")]
    public GameObject interactionUI;
    public GameObject chestRewardUI;

    [Header("Optional: object to disable after closing the chest UI. If null, uses this GameObject.")]
    public GameObject chestObjectToDisable;

    [Header("HUD root (optional). If null, tries to find PlayerHUD(Clone)")]
    public GameObject playerHUDRoot;

    // runtime storage for children + their previous active states
    private List<GameObject> hudChildren = new List<GameObject>();
    private List<bool> hudChildrenPrevActive = new List<bool>();

    bool playerNearby = false;
    bool chestRewardOpen = false;

    private float previousTimeScale = 1f;
    private float previousFixedDeltaTime = 0.02f;

    void Start()
    {
        if (chestObjectToDisable == null)
            chestObjectToDisable = this.gameObject;

        TryFindHudRoot();

        if (interactionUI != null) interactionUI.SetActive(false);
        if (chestRewardUI != null) chestRewardUI.SetActive(false);

        previousTimeScale = Time.timeScale;
        previousFixedDeltaTime = Time.fixedDeltaTime;
    }

    void Update()
    {
        bool ePressed = (Keyboard.current != null) ? Keyboard.current.eKey.wasPressedThisFrame : Input.GetKeyDown(KeyCode.E);
        bool escPressed = (Keyboard.current != null) ? Keyboard.current.escapeKey.wasPressedThisFrame : Input.GetKeyDown(KeyCode.Escape);

        if (!chestRewardOpen && playerNearby && ePressed)
        {
            OpenChestReward();
        }

        if (chestRewardOpen && escPressed)
        {
            CloseChestReward();
        }

        // handle external closing of the UI
        if (chestRewardOpen && chestRewardUI != null && !chestRewardUI.activeSelf)
        {
            CloseChestReward();
        }
    }

    void TryFindHudRoot()
    {
        if (playerHUDRoot == null)
            playerHUDRoot = GameObject.Find("PlayerHUD(Clone)");
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerNearby = true;
            if (!chestRewardOpen && interactionUI != null) interactionUI.SetActive(true);
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

    void OpenChestReward()
    {
        TryFindHudRoot();

        chestRewardOpen = true;

        if (interactionUI != null) interactionUI.SetActive(false);

        // disable all direct children of the HUD root (keep the root itself active)
        hudChildren.Clear();
        hudChildrenPrevActive.Clear();

        if (playerHUDRoot != null)
        {
            foreach (Transform child in playerHUDRoot.transform)
            {
                hudChildren.Add(child.gameObject);
                hudChildrenPrevActive.Add(child.gameObject.activeSelf);
                child.gameObject.SetActive(false);
            }
        }

        if (chestRewardUI != null) chestRewardUI.SetActive(true);

        // pause game (store previous values)
        previousTimeScale = Time.timeScale;
        previousFixedDeltaTime = Time.fixedDeltaTime;
        Time.timeScale = 0f;
        Time.fixedDeltaTime = previousFixedDeltaTime * Time.timeScale;
    }

    public void CloseChestReward()
    {
        chestRewardOpen = false;

        if (chestRewardUI != null) chestRewardUI.SetActive(false);

        // disable the chest object now (as requested)
        if (chestObjectToDisable != null)
            chestObjectToDisable.SetActive(false);

        // restore all HUD children to their previous active state
        for (int i = 0; i < hudChildren.Count; i++)
        {
            var go = hudChildren[i];
            if (go != null)
                go.SetActive(hudChildrenPrevActive[i]);
        }

        hudChildren.Clear();
        hudChildrenPrevActive.Clear();

        if (playerNearby && interactionUI != null) interactionUI.SetActive(true);

        // restore time
        Time.timeScale = previousTimeScale;
        Time.fixedDeltaTime = previousFixedDeltaTime;
    }

    void OnDestroy()
    {
        // restore time if destroyed while paused
        if (Time.timeScale == 0f)
        {
            Time.timeScale = previousTimeScale;
            Time.fixedDeltaTime = previousFixedDeltaTime;
        }

        // restore HUD children if needed
        for (int i = 0; i < hudChildren.Count; i++)
        {
            var go = hudChildren[i];
            if (go != null)
                go.SetActive(hudChildrenPrevActive[i]);
        }
    }
}
