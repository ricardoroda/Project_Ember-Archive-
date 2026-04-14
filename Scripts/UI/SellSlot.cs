using UnityEngine;
using UnityEngine.EventSystems;

public class SellSlot : MonoBehaviour, IDropHandler
{
    PlayerStats playerStats;

    void Awake()
    {
        playerStats = FindFirstObjectByType<PlayerStats>();
    }

    public bool HandleDrop(GameObject dragged)
    {
        if (dragged == null) return false;

        // InventoryItem (stackable)
        var inv = dragged.GetComponent<InventoryItem>();
        if (inv != null && inv.item != null)
        {
            int pricePer = inv.item.shopSellPrice;
            int qty = Mathf.Max(1, inv.count);
            int total = pricePer * qty;

            if (playerStats != null) playerStats.GainGold(total);

            // Destroy the UI instance (removes it from the inventory slot)
            Destroy(inv.gameObject);
            Debug.Log($"SellSlot: sold {inv.item.itemName} x{qty} for {total} gold.");
            return true;
        }

        // EquipmentItem (unique)
        var eq = dragged.GetComponent<EquipmentItem>();
        if (eq != null && eq.equipment != null)
        {
            int total = Mathf.RoundToInt(eq.equipment.shopSellPrice);
            if (playerStats != null) playerStats.GainGold(total);

            Destroy(eq.gameObject);
            Debug.Log($"SellSlot: sold equipment {eq.equipment.itemName} for {total} gold.");
            return true;
        }

        return false;
    }

    // Support EventSystem OnDrop as well (pointerDrag).
    public void OnDrop(PointerEventData eventData)
    {
        HandleDrop(eventData.pointerDrag);
    }
}
