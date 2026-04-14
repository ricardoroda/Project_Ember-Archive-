using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Minimal InventoryItem. Delegates equip/unequip to ActionBar to avoid duplicate logic.
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public class InventoryItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
{
    public ItemSO item;
    [HideInInspector] public Transform parentAfterDrag;
    [HideInInspector] public Transform originalParentBeforeEquip;
    [Header("UI")] public Image icon;
    public TextMeshProUGUI countText;
    public int count = 1;

    CanvasGroup cg;
    bool isProcessing = false;
    GameObject draggingInstance;

    void Awake()
    {
        cg = GetComponent<CanvasGroup>();
        if (cg == null) cg = gameObject.AddComponent<CanvasGroup>();
    }

    Transform GetDragRoot()
    {
        var cv = GetComponentInParent<Canvas>();
        return cv != null ? cv.transform : transform.root;
    }

    public void InitialiseItem(ItemSO newItem, int amount = 1)
    {
        item = newItem;
        count = Mathf.Max(1, amount);
        if (icon != null && item != null) icon.sprite = item.icon;
        UpdateCountText();
    }

    public void UpdateCountText()
    {
        if (countText == null) return;
        if (item != null && item.stackable) { countText.gameObject.SetActive(true); countText.text = count > 1 ? count.ToString() : ""; }
        else countText.gameObject.SetActive(false);
    }

    // add amount to this stack; return leftover that couldn't be added
    public int AddAmount(int amount)
    {
        if (item == null || !item.stackable) return amount;
        int space = item.maxStack - count;
        int toAdd = Mathf.Min(space, amount);
        count += toAdd;
        UpdateCountText();
        return amount - toAdd;
    }

    public bool ReduceAmount(int amount)
    {
        count -= amount;
        UpdateCountText();
        if (count <= 0) { Destroy(gameObject, 0.01f); return true; }
        return false;
    }

    // ---------------- Drag & Drop ----------------
    public void OnBeginDrag(PointerEventData eventData)
    {
        if (isProcessing) return;
        parentAfterDrag = transform.parent;
        draggingInstance = gameObject;
        if (cg != null) cg.blocksRaycasts = false;
        if (cg != null) cg.alpha = 0.6f;
        transform.SetParent(GetDragRoot(), true);
        transform.SetAsLastSibling();
    }

    public void OnDrag(PointerEventData eventData) { if (draggingInstance != null) draggingInstance.transform.position = Input.mousePosition; }

    // Action bar and sell slot on drag
    public void OnEndDrag(PointerEventData eventData)
    {
        if (isProcessing) return;
        isProcessing = true;

        GameObject rayGo = eventData.pointerCurrentRaycast.gameObject;
        if (rayGo == null) rayGo = eventData.pointerEnter;

        // existing ActionBar handling
        ActionBarSlot dropSlot = rayGo != null ? rayGo.GetComponentInParent<ActionBarSlot>() : null;
        if (dropSlot != null && dropSlot.owner != null)
        {
            if (dropSlot.slotKind == ActionBarSlot.SlotKind.Item && originalParentBeforeEquip == null)
                originalParentBeforeEquip = parentAfterDrag;
            dropSlot.owner.HandleDrop(dropSlot, gameObject);
            // continue to revert/cleanup below
        }
        else
        {
            // new: check for SellSlot in the raycasted hierarchy
            var sell = rayGo != null ? rayGo.GetComponentInParent<SellSlot>() : null;
            if (sell != null)
            {
                bool sold = sell.HandleDrop(gameObject);
                // if sold, stop further processing (object likely destroyed)
                if (sold)
                {
                    // cleanup local state and exit
                    draggingInstance = null;
                    isProcessing = false;
                    return;
                }
            }
        }

        if (cg != null) cg.blocksRaycasts = true;
        if (cg != null) cg.alpha = 1f;

        if (transform.parent == GetDragRoot() && parentAfterDrag != null)
        {
            transform.SetParent(parentAfterDrag, false);
            transform.localPosition = Vector3.zero;
        }
        else transform.localPosition = Vector3.zero;

        draggingInstance = null;
        isProcessing = false;
    }


    // ----------- Use() -----------
    public bool Use(PlayerStats stats = null, ItemSO overrideItem = null)
    {
        ItemSO useItem = overrideItem ?? item;
        if (useItem == null) { Debug.LogWarning("[InventoryItem] Item is null."); return false; }
        if (!useItem.consumable) { Debug.Log($"[InventoryItem] '{useItem.itemName}' not consumable."); return false; }

        if (stats == null) stats = FindFirstObjectByType<PlayerStats>();
        if (stats == null) { Debug.LogError("[InventoryItem] PlayerStats not found!"); return false; }

        bool applied = false;

        // apply health
        if (useItem.restoreHealth > 0f && stats.currentHealth < stats.maxHealth)
        {
            stats.Heal(useItem.restoreHealth);
            applied = true;
        }

        // apply mana
        if (useItem.restoreMana > 0f && stats.currentMana < stats.maxMana)
        {
            stats.EnergyDrink(useItem.restoreMana);
            applied = true;
        }

        // apply armor/shield
        if (useItem.restoreArmor > 0f && stats.currentArmor < stats.maxArmor)
        {
            stats.ShieldForce(useItem.restoreArmor);
            applied = true;
        }

        if (!applied)
        {
            Debug.Log($"[InventoryItem] '{useItem.itemName}' had no effect.");
            return false;
        }

        // consume one from stack and report
        ReduceAmount(1);
        Debug.Log($"[InventoryItem] Used '{useItem.itemName}'. Remaining: {count}.");
        return true;
    }


    // ----------- Quick Equip / Quick Unequip (delegate) -----------
    public void OnPointerClick(PointerEventData e)
    {
        if (e.button != PointerEventData.InputButton.Right) return;
        if (isProcessing) return;
        isProcessing = true;

        var actionBar = FindFirstObjectByType<ActionBar>();
        if (actionBar == null) { Debug.LogError("[InventoryItem] ActionBar not found!"); isProcessing = false; return; }

        // if in action slot -> try unequip
        var actionSlot = GetComponentInParent<ActionBarSlot>();
        if (actionSlot != null && actionSlot.slotKind == ActionBarSlot.SlotKind.Item)
        {
            // block if cooldown overlay present
            foreach (Transform c in actionSlot.transform) if (c != null && c.name != null && c.name.StartsWith("CooldownOverlay")) { isProcessing = false; return; }

            bool ok = actionBar.TryUnequip(this);
            if (!ok) Debug.Log($"[InventoryItem] Unequip aborted (target full?) '{item?.itemName}'.");
            isProcessing = false;
            return;
        }

        // otherwise quick-equip
        bool equipped = actionBar.TryQuickEquip(this);
        if (!equipped) Debug.Log($"[InventoryItem] Quick-equip failed (no slot or full) '{item?.itemName}'.");
        isProcessing = false;
    }

    void OnValidate() { if (countText != null) UpdateCountText(); }
}
