using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using UnityEngine.UI;

public class ActionBar : MonoBehaviour
{
    [Tooltip("Assign 8 slots: 0 = dash, 1-5 = abilities, 6-7 = items")]
    public ActionBarSlot[] slots;
    public AbilityHandler abilityHandler;
    public GameObject cooldownOverlayPrefab;
    public GameObject skillItemUIPrefab;

    [Header("Debug")]
    public bool debug = false;

    HashSet<InventoryItem> processing = new HashSet<InventoryItem>();

    InputSystemActions inputActions;
    InputAction[] abilityUse = new InputAction[8];
    System.Action<InputAction.CallbackContext>[] handlers = new System.Action<InputAction.CallbackContext>[8];

    void Awake()
    {
        if (abilityHandler == null) abilityHandler = FindFirstObjectByType<AbilityHandler>();
        if (slots == null) return;
        for (int i = 0; i < slots.Length; i++)
            if (slots[i] != null) { slots[i].slotIndex = i; slots[i].owner = this; }
    }

    void OnEnable()
    {
        inputActions = new InputSystemActions();
        abilityUse[0] = inputActions.Player.Dash;
        abilityUse[1] = inputActions.Player.Ability1;
        abilityUse[2] = inputActions.Player.Ability2;
        abilityUse[3] = inputActions.Player.Ability3;
        abilityUse[4] = inputActions.Player.Ability4;
        abilityUse[5] = inputActions.Player.Ability5;
        abilityUse[6] = inputActions.Player.Item1;
        abilityUse[7] = inputActions.Player.Item2;

        for (int i = 0; i < abilityUse.Length; i++)
        {
            int idx = i;
            handlers[i] = ctx => OnAbilityInput(idx);
            if (abilityUse[i] != null) { abilityUse[i].performed += handlers[i]; abilityUse[i].Enable(); }
        }

        InitializeActionBarState();
    }

    void OnDisable()
    {
        for (int i = 0; i < abilityUse.Length; i++)
            if (abilityUse[i] != null) { abilityUse[i].performed -= handlers[i]; abilityUse[i].Disable(); }
    }

    // initialize slots and sync abilities
    public void InitializeActionBarState()
    {
        if (slots == null || slots.Length == 0) return;
        for (int i = 0; i < slots.Length; i++)
        {
            var slot = slots[i];
            if (slot == null) continue;
            if (slot.transform.childCount > 0)
            {
                var skill = slot.GetComponentInChildren<SkillItem>();
                if (skill != null) BindSlotToAbility(i, skill);
                var inv = slot.GetComponentInChildren<InventoryItem>();
                if (inv != null) slot.SetItemInstance(inv);
            }
        }
        SyncWithAbilityHandler();
        ConsolidateActionBarStacks(); // defensive cleanup
    }

    public void SyncWithAbilityHandler()
    {
        if (abilityHandler == null) abilityHandler = FindFirstObjectByType<AbilityHandler>();
        if (abilityHandler == null || slots == null || abilityHandler.abilitiesPrefab == null) return;

        int len = Mathf.Min(slots.Length, abilityHandler.abilitiesPrefab.Length);
        for (int i = 0; i < len; i++)
        {
            var prefab = abilityHandler.abilitiesPrefab[i];
            var slot = slots[i];
            if (slot == null) continue;

            if (prefab == null)
            {
                var existingSkill = slot.GetComponentInChildren<SkillItem>();
                if (existingSkill != null) Destroy(existingSkill.gameObject);
                UnbindSlot(i);
                continue;
            }

            var present = slot.GetComponentInChildren<SkillItem>();
            if (present != null)
            {
                if (present.abilityPrefab == prefab) { BindSlotToAbility(i, present); continue; }
                Destroy(present.gameObject);
            }

            if (skillItemUIPrefab == null) { Debug.LogWarning("[ActionBar] skillItemUIPrefab missing for slot " + i); continue; }
            var ui = Instantiate(skillItemUIPrefab, slot.transform, false);
            var si = ui.GetComponent<SkillItem>();
            if (si == null) { Debug.LogWarning("[ActionBar] skillItemUIPrefab missing SkillItem"); Destroy(ui); continue; }
            si.abilityPrefab = prefab; si.parentAfterDrag = slot.transform;
            slot.SetItemInstance(si); BindSlotToAbility(i, si);
        }
    }

