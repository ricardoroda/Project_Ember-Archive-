using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;

public class InventoryManager : MonoBehaviour
{
    public InventorySlot[] inventorySlots;
    public GameObject InventoryItemPrefab; 
    public GameObject inventoryPanel;
    public GameObject inventoryTab;
    public Button inventoryTabButton;
    public Button closeInventoryButton;
    public Button openInventoryButton;
    public TextMeshProUGUI goldText;
    public TextMeshProUGUI soulsText;
    public GameObject[] blockingPanels;

    public void Update()
    {
        if (Keyboard.current == null) Debug.LogWarning("InventoryManager: Keyboard.current is null. Ensure you have the Input System package installed and set up correctly.");
        if (Keyboard.current.iKey.wasPressedThisFrame)
        {
            if (inventoryPanel != null && inventoryPanel.activeSelf)
            {
                closeInventoryButton?.onClick.Invoke();
            }
            else
            {
                // if any blocking panels are open, do not open the skill tree
                if (IsAnyBlockingPanelOpen()) return;

                openInventoryButton?.onClick.Invoke();
            }
        }

        if (goldText != null)
        {
            PlayerStats stats = FindFirstObjectByType<PlayerStats>();
            if (stats != null) goldText.text = "Gold: " + stats.gold;
        }

        if (soulsText != null)
        {
            PlayerStats stats = FindFirstObjectByType<PlayerStats>();
            if (stats != null) soulsText.text = "Souls: " + stats.souls;
        }

        if (inventoryTab.activeInHierarchy)
        {
            inventoryTabButton.GetComponent<Image>().color = new Color(0.82f, 0.55f, 0.35f, 1f);
        }
        else
        {
            inventoryTabButton.GetComponent<Image>().color = new Color(0.60f, 0.40f, 0.27f, 1f);
        }
    }

    private bool IsAnyBlockingPanelOpen()
    {
        if (blockingPanels == null) return false;
        foreach (var g in blockingPanels)
        {
            if (g != null && g.activeInHierarchy) return true;
        }
        return false;
    }
    public bool AddItem(ItemSO item)
    {
        return AddItem(item, 1);
    }

   //Add logic
    public bool AddItem(ItemSO item, int amount)
    {
        if (item == null || amount <= 0) return false;

        int remaining = amount;

        // if stackable, try to add to existing items first
        if (item.stackable)
        {
            for (int i = 0; i < inventorySlots.Length && remaining > 0; i++)
            {
                InventorySlot slot = inventorySlots[i];
                if (slot.transform.childCount > 0)
                {
                    InventoryItem existing = slot.GetComponentInChildren<InventoryItem>();
                    if (existing != null && existing.item == item && existing.count < item.maxStack)
                    {
                        remaining = existing.AddAmount(remaining);
                    }
                }
            }
        }

        // if slot full create items in empty slots
        for (int i = 0; i < inventorySlots.Length && remaining > 0; i++)
        {
            InventorySlot slot = inventorySlots[i];
            if (slot.transform.childCount == 0)
            {
                int spawnAmount = item.stackable ? Mathf.Min(item.maxStack, remaining) : 1;
                SpawnNewItem(item, slot, spawnAmount);
                remaining -= spawnAmount;
            }
        }

        return remaining == 0;
    }

    void SpawnNewItem(ItemSO item, InventorySlot slot, int amount = 1)
    {
        GameObject newItemObject = Instantiate(InventoryItemPrefab, slot.transform);
        newItemObject.transform.localPosition = Vector3.zero;

        InventoryItem inventoryItem = newItemObject.GetComponent<InventoryItem>();
        if (inventoryItem != null)
        {
            inventoryItem.InitialiseItem(item, amount);
            inventoryItem.parentAfterDrag = slot.transform;
        }
        else
        {
            Debug.LogWarning("InventoryItem component missing on InventoryItemPrefab.");
        }
    }
}
