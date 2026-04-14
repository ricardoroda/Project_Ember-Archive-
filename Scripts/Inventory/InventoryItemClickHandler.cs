using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(InventoryItem))]
public class InventoryItemClickHandlerSimple : MonoBehaviour, IPointerClickHandler
{
    private InventoryItem invItem;
    private ItemDetailPanel panel;

    private void Awake()
    {
        invItem = GetComponent<InventoryItem>();

        panel = FindFirstObjectByType<ItemDetailPanel>(); 
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        // only show details on left click
        if (eventData.button != PointerEventData.InputButton.Left) return;

        if (invItem == null || invItem.item == null || panel == null) return;

        panel.Show(invItem.item);
    }
}
