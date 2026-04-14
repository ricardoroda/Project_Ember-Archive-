using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasGroup))]
public class EquipmentItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
{
    [HideInInspector] public EquipmentSO equipment;
    [HideInInspector] public Transform parentAfterDrag;

    // optional: keep previous sibling if you need to restore order later
    [HideInInspector] public int previousSiblingIndex = -1;

    public Image icon;

    private CanvasGroup canvasGroup;

    // NEW: store where we parented the item while dragging so OnEndDrag can detect fallback
    Transform dragCanvas;

    void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
    }

    public void Initialise(EquipmentSO eq)
    {
        equipment = eq;
        parentAfterDrag = transform.parent;

        // remove any stale OriginalParentInfo that might remain on this instance
        var stale = GetComponent<OriginalParentInfo>();
        if (stale != null) Destroy(stale);

        if (icon != null) icon.sprite = eq.icon;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        canvasGroup.blocksRaycasts = false;

        // store previous sibling index (small safe addition)
        previousSiblingIndex = transform.GetSiblingIndex();

        // prefer stored original parent (set by BlacksmithShop) if present; else use current parent
        var storedInfo = GetComponent<OriginalParentInfo>();
        parentAfterDrag = (storedInfo != null && storedInfo.originalParent != null) ? storedInfo.originalParent : transform.parent;

        // Choose the best canvas to parent to so the item stays visible while dragging:
        // 1) original parent's Canvas (if stored)
        // 2) PlayerHUD(Clone) if exists (common HUD canvas)
        // 3) nearest parent Canvas
        // 4) any Canvas in scene
        Transform canvasTransform = null;
        if (storedInfo != null && storedInfo.originalParent != null)
        {
            var c = storedInfo.originalParent.GetComponentInParent<Canvas>();
            if (c != null) canvasTransform = c.transform;
        }

        if (canvasTransform == null)
        {
            var hud = GameObject.Find("PlayerHUD(Clone)");
            if (hud != null) canvasTransform = hud.transform;
        }

        if (canvasTransform == null)
        {
            var parentCanvas = transform.GetComponentInParent<Canvas>();
            if (parentCanvas != null) canvasTransform = parentCanvas.transform;
        }

        if (canvasTransform == null)
        {
            var anyCanvas = FindFirstObjectByType<Canvas>();
            if (anyCanvas != null) canvasTransform = anyCanvas.transform;
        }

        // final fallback
        if (canvasTransform == null) canvasTransform = transform.root;

        // store the canvas used so OnEndDrag can tell whether to revert
        dragCanvas = canvasTransform;

        // parent to the chosen canvas (keeps visible) and bring to top
        transform.SetParent(canvasTransform, true);
        transform.SetAsLastSibling();
    }

    public void OnDrag(PointerEventData eventData)
    {
        transform.position = Input.mousePosition;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        canvasGroup.blocksRaycasts = true;

        // sell slot check (if any)
        GameObject rayGo = eventData.pointerCurrentRaycast.gameObject;
        if (rayGo == null) rayGo = eventData.pointerEnter;
        var sell = rayGo != null ? rayGo.GetComponentInParent<SellSlot>() : null;
        if (sell != null)
        {
            bool sold = sell.HandleDrop(gameObject);
            if (sold) return; // do not revert
        }

        // If not accepted by any slot, return to original parent **only if still parented to dragCanvas**.
        // This avoids undoing deliberate reparenting by drop handlers (like Blacksmith.OnDrop).
        if (transform.parent == dragCanvas && parentAfterDrag != null)
        {
            transform.SetParent(parentAfterDrag, false);
            transform.localPosition = Vector3.zero;
            transform.SetAsLastSibling();

            // cleanup OriginalParentInfo — once returned to original container it is no longer needed
            var info = GetComponent<OriginalParentInfo>();
            if (info != null) Destroy(info);
        }

        // clear dragCanvas (safety)
        dragCanvas = null;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (equipment == null) return;
        if (eventData.button != PointerEventData.InputButton.Right) return;

        var armorySlot = GetComponentInParent<ArmorySlot>();
        if (armorySlot != null) { TryEquipFromArmory(armorySlot); return; }

        var equipSlot = GetComponentInParent<EquipSlot>();
        if (equipSlot != null) { TryUnequipToArmory(equipSlot); return; }
    }

    void TryEquipFromArmory(ArmorySlot fromArmory)
    {
        var playerEquip = FindFirstObjectByType<PlayerEquipController>();
        EquipSlot[] slots = playerEquip != null
            ? playerEquip.GetComponentsInChildren<EquipSlot>()
            : FindObjectsByType<EquipSlot>(FindObjectsSortMode.None);

        foreach (var slot in slots)
        {
            if (slot == null) continue;

            bool slotAcceptsDeclared = slot.Accepts(equipment.equipSlot);

            // ask controller rule (if present)
            bool canPlace = slot.CanPlace == null ? true : slot.CanPlace(equipment, slot.slotIndex);

            if (!slotAcceptsDeclared)
            {
                // allow WeaponMain items to be considered for WeaponOffhand if CanPlace allows it
                if (equipment.equipSlot != EquipmentTagsData.EquipSlotType.WeaponMain ||
                    slot.accepts != EquipmentTagsData.EquipSlotType.WeaponOffhand)
                    continue; // not compatible
                if (!canPlace) continue;
            }
            else
            {
                if (!canPlace) continue;
            }

            // valid slot found
            if (slot.IsEmpty())
            {
                PutIntoEquipSlot(slot);
                return;
            }

            var existing = slot.transform.GetChild(0).GetComponent<EquipmentItem>();
            if (existing != null)
            {
                fromArmory.SetItemInstance(existing);
                PutIntoEquipSlot(slot);
                return;
            }
        }
    }

    void PutIntoEquipSlot(EquipSlot slot)
    {
        if (slot == null) return;

        if (slot.CanPlace != null && !slot.CanPlace(equipment, slot.slotIndex))
        {
            Debug.Log($"Cannot place {equipment.itemName} into slot {slot.name} due to placement rules.");
            return;
        }

        parentAfterDrag = slot.transform;
        transform.SetParent(slot.transform, false);
        transform.localPosition = Vector3.zero;
    }

    void TryUnequipToArmory(EquipSlot fromEquip)
    {
        var armory = FindFirstObjectByType<ArmoryManager>();
        ArmorySlot[] slots = armory != null ? armory.GetSlots() : FindObjectsByType<ArmorySlot>(FindObjectsSortMode.None);

        foreach (var s in slots)
        {
            if (s != null && s.IsEmpty())
            {
                s.SetItemInstance(this);
                parentAfterDrag = s.transform;
                return;
            }
        }

        Debug.Log($"[EquipmentItem] No empty armory slot to unequip {equipment.itemName}");
    }
}
