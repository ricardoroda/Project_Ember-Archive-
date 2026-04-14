using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class EquipSlot : MonoBehaviour, IDropHandler
{
    public EquipmentTagsData.EquipSlotType accepts;
    public int slotIndex = -1;
    public bool hideOnEmpty = false;

    public Action<EquipmentSO, int> OnEquip;
    public Action<EquipmentSO, int> OnUnequip;

    // PlayerEquipController will assign this to apply custom rules
    public Func<EquipmentSO, int, bool> CanPlace;

    private EquipmentItem lastEquipped;

    public bool Accepts(EquipmentTagsData.EquipSlotType t) => t == accepts;
    public bool IsEmpty() => transform.childCount == 0;

    public void OnDrop(PointerEventData eventData)
    {
        if (eventData == null || eventData.pointerDrag == null) return;

        var dragged = eventData.pointerDrag.GetComponent<EquipmentItem>();
        if (dragged == null) return;

        // Normal quick check: slot accepts the item's declared slot?
        bool slotAcceptsDeclared = Accepts(dragged.equipment.equipSlot);

        // Allow special case: a main-hand weapon
        // may be considered for an offhand slot if controller's CanPlace approves.
        if (!slotAcceptsDeclared)
        {
            if (dragged.equipment.equipSlot != EquipmentTagsData.EquipSlotType.WeaponMain ||
                accepts != EquipmentTagsData.EquipSlotType.WeaponOffhand)
                return; // reject
        }

        // ask controller if this item may be placed here
        if (CanPlace != null && !CanPlace(dragged.equipment, slotIndex)) return;

        // slot empty -> place
        if (IsEmpty())
        {
            dragged.parentAfterDrag = transform;
            dragged.transform.SetParent(transform, false);
            dragged.transform.localPosition = Vector3.zero;

            lastEquipped = dragged;
            OnEquip?.Invoke(dragged.equipment, slotIndex);
            return;
        }

        // swap with existing item
        var existingItem = transform.GetChild(0).GetComponent<EquipmentItem>();
        if (existingItem != null)
        {
            Transform draggedOriginalParent = dragged.parentAfterDrag;

            existingItem.transform.SetParent(draggedOriginalParent, false);
            existingItem.transform.localPosition = Vector3.zero;
            existingItem.parentAfterDrag = draggedOriginalParent;

            dragged.parentAfterDrag = transform;
            dragged.transform.SetParent(transform, false);
            dragged.transform.localPosition = Vector3.zero;

            OnUnequip?.Invoke(existingItem.equipment, slotIndex);
            lastEquipped = dragged;
            OnEquip?.Invoke(dragged.equipment, slotIndex);
        }
    }

    private void OnTransformChildrenChanged()
    {
        if (IsEmpty())
        {
            if (lastEquipped != null)
            {
                OnUnequip?.Invoke(lastEquipped.equipment, slotIndex);
                lastEquipped = null;
            }
            return;
        }

        var child = transform.GetChild(0).GetComponent<EquipmentItem>();
        if (child == null) return;

        if (lastEquipped == null || lastEquipped != child)
        {
            lastEquipped = child;
            OnEquip?.Invoke(child.equipment, slotIndex);
        }
    }

    public EquipmentItem RemoveItem()
    {
        if (IsEmpty()) return null;
        var child = transform.GetChild(0);
        var ei = child.GetComponent<EquipmentItem>();
        if (ei != null)
        {
            OnUnequip?.Invoke(ei.equipment, slotIndex);
            ei.transform.SetParent(null, true);
            ei.parentAfterDrag = null;
            lastEquipped = null;
            return ei;
        }
        return null;
    }
}
