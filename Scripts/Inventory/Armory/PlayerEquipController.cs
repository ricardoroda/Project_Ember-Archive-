using System;
using UnityEngine;
using UnityEngine.UI;

public class PlayerEquipController : MonoBehaviour
{
    public EquipSlot[] equipSlots;
    public EquipmentSO[] currentBySlot;
    public Button equipButton;
    public GameObject equipTab;

    [Header("Equipment Item Prefab")]
    [SerializeField] private GameObject equipmentItemPrefab;

    private void Awake()
    {
        PlayerStats playerStats = FindFirstObjectByType<PlayerStats>();
        if (playerStats == null)
        {
            Debug.LogError("PlayerStats not found in scene.");
            enabled = false;
            return;
        }

        if (equipSlots == null || equipSlots.Length == 0)
        {
            Debug.LogError("Slots not defined.");
            enabled = false;
            return;
        }

        currentBySlot = new EquipmentSO[equipSlots.Length];

        for (int i = 0; i < equipSlots.Length; i++)
        {
            var slot = equipSlots[i];
            if (slot == null) continue;
            if (slot.slotIndex < 0) slot.slotIndex = i;

            int idx = i;
            slot.OnEquip += (eq, s) =>
            {
                if (eq == null) return;
                if (currentBySlot[idx] != null && currentBySlot[idx] != eq)
                    playerStats.RemoveEquipmentBonus(currentBySlot[idx]);

                currentBySlot[idx] = eq;
                playerStats.AddEquipmentBonus(eq);
            };

            slot.OnUnequip += (eq, s) =>
            {
                if (currentBySlot[idx] != null)
                {
                    playerStats.RemoveEquipmentBonus(currentBySlot[idx]);
                    currentBySlot[idx] = null;
                }
                else if (eq != null)
                {
                    playerStats.RemoveEquipmentBonus(eq);
                }
            };

            // supply placement rule function to the slot
            slot.CanPlace = (equipmentSO, targetSlotIndex) => CanPlaceEquipment(equipmentSO, targetSlotIndex);
        }
    }

    public void Update()
    {
        if (equipTab.activeInHierarchy)
        {
            equipButton.GetComponent<Image>().color = new Color(0.82f, 0.55f, 0.35f, 1f);
        }
        else
        {
            equipButton.GetComponent<Image>().color = new Color(0.60f, 0.40f, 0.27f, 1f);
        }
    }

    private int FindSlotIndex(EquipmentTagsData.EquipSlotType type)
    {
        for (int i = 0; i < equipSlots.Length; i++)
            if (equipSlots[i] != null && equipSlots[i].accepts == type)
                return i;
        return -1;
    }

    private EquipmentTagsData.WeaponTags? GetWeaponTagInSlot(int slotIdx)
    {
        if (slotIdx < 0 || slotIdx >= currentBySlot.Length) return null;
        var so = currentBySlot[slotIdx] as WeaponSO;
        return so != null ? (EquipmentTagsData.WeaponTags?)so.weaponType : null;
    }

    // helpers
    private bool IsTwoHanded(EquipmentTagsData.WeaponTags t) =>
        t == EquipmentTagsData.WeaponTags.TwoHandedAxe ||
        t == EquipmentTagsData.WeaponTags.TwoHandedMace ||
        t == EquipmentTagsData.WeaponTags.TwoHandedSword;

    private bool IsOneHandedWeapon(EquipmentTagsData.WeaponTags t) =>
        t == EquipmentTagsData.WeaponTags.Axe ||
        t == EquipmentTagsData.WeaponTags.Mace ||
        t == EquipmentTagsData.WeaponTags.Sword ||
        t == EquipmentTagsData.WeaponTags.Dagger ||
        t == EquipmentTagsData.WeaponTags.Spear;

    // offhand rules when a main is present
    private bool OffhandAllowedWithMain(EquipmentTagsData.WeaponTags mainTag, EquipmentTagsData.WeaponTags off)
    {
        // two-handed main blocks any off-hand
        if (IsTwoHanded(mainTag)) return false;

        if (mainTag == EquipmentTagsData.WeaponTags.Bow || mainTag == EquipmentTagsData.WeaponTags.Musket)
        {
            // only quiver allowed
            return off == EquipmentTagsData.WeaponTags.Quiver;
        }

        if (mainTag == EquipmentTagsData.WeaponTags.Staff)
        {
            // only orb or tome allowed (no shield, no weapon)
            return off == EquipmentTagsData.WeaponTags.Orb || off == EquipmentTagsData.WeaponTags.Tome;
        }

        // main is one-handed or other: allow
        // allow another one-handed weapon (dual-wield), shield, orb/tome
        if (IsOneHandedWeapon(off)) return true;
        if (off == EquipmentTagsData.WeaponTags.Shield) return true;
        if (off == EquipmentTagsData.WeaponTags.Orb || off == EquipmentTagsData.WeaponTags.Tome) return true;

        // quiver only allowed with bow/musket
        return false;
    }