    // main drop entry
    public void HandleDrop(ActionBarSlot slot, GameObject dragged)
    {
        if (slot == null || dragged == null) return;
        var skill = dragged.GetComponentInParent<SkillItem>();
        var inv = dragged.GetComponentInParent<InventoryItem>();
        if (skill != null) HandleSkillDrop(slot, skill);
        else if (inv != null) HandleInventoryDrop(slot, inv);
    }

    void HandleSkillDrop(ActionBarSlot slot, SkillItem skill)
    {
        bool fromTree = skill.GetComponentInParent<SkillTreeSlot>() != null;
        if (!slot.AcceptsSkill(skill.skillType) || IsAbilityAlreadyEquipped(skill.abilityPrefab, slot.slotIndex) || SlotHasCooldown(slot))
        { ReturnToParent(skill.gameObject, skill.parentAfterDrag); return; }

        if (fromTree)
        {
            if (skillItemUIPrefab == null) { ReturnToParent(skill.gameObject, skill.parentAfterDrag); return; }
            var ui = Instantiate(skillItemUIPrefab, slot.transform, false);
            var si = ui.GetComponent<SkillItem>();
            if (si != null) { si.abilityPrefab = skill.abilityPrefab; si.parentAfterDrag = slot.transform; slot.SetItemInstance(si); BindSlotToAbility(slot.slotIndex, si); }
            return;
        }

        if (slot.IsEmpty()) { slot.SetItemInstance(skill); BindSlotToAbility(slot.slotIndex, skill); return; }

        var existingSkill = slot.GetComponentInChildren<SkillItem>();
        if (existingSkill != null)
        {
            Transform dest = skill.parentAfterDrag != null ? skill.parentAfterDrag : existingSkill.parentAfterDrag;
            existingSkill.transform.SetParent(dest, false);
            existingSkill.transform.localPosition = Vector3.zero;
            slot.SetItemInstance(skill);
            BindSlotToAbility(slot.slotIndex, skill);
        }
    }

    // small helpers to read immediate children
    InventoryItem GetImmediateInventoryItem(ActionBarSlot s, InventoryItem ignore = null)
    {
        if (s == null) return null;
        foreach (Transform c in s.transform) { if (c == null) continue; var it = c.GetComponent<InventoryItem>(); if (it != null && it != ignore) return it; }
        return null;
    }
    InventoryItem GetImmediateInventoryItemFromTransform(Transform parent, InventoryItem ignore = null)
    {
        if (parent == null) return null;
        foreach (Transform c in parent) { if (c == null) continue; var it = c.GetComponent<InventoryItem>(); if (it != null && it != ignore) return it; }
        return null;
    }

    ActionBarSlot FindActionSlotWithItem(ItemSO item, InventoryItem ignore = null)
    {
        if (item == null || slots == null) return null;
        for (int i = 0; i < slots.Length; i++)
        {
            var s = slots[i];
            if (s == null || s.slotKind != ActionBarSlot.SlotKind.Item) continue;
            var it = GetImmediateInventoryItem(s, ignore);
            if (it != null && it.item == item) return s;
        }
        return null;
    }

    Transform FindFirstEmptyInventorySlot()
    {
        var invMgr = FindFirstObjectByType<InventoryManager>();
        if (invMgr == null || invMgr.inventorySlots == null) return null;
        foreach (var slot in invMgr.inventorySlots) if (slot != null && slot.transform.childCount == 0) return slot.transform;
        return null;
    }

    Transform FindSuitableInventoryParent(ItemSO item, InventoryItem ignore = null)
    {
        var invMgr = FindFirstObjectByType<InventoryManager>();
        if (invMgr == null || invMgr.inventorySlots == null) return null;
        foreach (var slot in invMgr.inventorySlots)
        {
            if (slot == null) continue;
            var existing = GetImmediateInventoryItemFromTransform(slot.transform, ignore);
            if (existing != null && existing.item == item && item.stackable && existing.count < item.maxStack) return slot.transform;
        }
        foreach (var slot in invMgr.inventorySlots) if (slot != null && slot.transform.childCount == 0) return slot.transform;
        return null;
    }

