using UnityEngine;
using UnityEngine.EventSystems;

// Simple per-slot behaviour
public class ActionBarSlot : MonoBehaviour, IDropHandler
{
    public enum SlotKind { Dash, Ability, Item }
    public SlotKind slotKind = SlotKind.Ability;
    [HideInInspector] public int slotIndex = -1;
    [HideInInspector] public ActionBar owner;

    void Awake() { if (owner == null) owner = GetComponentInParent<ActionBar>(); }

    public bool IsEmpty() => transform.childCount == 0;

    public bool AcceptsSkill(SkillItem.SkillType skillType)
    {
        if (slotKind == SlotKind.Item) return false;
        if (slotKind == SlotKind.Dash) return skillType == SkillItem.SkillType.Dash;
        return slotKind == SlotKind.Ability && skillType == SkillItem.SkillType.Normal;
    }

    public void SetItemInstance(MonoBehaviour item)
    {
        if (item == null) return;
        item.transform.SetParent(transform, false);
        item.transform.localPosition = Vector3.zero;
        if (item is SkillItem s) s.parentAfterDrag = transform;
        if (item is InventoryItem it) it.parentAfterDrag = transform;
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (owner == null) owner = FindFirstObjectByType<ActionBar>();
        if (owner == null) return;
        owner.HandleDrop(this, eventData.pointerDrag);
    }
}
