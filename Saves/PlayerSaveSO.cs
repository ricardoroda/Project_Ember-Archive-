using UnityEngine;
using System.Collections.Generic;
using static ClassListData;
using System;
using System.IO;

[CreateAssetMenu(fileName = "PlayerSaveSO", menuName = "Scriptable Objects/PlayerSaveSO")]

[System.Serializable]
public class PlayerItemSave
{
    public int index;
    public int identifier;
    public string itemSOString;

    public PlayerItemSave(int index, int identifier, string itemSOString)
    {
        this.index = index;
        this.identifier = identifier;
        this.itemSOString = itemSOString;
    }
}

[System.Serializable]
public class PlayerItemSaveList
{
    public List<PlayerItemSave> list;

    public PlayerItemSaveList()
    {
        list = new List<PlayerItemSave>();
    }
}

[System.Serializable]
public class PlayerSkillTreeSave
{
    public List<bool> list;

    public PlayerSkillTreeSave()
    {
        list = new List<bool>();
    }
}

[System.Serializable]
public class PlayerActionBarSave
{
    public List<int> list;

    public PlayerActionBarSave()
    {
        list = new List<int>();
    }
}

[System.Serializable]
public class PlayerSaveSO : ScriptableObject
{
    //Save Logistics
    public string saveFileName;
    public bool saveFileExists = false;

    //Base Player Stats
    public int playerClassIndex;
    public int level;
    public int xp;
    public int souls;
    public int gold;

    //Ability & Skill Tree
    public PlayerSkillTreeSave skillTree;
    public PlayerActionBarSave actionBar;

    //Equipment & Inventory
    public PlayerItemSaveList inventoryItems;
    public PlayerItemSaveList armoryEquipment;
    public PlayerItemSaveList equippedSlotsEquipment;
    
    public bool CheckSaveFileExistence(string saveDirectoryPath)
    {
        string saveFilePath = saveDirectoryPath + "/" + saveFileName;

        if (!Directory.Exists(saveDirectoryPath))
        {
            Debug.LogError("Directory doesn't exist!");
        }
        else if (File.Exists(saveFilePath))
        {
            saveFileExists = true;
            return true;
        }
        else
        {
            saveFileExists = false;
            return false;
        }

        return false;
    }

    public PlayerItemSave SaveItem(int index, int count, ItemSO itemSO)
    {
        string itemString = JsonUtility.ToJson(itemSO, true);

        return new PlayerItemSave(index, count, itemString);
    }

    public PlayerItemSave SaveEquipment(int index, EquipmentSO equipmentSO)
    {
        int equipmentType = -1;
        string equipmentSOString = null;

        if(equipmentSO != null)
        {
            if(equipmentSO is WeaponSO)
            {
                equipmentType = 0;

            }

            if(equipmentSO is ArmorSO)
            {
                equipmentType = 1;
            }

            if (equipmentSO is AccessorySO)
            {
                equipmentType = 2;
            }

            if (equipmentType != -1)
            {
                equipmentSOString = JsonUtility.ToJson(equipmentSO,true);
            }
            else
            {
                Debug.LogError("Couldn't Save Equipment");
            }
        }
        return new PlayerItemSave(index, equipmentType, equipmentSOString);
    }

    public static KeyValuePair<int, Tuple<int, ItemSO>> LoadItem(PlayerItemSave itemSlotSave)
    {
        ItemSO itemSO = ScriptableObject.CreateInstance<ItemSO>();
        int slotIndex = itemSlotSave.index;
        int itemCount = itemSlotSave.identifier;
        JsonUtility.FromJsonOverwrite(itemSlotSave.itemSOString, itemSO);

        return new KeyValuePair<int, Tuple<int, ItemSO>>(slotIndex, new Tuple<int, ItemSO>(itemCount, itemSO));
    }

    public static KeyValuePair<int, EquipmentSO> LoadEquiment(PlayerItemSave equipmentSlotSave)
    {
        int slotIndex = equipmentSlotSave.index;
        EquipmentSO equipmentSO = null;
        switch (equipmentSlotSave.identifier)
        {
            case 0:
                equipmentSO = ScriptableObject.CreateInstance<WeaponSO>();
                JsonUtility.FromJsonOverwrite(equipmentSlotSave.itemSOString, equipmentSO as WeaponSO);
                break;
            case 1:
                equipmentSO = ScriptableObject.CreateInstance<ArmorSO>();
                JsonUtility.FromJsonOverwrite(equipmentSlotSave.itemSOString, equipmentSO as ArmorSO);
                break;
            case 2:
                equipmentSO = ScriptableObject.CreateInstance<AccessorySO>();
                JsonUtility.FromJsonOverwrite(equipmentSlotSave.itemSOString, equipmentSO as AccessorySO);
                break;
        }

        return new KeyValuePair<int, EquipmentSO> (slotIndex, equipmentSO);
    }

    public void ResetSave()
    {
        //Save Logistics
        saveFileExists = false;

        //Base Player Stats
        playerClassIndex = 0;
        level = 1;
        xp = 0;
        souls = 0;
        gold = 0;

        //Ability & Skill Tree
        skillTree = new PlayerSkillTreeSave();
        actionBar = new PlayerActionBarSave();

        //Equipment & Inventory
        inventoryItems = new PlayerItemSaveList();
        armoryEquipment = new PlayerItemSaveList();
        equippedSlotsEquipment = new PlayerItemSaveList();
}
}