    bool HandleInventoryDrop(ActionBarSlot slot, InventoryItem inv)
    {
        if (slot == null || inv == null) return false;

        if (processing.Contains(inv))
        {
            if (debug) Debug.Log($"HandleInventoryDrop: already processing {inv.name} - skipping.");
            ReturnToParent(inv.gameObject, inv.parentAfterDrag);
            return true;
        }
        processing.Add(inv);

        if (slot.slotKind != ActionBarSlot.SlotKind.Item || SlotHasCooldown(slot))
        {
            if (debug) Debug.Log("HandleInventoryDrop: slot invalid or cooldown.");
            StartCoroutine(ReleaseProcessing(inv)); ReturnToParent(inv.gameObject, inv.parentAfterDrag); return true;
        }

        var srcParent = inv.parentAfterDrag;
        var srcSlot = srcParent != null ? srcParent.GetComponent<ActionBarSlot>() : null;
        int srcIndex = srcSlot != null ? srcSlot.slotIndex : -1;
        if (debug) Debug.Log($"HandleInventoryDrop: slot={slot.slotIndex} srcIndex={srcIndex} item={inv.item?.itemName} count={inv.count}");

        // prevent duplication when moving within action bar
        if (srcIndex >= 0 && IsItemAlreadyEquipped(inv.item, srcIndex))
        {
            if (debug) Debug.Log("Blocked duplicate equip from action-slot.");
            StartCoroutine(ReleaseProcessing(inv)); ReturnToParent(inv.gameObject, inv.parentAfterDrag); return true;
        }

        var existingInv = GetImmediateInventoryItem(slot, inv);

        // if coming from inventory and item already equipped elsewhere, merge there first
        if (srcIndex < 0 && existingInv == null && inv.item != null && IsItemAlreadyEquipped(inv.item))
        {
            var otherSlot = FindActionSlotWithItem(inv.item, inv);
            var otherInv = otherSlot != null ? GetImmediateInventoryItem(otherSlot, inv) : null;
            if (otherInv != null && inv.item.stackable)
            {
                if (debug) Debug.Log("Merging into existing action-slot (prefer merge).");
                int leftover = MergeInto(otherInv, inv.count, inv);
                if (leftover <= 0) { if (srcSlot == null) inv.originalParentBeforeEquip = srcParent; StartCoroutine(ReleaseProcessing(inv)); Destroy(inv.gameObject, 0.01f); ConsolidateActionBarStacks(); return true; }
                inv.count = leftover; inv.UpdateCountText(); inv.parentAfterDrag = srcParent; StartCoroutine(ReleaseProcessing(inv)); ReturnToParent(inv.gameObject, srcParent); ConsolidateActionBarStacks(); return true;
            }
        }

        // empty -> equip (move instance)
        if (existingInv == null)
        {
            slot.SetItemInstance(inv);
            inv.parentAfterDrag = slot.transform;
            if (srcSlot == null) inv.originalParentBeforeEquip = srcParent;
            inv.transform.SetParent(slot.transform, false);
            inv.transform.localPosition = Vector3.zero;
            if (debug) Debug.Log("Equipped into empty action-slot.");
            StartCoroutine(ReleaseProcessing(inv)); ConsolidateActionBarStacks(); return true;
        }

        // same item -> merge
        if (existingInv.item == inv.item && inv.item != null && inv.item.stackable)
        {
            if (debug) Debug.Log("Merging into existing item in action slot.");
            int leftover = MergeInto(existingInv, inv.count, inv);
            if (leftover <= 0) { if (srcSlot == null) inv.originalParentBeforeEquip = srcParent; inv.parentAfterDrag = slot.transform; StartCoroutine(ReleaseProcessing(inv)); Destroy(inv.gameObject, 0.01f); ConsolidateActionBarStacks(); return true; }
            inv.count = leftover; inv.UpdateCountText(); inv.parentAfterDrag = srcParent; StartCoroutine(ReleaseProcessing(inv)); ReturnToParent(inv.gameObject, srcParent); ConsolidateActionBarStacks(); return true;
        }

        // different item -> swap
        if (debug) Debug.Log("Swap: moving existing back to source and equipping incoming.");
        Transform dest = srcParent != null ? srcParent : inv.transform.root;
        existingInv.transform.SetParent(dest, false); existingInv.transform.localPosition = Vector3.zero; existingInv.parentAfterDrag = dest;

        slot.SetItemInstance(inv);
        inv.parentAfterDrag = slot.transform;
        if (srcSlot == null) inv.originalParentBeforeEquip = srcParent;
        inv.transform.SetParent(slot.transform, false); inv.transform.localPosition = Vector3.zero;

        StartCoroutine(ReleaseProcessing(inv)); ConsolidateActionBarStacks(); return true;
    }