    // main rules when offhand is present
    private bool MainAllowedWithOffhand(EquipmentTagsData.WeaponTags candidateMain, EquipmentTagsData.WeaponTags offTag)
    {
        // two-handed main cannot be placed if offhand is occupied
        if (IsTwoHanded(candidateMain)) return false;

        if (offTag == EquipmentTagsData.WeaponTags.Quiver)
        {
            // quiver pairs only with bow or musket as main
            return candidateMain == EquipmentTagsData.WeaponTags.Bow ||
                   candidateMain == EquipmentTagsData.WeaponTags.Musket;
        }

        if (offTag == EquipmentTagsData.WeaponTags.Orb || offTag == EquipmentTagsData.WeaponTags.Tome)
        {
            // orb/tome allowed with staff or one-handed weapons, not with bow/musket
            if (candidateMain == EquipmentTagsData.WeaponTags.Bow || candidateMain == EquipmentTagsData.WeaponTags.Musket) return false;
            return candidateMain == EquipmentTagsData.WeaponTags.Staff || IsOneHandedWeapon(candidateMain);
        }

        if (offTag == EquipmentTagsData.WeaponTags.Shield)
        {
            // shield not allowed with bow/musket nor with staff
            if (candidateMain == EquipmentTagsData.WeaponTags.Bow || candidateMain == EquipmentTagsData.WeaponTags.Musket) return false;
            if (candidateMain == EquipmentTagsData.WeaponTags.Staff) return false;
            return IsOneHandedWeapon(candidateMain);
        }

        if (IsOneHandedWeapon(offTag))
        {
            // dual-wield requires both to be one-handed
            return IsOneHandedWeapon(candidateMain);
        }

        // default deny unknown combinations
        return false;
    }

    // main entry point used by slots
    private bool CanPlaceEquipment(EquipmentSO eq, int targetIdx)
    {
        if (eq == null) return false;

        int mainIdx = FindSlotIndex(EquipmentTagsData.EquipSlotType.WeaponMain);
        int offIdx  = FindSlotIndex(EquipmentTagsData.EquipSlotType.WeaponOffhand);

        var targetSlot = equipSlots[targetIdx];
        var placingW = eq as WeaponSO;
        EquipmentTagsData.WeaponTags? placingTag = placingW != null ? (EquipmentTagsData.WeaponTags?)placingW.weaponType : null;
        EquipmentTagsData.WeaponTags? mainTag = GetWeaponTagInSlot(mainIdx);
        EquipmentTagsData.WeaponTags? offTag  = GetWeaponTagInSlot(offIdx);

        // placing into offhand: check main and placed tag compatibility
        if (targetSlot.accepts == EquipmentTagsData.EquipSlotType.WeaponOffhand)
        {
            // if no main weapon, allow (we allow placing accessories/weapons on empty offhand)
            if (mainTag == null) return true;

            // placing not a weapon (null tag) -> allow unless main is two-handed
            if (placingTag == null)
            {
                if (IsTwoHanded(mainTag.Value)) return false;
                return true;
            }

            return OffhandAllowedWithMain(mainTag.Value, placingTag.Value);
        }

        // placing into main: check offhand compatibility
        if (targetSlot.accepts == EquipmentTagsData.EquipSlotType.WeaponMain)
        {
            if (placingTag == null) return true;

            // offhand empty -> allow (two-handed will occupy off-hand implicitly)
            if (offTag == null) return true;

            // offTag present -> check compatibility
            return MainAllowedWithOffhand(placingTag.Value, offTag.Value);
        }

        // other slots: allow by default
        return true;
    }

    public EquipmentItem SpawnEquipmentItemInSlot(EquipmentSO equipment, EquipSlot slot)
    {
        if (equipment == null || slot == null)
        {
            Debug.LogWarning("PlayerEquipController: equipment or slot is null.");
            return null;
        }

        if (equipmentItemPrefab == null)
        {
            Debug.LogError("PlayerEquipController: equipmentItemPrefab not assigned.");
            return null;
        }

        GameObject go = Instantiate(equipmentItemPrefab, slot.transform);
        go.transform.localPosition = Vector3.zero;
        var ei = go.GetComponent<EquipmentItem>();
        if (ei == null)
        {
            Debug.LogError("equipmentItemPrefab dont have component EquipmentItem.");
            Destroy(go);
            return null;
        }

        ei.Initialise(equipment);
        ei.parentAfterDrag = slot.transform;
        return ei;
    }
}
