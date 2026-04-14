using UnityEngine;
using UnityEngine.EventSystems;

public class InventorySlot : MonoBehaviour, IDropHandler
{
    public void OnDrop(PointerEventData eventData)
    {
        if (eventData == null || eventData.pointerDrag == null) return;

        InventoryItem droppedItem = eventData.pointerDrag.GetComponent<InventoryItem>();
        if (droppedItem == null) return;

        InventoryItem itemHere = null;
        if (transform.childCount > 0)
            itemHere = transform.GetChild(0).GetComponent<InventoryItem>();

        if (itemHere == null)
        {
            droppedItem.parentAfterDrag = transform;
            return;
        }

        // Merge stacks
        if (itemHere.item == droppedItem.item && itemHere.item.stackable)
        {
            int leftover = itemHere.AddAmount(droppedItem.count);
            if (leftover == 0)
            {
                // destroy item if it was fully merged
                Destroy(droppedItem.gameObject);
            }
            else
            {
                droppedItem.count = leftover;
                droppedItem.UpdateCountText();
            }
            return;
        }

        // Swap
        Transform originalParent = droppedItem.parentAfterDrag;
        itemHere.transform.SetParent(originalParent);
        itemHere.transform.localPosition = Vector3.zero;

        droppedItem.parentAfterDrag = transform;
    }
}