    int MergeInto(InventoryItem existingInv, int amount, InventoryItem sourceInv)
    {
        if (existingInv == null || existingInv.item == null || !existingInv.item.stackable) return amount;
        int space = existingInv.item.maxStack - existingInv.count;
        int toAdd = Mathf.Min(space, amount);
        if (toAdd > 0) { existingInv.count += toAdd; existingInv.UpdateCountText(); }
        return amount - toAdd;
    }

    void ReturnToParent(GameObject go, Transform parent) { if (go == null || parent == null) return; go.transform.SetParent(parent, false); go.transform.localPosition = Vector3.zero; }

    IEnumerator ReleaseProcessing(InventoryItem inv) { yield return null; if (processing.Contains(inv)) processing.Remove(inv); }

    // quick-equip: merge into existing, then fill empties (move original when appropriate)
    public bool TryQuickEquip(InventoryItem inv)
    {
        if (inv == null || inv.item == null || slots == null) return false;
        if (processing.Contains(inv)) { if (debug) Debug.Log($"TryQuickEquip: already processing {inv.name}"); return false; }
        processing.Add(inv);
        if (debug) Debug.Log($"TryQuickEquip start: item={inv.item.itemName} count={inv.count}");

        int remaining = inv.count;
        ItemSO itemSO = inv.item;

        // merge into existing action slots
        for (int i = 0; i < slots.Length && remaining > 0; i++)
        {
            var slot = slots[i];
            if (slot == null || slot.slotKind != ActionBarSlot.SlotKind.Item) continue;
            var existing = GetImmediateInventoryItem(slot, inv);
            if (existing != null && existing.item == itemSO && itemSO.stackable)
            {
                int space = itemSO.maxStack - existing.count;
                int toAdd = Mathf.Min(space, remaining);
                if (toAdd > 0) { existing.count += toAdd; existing.UpdateCountText(); remaining -= toAdd; }
            }
        }

        // sync source count
        inv.count = remaining; inv.UpdateCountText();
        if (remaining <= 0) { StartCoroutine(ReleaseProcessing(inv)); Destroy(inv.gameObject, 0.01f); if (debug) Debug.Log("TryQuickEquip: fully merged into existing action slots."); ConsolidateActionBarStacks(); return true; }

        // fill empty action slots, moving original if it exactly matches leftover
        for (int i = 0; i < slots.Length && remaining > 0; i++)
        {
            var slot = slots[i];
            if (slot == null || slot.slotKind != ActionBarSlot.SlotKind.Item || !slot.IsEmpty()) continue;
            int moveAmount = Mathf.Min(itemSO.maxStack, remaining);

            if (inv.count == remaining && moveAmount == remaining)
            {
                slot.SetItemInstance(inv);
                inv.parentAfterDrag = slot.transform;
                inv.transform.SetParent(slot.transform, false);
                inv.transform.localPosition = Vector3.zero;
                inv.UpdateCountText();
                remaining -= moveAmount;
                if (remaining <= 0) { StartCoroutine(ReleaseProcessing(inv)); if (debug) Debug.Log("TryQuickEquip: moved original to empty slot."); ConsolidateActionBarStacks(); return true; }
            }
            else
            {
                GameObject cloneGO = Instantiate(inv.gameObject, slot.transform, false);
                InventoryItem cloneInv = cloneGO.GetComponent<InventoryItem>();
                if (cloneInv != null) { cloneInv.count = moveAmount; cloneInv.UpdateCountText(); cloneInv.parentAfterDrag = slot.transform; cloneGO.transform.localPosition = Vector3.zero; }
                remaining -= moveAmount;
                inv.count = remaining; inv.UpdateCountText();
            }
        }

        if (remaining <= 0) { StartCoroutine(ReleaseProcessing(inv)); Destroy(inv.gameObject, 0.01f); if (debug) Debug.Log("TryQuickEquip: fully moved to action-bar."); ConsolidateActionBarStacks(); return true; }

        // leftover stays in inventory
        inv.count = remaining; inv.UpdateCountText(); StartCoroutine(ReleaseProcessing(inv));
        if (debug) Debug.Log($"TryQuickEquip: partial equip done, leftover={remaining}");
        ConsolidateActionBarStacks();
        return true;
    }

