using Unity.VisualScripting;
using UnityEngine;

public class EquipmentTagsData
{
    public enum Rarity
    {
        Common,
        Magic,
        Rare
    }

    public enum EquipSlotType
    {
        WeaponMain,
        WeaponOffhand,
        Accessory,
        Head,
        Chest,
        Legs,
        Hands,
        Feet
    }

    public enum ArmorTags
    {
        Heavy,
        Medium,
        Light
    }

    public enum WeaponTags
    {
        Axe,
        TwoHandedAxe,
        Mace,
        TwoHandedMace,
        Sword,
        TwoHandedSword,
        Dagger,
        Staff,
        Spear,
        Bow,
        Musket,
        Shield,
        Orb,
        Tome,
        Quiver
    }

    public enum AccessoryTags
    {
        //To Implement
    }

    public string[] equipSlotTypeString =
    {

        "WeaponMain",
        "WeaponOffhand",
        "Accessory",
        "Head",
        "Chest",
        "Legs",
        "Hands",
        "Feet"
    };

    public string[] armorTagsString =
    {
        "Heavy",
        "Medium",
        "Light"
    };

    public string[] weaponTagsString =
    {
        "Axe",
        "TwoHandedAxe",
        "Mace",
        "TwoHandedMace",
        "Sword",
        "TwoHandedSword",
        "Dagger",
        "Staff",
        "Spear",
        "Bow",
        "Musket",
        "Shield",
        "Orb",
        "Tome",
        "Quiver"
    };

    public string[] AccessoryTagsString =
    {
        //To Implement
    };
}
