using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(RectTransform))]
public class ArmorySlot : MonoBehaviour, IDropHandler
{
    [HideInInspector] public int slotIndex;
    private ArmoryManager manager;

    public void Setup(int index, ArmoryManager m)
    {

        manager = m;
    }

    public bool IsEmpty() => transform.childCount == 0;

   
    public void SetItemInstance(EquipmentItem instance)
    {
        if (instance == null) return;
        instance.transform.SetParent(transform, false);
        instance.transform.localPosition = Vector3.zero;
        instance.parentAfterDrag = transform;
    }

    public EquipmentItem RemoveItem()
    {
        if (transform.childCount == 0) return null;
        var child = transform.GetChild(0);
        var ei = child.GetComponent<EquipmentItem>();
        if (ei != null)
        {
            ei.transform.SetParent(null, true);
            return ei;
        }
        return null;
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (eventData == null || eventData.pointerDrag == null) return;

        // only accept EquipmentItem instances
        var draggedEquip = eventData.pointerDrag.GetComponent<EquipmentItem>();
        if (draggedEquip == null) return;

        if (IsEmpty())
        {
            draggedEquip.parentAfterDrag = transform;
            draggedEquip.transform.SetParent(transform, false);
            draggedEquip.transform.localPosition = Vector3.zero;
            return;
        }

        // swap: move existing item back to its original parent
        // and place dragged item in this slot
        var existing = transform.GetChild(0).GetComponent<EquipmentItem>();
        if (existing != null)
        {
            Transform originalParent = draggedEquip.parentAfterDrag;

            existing.transform.SetParent(originalParent, false);
            existing.transform.localPosition = Vector3.zero;
            existing.parentAfterDrag = originalParent;

            draggedEquip.parentAfterDrag = transform;
            draggedEquip.transform.SetParent(transform, false);
            draggedEquip.transform.localPosition = Vector3.zero;
        }
    }
}