    // unequip from action slot into inventory 
    public bool TryUnequip(InventoryItem inv)
    {
        if (inv == null) return false;
        var actionSlot = inv.GetComponentInParent<ActionBarSlot>();
        if (actionSlot == null || actionSlot.slotKind != ActionBarSlot.SlotKind.Item) return false;
        if (debug) Debug.Log($"TryUnequip: item={inv.item?.itemName} count={inv.count}");

        Transform target = FindSuitableInventoryParent(inv.item, inv) ?? inv.originalParentBeforeEquip ?? inv.parentAfterDrag;
        if (target == null)
        {
            inv.transform.SetParent(inv.transform.root, false); inv.transform.localPosition = Vector3.zero; inv.parentAfterDrag = inv.transform.parent; inv.originalParentBeforeEquip = null;
            if (debug) Debug.Log("Unequip -> placed at root (no inventory available)."); return true;
        }

        var existing = GetImmediateInventoryItemFromTransform(target, inv);
        if (existing != null && inv.item != null && inv.item.stackable)
        {
            int leftover = MergeInto(existing, inv.count, inv);
            if (leftover == inv.count)
            {
                inv.transform.SetParent(actionSlot.transform, false); inv.transform.localPosition = Vector3.zero;
                if (debug) Debug.Log("Unequip aborted: inventory stack full."); return false;
            }
            if (leftover <= 0)
            {
                inv.parentAfterDrag = target; inv.originalParentBeforeEquip = null; Destroy(inv.gameObject, 0.01f);
                if (debug) Debug.Log("Unequip -> fully merged and destroyed source."); return true;
            }
            Transform empty = FindFirstEmptyInventorySlot();
            if (empty != null)
            {
                inv.count = leftover; inv.UpdateCountText(); inv.transform.SetParent(empty, false); inv.transform.localPosition = Vector3.zero; inv.parentAfterDrag = empty; inv.originalParentBeforeEquip = null;
                if (debug) Debug.Log("Unequip -> partially merged, leftover moved to empty slot."); return true;
            }
            inv.transform.SetParent(actionSlot.transform, false); inv.transform.localPosition = Vector3.zero;
            if (debug) Debug.Log("Unequip aborted: leftover exists and no empty slot available."); return false;
        }

        inv.transform.SetParent(target, false); inv.transform.localPosition = Vector3.zero; inv.parentAfterDrag = target; inv.originalParentBeforeEquip = null;
        if (debug) Debug.Log("Unequip -> placed into empty inventory slot."); return true;
    }

    public bool IsItemAlreadyEquipped(ItemSO item, int checkedSlotIndex)
    {
        if (item == null || slots == null) return false;
        for (int i = 0; i < slots.Length; i++)
        {
            if (i == checkedSlotIndex) continue;
            var s = slots[i];
            if (s == null || s.slotKind != ActionBarSlot.SlotKind.Item) continue;
            var it = s.GetComponentInChildren<InventoryItem>();
            if (it != null && it.item != null && it.item == item) return true;
        }
        return false;
    }
    public bool IsItemAlreadyEquipped(ItemSO item) => IsItemAlreadyEquipped(item, -1);

    public bool IsAbilityAlreadyEquipped(GameObject abilityPrefab, int checkedSlotIndex)
    {
        if (abilityPrefab == null || abilityHandler == null || abilityHandler.abilitiesPrefab == null) return false;
        for (int i = 0; i < abilityHandler.abilitiesPrefab.Length; i++)
            if (i != checkedSlotIndex && abilityHandler.abilitiesPrefab[i] == abilityPrefab) return true;
        return false;
    }
    public bool IsAbilityAlreadyEquipped(GameObject abilityPrefab) => IsAbilityAlreadyEquipped(abilityPrefab, -1);

