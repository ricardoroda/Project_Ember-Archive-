using System.Collections.Generic;
using UnityEngine;
using static EquipmentRoller;
using UnityEngine.UI;

public class ArmoryManager : MonoBehaviour
{
    [Header("Armory slots")]
    public ArmorySlot[] armorySlots;
    public EquipmentSO[] currentBySlot;

    [Header("Equipment Item Prefab")]
    [SerializeField] private GameObject equipmentItemPrefab;
    public GameObject armoryTab;
    public GameObject armoryTabButton;

    [SerializeField] private EquipmentSO[] equipmentTest = new EquipmentSO[5];

    private void Awake()
    {
        if (armorySlots == null) armorySlots = new ArmorySlot[0];
        for (int i = 0; i < armorySlots.Length; i++)
        {
            if (armorySlots[i] != null)
                armorySlots[i].Setup(i, this);
        }

        for (int i = 0; i < 5; i++)
        {
            EquipmentSO newEquipment = gameObject.GetComponent<EquipmentRoller>().RollEquipment(1);
            equipmentTest[i] = newEquipment;
           AddEquipment(newEquipment);
        }
    }

    public void Update()
    {
        if (armoryTab.activeInHierarchy)
        {
            armoryTabButton.GetComponent<Image>().color = new Color(0.82f, 0.55f, 0.35f, 1f);
        }
        else
        {
            armoryTabButton.GetComponent<Image>().color = new Color(0.60f, 0.40f, 0.27f, 1f);
        }
    }


    //Add logic 
    public bool AddEquipment(EquipmentSO equipment)
    {
        if (equipment == null) return false;

        for (int i = 0; i < armorySlots.Length; i++)
        {
            var s = armorySlots[i];
            if (s == null) continue;
            if (s.IsEmpty())
            {
                SpawnEquipmentItemInSlot(equipment, s);
                return true;
            }
        }

        Debug.Log($"ArmoryManager: full {equipment.itemName}");
        return false;
    }

    public EquipmentItem SpawnEquipmentItemInSlot(EquipmentSO equipment, ArmorySlot slot)
    {
        if (equipment == null || slot == null)
        {
            Debug.LogWarning("ArmoryManager: equipment or slot is null.");
            return null;
        }

        if (equipmentItemPrefab == null)
        {
            Debug.LogError("ArmoryManager: equipmentItemPrefab not assigned.");
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

    // Remove item from a specific slot
    public EquipmentItem RemoveFromSlot(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= armorySlots.Length) return null;
        return armorySlots[slotIndex].RemoveItem();
    }

    public ArmorySlot[] GetSlots() => armorySlots;
    
}
