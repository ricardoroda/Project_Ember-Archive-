using System;
using System.Collections.Generic;
using UnityEngine;

public class InventoryTestSpawner : MonoBehaviour
{
    [Serializable]
    public struct SpawnItemEntry
    {
        public ItemSO item;
        public int amount;
    }

    [Header("Items")]
    public List<SpawnItemEntry> itemsToSpawn = new List<SpawnItemEntry>();

    [Header("Equipment (armory)")]
    public List<EquipmentSO> equipmentsToSpawn = new List<EquipmentSO>();

    [Header("Optional - assign in inspector")]
    public InventoryManager inventoryManager;
    public ArmoryManager armoryManager;

    private void Start()
    {
        if (inventoryManager == null)
            inventoryManager = FindFirstObjectByType<InventoryManager>();

        if (armoryManager == null)
            armoryManager = FindFirstObjectByType<ArmoryManager>();

        SpawnAll();
    }

    public void SpawnAll()
    {
        SpawnItems();
        SpawnEquipments();
    }

    [ContextMenu("Spawn Items (Inspector)")]
    public void SpawnItems()
    {
        if (itemsToSpawn == null || itemsToSpawn.Count == 0) return;
        if (inventoryManager == null) return;

        foreach (var entry in itemsToSpawn)
        {
            if (entry.item == null) continue;
            int amount = Mathf.Max(1, entry.amount);
            inventoryManager.AddItem(entry.item, amount);
        }
    }

    [ContextMenu("Spawn Equipments (Inspector)")]
    public void SpawnEquipments()
    {
        if (equipmentsToSpawn == null || equipmentsToSpawn.Count == 0) return;
        if (armoryManager == null) return;

        foreach (var eq in equipmentsToSpawn)
        {
            if (eq == null) continue;
            armoryManager.AddEquipment(eq);
        }
    }
}