    bool SlotHasCooldown(ActionBarSlot s)
    {
        if (s == null) return false;
        foreach (Transform c in s.transform) if (c != null && c.name != null && c.name.StartsWith("CooldownOverlay")) return true;
        return false;
    }

    // equip from skill tree (SkillItem uses this)
    public void EquipFromTreeShortcut(SkillTreeSlot treeSlot, SkillItem skill)
    {
        if (treeSlot == null || skill == null || slots == null) return;
        for (int i = 0; i < slots.Length; i++)
        {
            var slot = slots[i];
            if (slot == null || !slot.AcceptsSkill(skill.skillType) || !slot.IsEmpty() || IsAbilityAlreadyEquipped(skill.abilityPrefab)) continue;
            if (skillItemUIPrefab != null)
            {
                var ui = Instantiate(skillItemUIPrefab, slot.transform, false);
                var si = ui.GetComponent<SkillItem>();
                if (si != null) { si.abilityPrefab = skill.abilityPrefab; si.parentAfterDrag = slot.transform; slot.SetItemInstance(si); BindSlotToAbility(i, si); return; }
            }
            var clone = Instantiate(skill.gameObject, slot.transform, false);
            var cloneSi = clone.GetComponent<SkillItem>();
            if (cloneSi != null) { cloneSi.parentAfterDrag = slot.transform; slot.SetItemInstance(cloneSi); BindSlotToAbility(i, cloneSi); return; }
            Destroy(clone);
        }
    }

    public void BindSlotToAbility(int slotIndex, SkillItem skill)
    {
        if (abilityHandler == null) abilityHandler = FindFirstObjectByType<AbilityHandler>();
        if (abilityHandler == null) { Debug.LogError("[BindSlot] abilityHandler missing"); return; }
        if (abilityHandler.abilitiesPrefab == null || slotIndex < 0 || slotIndex >= abilityHandler.abilitiesPrefab.Length) { Debug.LogError("[BindSlot] slotIndex out of range or abilitiesPrefab missing"); return; }

        // remove duplicates
        for (int i = 0; i < abilityHandler.abilitiesPrefab.Length; i++)
        {
            if (i == slotIndex) continue;
            if (abilityHandler.abilitiesPrefab[i] == skill?.abilityPrefab)
            {
                abilityHandler.abilitiesPrefab[i] = null; abilityHandler.abilityData[i] = null;
                if (abilityHandler.abilities[i] != null) Destroy(abilityHandler.abilities[i]);
                abilityHandler.abilities[i] = null;
                if (i < slots.Length && slots[i] != null && slots[i].transform.childCount > 0) Destroy(slots[i].transform.GetChild(0).gameObject);
            }
        }

        if (skill == null || skill.abilityPrefab == null)
        {
            abilityHandler.abilitiesPrefab[slotIndex] = null; abilityHandler.abilityData[slotIndex] = null;
            if (abilityHandler.abilities[slotIndex] != null) Destroy(abilityHandler.abilities[slotIndex]);
            abilityHandler.abilities[slotIndex] = null; return;
        }

        abilityHandler.abilitiesPrefab[slotIndex] = skill.abilityPrefab;
        var abBehaviour = skill.abilityPrefab.GetComponent<AbilityBehaviour>();
        if (abBehaviour != null) abilityHandler.abilityData[slotIndex] = abBehaviour.GetAbilitySO();

        abilityHandler.SetUpAbilityInstance(slotIndex);
    }

    public void UnbindSlot(int slotIndex)
    {
        if (abilityHandler == null) abilityHandler = FindFirstObjectByType<AbilityHandler>();
        if (abilityHandler == null) return;
        if (abilityHandler.abilitiesPrefab == null || slotIndex < 0 || slotIndex >= abilityHandler.abilitiesPrefab.Length) return;

        abilityHandler.abilitiesPrefab[slotIndex] = null; abilityHandler.abilityData[slotIndex] = null;
        if (abilityHandler.abilities[slotIndex] != null) Destroy(abilityHandler.abilities[slotIndex]);
        abilityHandler.abilities[slotIndex] = null;

        if (slots != null && slotIndex < slots.Length && slots[slotIndex] != null)
            if (slots[slotIndex].transform.childCount > 0) Destroy(slots[slotIndex].transform.GetChild(0).gameObject);
    }

    void OnAbilityInput(int index)
    {
        if (slots == null || index < 0 || index >= slots.Length) return;
        var slot = slots[index]; if (slot == null) return;

        if (slot.slotKind == ActionBarSlot.SlotKind.Item)
        {
            if (SlotHasCooldown(slot)) return;
            var inv = slot.GetComponentInChildren<InventoryItem>();
            if (inv == null) return;
            bool used = inv.Use();
            if (!used) return;
            StartCoroutine(ShowCooldownCoroutine(slot, inv.item != null && inv.item.cooldownFlag ? inv.item.cooldown : 5f));
            return;
        }

        if (abilityHandler == null || abilityHandler.abilityData == null) return;
        if (index < 0 || index >= abilityHandler.abilityData.Length) return;
        var data = abilityHandler.abilityData[index]; if (data == null) return;
        var playerStats = abilityHandler.GetComponent<PlayerStats>();
        float duration = data.cooldownFlag ? data.cooldown / playerStats.currentCooldownReduction : (data.fireRate != 0 ? 1f / data.fireRate : 0f);
        if (duration <= 0f) return;
        foreach (Transform c in slot.transform) if (c != null && c.name != null && c.name.StartsWith("CooldownOverlay")) return;
        StartCoroutine(ShowCooldownCoroutine(slot, duration));
    }

    IEnumerator ShowCooldownCoroutine(ActionBarSlot slot, float duration)
    {
        if (cooldownOverlayPrefab == null || slot == null) yield break;
        if (SlotHasCooldown(slot)) yield break;
        var overlay = Instantiate(cooldownOverlayPrefab, slot.transform, false);
        overlay.name = "CooldownOverlay";
        overlay.transform.SetAsLastSibling();
        var fill = overlay.GetComponentInChildren<Image>();
        var tmp = overlay.GetComponentInChildren<TextMeshProUGUI>();
        float t = duration;
        while (t > 0f)
        {
            t -= Time.deltaTime;
            if (fill != null) fill.fillAmount = Mathf.Clamp01(t / duration);
            if (tmp != null) tmp.text = Mathf.CeilToInt(t).ToString();
            yield return null;
        }
        Destroy(overlay);
    }

    // consolidate duplicates on action bar and push leftovers to inventory
    void ConsolidateActionBarStacks()
    {
        if (slots == null) return;
        var seen = new Dictionary<ItemSO, InventoryItem>();
        for (int i = 0; i < slots.Length; i++)
        {
            var s = slots[i];
            if (s == null || s.slotKind != ActionBarSlot.SlotKind.Item) continue;
            var inv = GetImmediateInventoryItem(s, null);
            if (inv == null || inv.item == null) continue;
            if (!seen.ContainsKey(inv.item)) { seen[inv.item] = inv; continue; }

            var primary = seen[inv.item];
            int toAdd = Mathf.Min(primary.item.maxStack - primary.count, inv.count);
            if (toAdd > 0) { primary.count += toAdd; primary.UpdateCountText(); inv.count -= toAdd; inv.UpdateCountText(); }
            if (inv.count <= 0) { Destroy(inv.gameObject); continue; }

            Transform target = FindSuitableInventoryParent(inv.item, inv);
            if (target != null)
            {
                var existing = GetImmediateInventoryItemFromTransform(target, inv);
                if (existing != null && existing.item == inv.item && existing.item.stackable)
                {
                    int toAdd2 = Mathf.Min(existing.item.maxStack - existing.count, inv.count);
                    if (toAdd2 > 0) { existing.count += toAdd2; existing.UpdateCountText(); inv.count -= toAdd2; inv.UpdateCountText(); }
                }
                if (inv.count <= 0) Destroy(inv.gameObject); else inv.transform.SetParent(target, false);
            }
        }
    }
}
