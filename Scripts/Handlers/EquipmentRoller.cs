// using NUnit.Framework;
// using NUnit.Framework.Interfaces;
// using NUnit.Framework.Internal;
using System;
using System.Collections.Generic;
// using System.Linq;
// using Unity.Mathematics;
// using Unity.VisualScripting;
using UnityEngine;
using static EquipmentTagsData;
// using static UnityEditor.IMGUI.Controls.PrimitiveBoundsHandle;
// using static UnityEditor.Progress;
// using static UnityEngine.Rendering.DebugUI;

public static class WeightedEquipmentTable
{
    public static Dictionary<int, Dictionary<int, double>> rarityWeightTable = new Dictionary<int, Dictionary<int, double>>
    {
        // (Item Tier (Number of Affixes, Probability))
        {1, new Dictionary<int, double>
            {
                { 0, 30 },
                { 1, 40 },
                { 2, 20 },
                { 3, 8 },
                { 4, 2 }
            }
        },
        {2, new Dictionary<int, double>
            {
                { 0, 20 },
                { 1, 30 },
                { 2, 30 },
                { 3, 10 },
                { 4, 10 }
            }
        },
        {3, new Dictionary<int, double>
            {
                { 0, 10 },
                { 1, 10 },
                { 2, 30 },
                { 3, 30 },
                { 4, 20 }
            }
        },
    };

    public static Dictionary<EquipSlotType, double> equipSlotWeightTable = new Dictionary<EquipSlotType, double>
    {
        { EquipSlotType.WeaponMain, 25 },
        { EquipSlotType.WeaponOffhand, 15 },
        { EquipSlotType.Accessory, 20 },
        { EquipSlotType.Head, 10 },
        { EquipSlotType.Chest, 10 },
        { EquipSlotType.Legs, 10 },
        { EquipSlotType.Hands, 10 },
        { EquipSlotType.Feet, 10 }
    };

    public static Dictionary<ArmorTags, double> armorTypeWeightTable = new Dictionary<ArmorTags, double>
    {
        { ArmorTags.Heavy, 1 },
        { ArmorTags.Medium, 1 },
        { ArmorTags.Light, 1 }
    };

    public static Dictionary<WeaponTags, double> mainHandWeightTable = new Dictionary<WeaponTags, double>
    {
        { WeaponTags.Axe, 30 },
        { WeaponTags.TwoHandedAxe, 40 },
        { WeaponTags.Mace, 30 },
        { WeaponTags.TwoHandedMace, 40 },
        { WeaponTags.Sword, 30 },
        { WeaponTags.TwoHandedSword, 40 },
        { WeaponTags.Dagger, 30 },
        { WeaponTags.Staff, 40 },
        { WeaponTags.Spear, 40 },
        { WeaponTags.Bow, 40 },
        { WeaponTags.Musket, 40 }
    };

    public static Dictionary<WeaponTags, double> offHandWeightTable = new Dictionary<WeaponTags, double>
    {
        { WeaponTags.Axe, 15 },
        { WeaponTags.Mace, 15 },
        { WeaponTags.Sword, 15 },
        { WeaponTags.Dagger, 15 },
        { WeaponTags.Shield, 20 },
        { WeaponTags.Orb, 20 },
        { WeaponTags.Tome, 20 },
        { WeaponTags.Quiver, 20 }
    };

}

public class EquipmentRoller : MonoBehaviour
{
    [Header("Weapons SO: Axes")]
    public WeaponSO[] axes;

    [Header("Weapons SO: Two-Handed Axes")]
    public WeaponSO[] twoHandedAxes;

    [Header("Weapons SO: Maces")]
    public WeaponSO[] maces;

    [Header("Weapons SO: Two-Handed Maces")]
    public WeaponSO[] twoHandedMaces;

    [Header("Weapons SO: Swords")]
    public WeaponSO[] swords;

    [Header("Weapons SO: Two-Handed Swords")]
    public WeaponSO[] twoHandedSwords;

    [Header("Weapons SO: Daggers")]
    public WeaponSO[] daggers;

    [Header("Weapons SO: Staves")]
    public WeaponSO[] staves;

    [Header("Weapons SO: Spears")]
    public WeaponSO[] spears;

    [Header("Weapons SO: Bows")]
    public WeaponSO[] bows;

    [Header("Weapons SO: Muskets")]
    public WeaponSO[] muskets;

    [Header("Weapons SO: Shields")]
    public WeaponSO[] shields;

    [Header("Weapons SO: Orbs")]
    public WeaponSO[] orbs;

    [Header("Weapons SO: Tomes")]
    public WeaponSO[] tomes;

    [Header("Weapons SO: Quivers")]
    public WeaponSO[] quivers;

    [Header("Accessories SO")]
    public AccessorySO[] accessories;

    [Header("Heavy Armor SO: Helmets")]
    public ArmorSO[] heavyHelmets;

    [Header("Heavy Armor SO: Body Armor")]
    public ArmorSO[] heavyBodyArmor;

    [Header("Heavy Armor SO: Leg Armor")]
    public ArmorSO[] heavyLegArmor;

    [Header("Heavy Armor SO: Gloves")]
    public ArmorSO[] heavyGloves;

    [Header("Heavy Armor SO: Shoes")]
    public ArmorSO[] heavyShoes;

    [Header("Medium Armor SO: Helmets")]
    public ArmorSO[] mediumHelmets;

    [Header("Medium Armor SO: Body Armor")]
    public ArmorSO[] mediumBodyArmor;

    [Header("Medium Armor SO: Leg Armor")]
    public ArmorSO[] mediumLegArmor;

    [Header("Medium Armor SO: Gloves")]
    public ArmorSO[] mediumGloves;

    [Header("Medium Armor SO: Shoes")]
    public ArmorSO[] mediumShoes;

    [Header("Light Armor SO: Helmets")]
    public ArmorSO[] lightHelmets;

    [Header("Light Armor SO: Body Armor")]
    public ArmorSO[] lightBodyArmor;

    [Header("Light Armor SO: Leg Armor")]
    public ArmorSO[] lightLegArmor;

    [Header("Light Armor SO: Gloves")]
    public ArmorSO[] lightGloves;

    [Header("Light Armor SO: Shoes")]
    public ArmorSO[] lightShoes;

    public EquipmentSO RollEquipment(int equipmentTier)
    {
        EquipmentSO newEquipmentSO = null;
        System.Random rand = new System.Random();

        //------------------------
        //Roll Equip Slot of Item
        //------------------------
        EquipSlotType equimentSlotType = GetRandomWeightedItem<EquipSlotType>(WeightedEquipmentTable.equipSlotWeightTable);

        //-------------------------------------------------------------------
        //Roll Type of Items & Variety // Roll Core Values & Affixes
        //-------------------------------------------------------------------

        //Main-Hand
        if (equimentSlotType == EquipSlotType.WeaponMain)
        {
            WeaponTags weaponTag = GetRandomWeightedItem<WeaponTags>(WeightedEquipmentTable.mainHandWeightTable);
            WeaponSO weaponSO = null;
            //Roll Weapon Type & Variety
            switch (weaponTag)
            {
                case WeaponTags.Axe:
                    weaponSO = WeaponSO.Instantiate(axes[rand.Next(axes.Length)]);
                    break;
                case WeaponTags.TwoHandedAxe:
                    weaponSO = WeaponSO.Instantiate(twoHandedAxes[rand.Next(twoHandedAxes.Length)]);
                    break;
                case WeaponTags.Mace:
                    weaponSO = WeaponSO.Instantiate(maces[rand.Next(maces.Length)]);
                    break;
                case WeaponTags.TwoHandedMace:
                    weaponSO = WeaponSO.Instantiate(twoHandedMaces[rand.Next(twoHandedMaces.Length)]);
                    break;
                case WeaponTags.Sword:
                    weaponSO = WeaponSO.Instantiate(swords[rand.Next(swords.Length)]);
                    break;
                case WeaponTags.TwoHandedSword:
                    weaponSO = WeaponSO.Instantiate(twoHandedSwords[rand.Next(twoHandedSwords.Length)]);
                    break;
                case WeaponTags.Dagger:
                    weaponSO = WeaponSO.Instantiate(daggers[rand.Next(daggers.Length)]);
                    break;
                case WeaponTags.Staff:
                    weaponSO = WeaponSO.Instantiate(staves[rand.Next(staves.Length)]);
                    break;
                case WeaponTags.Spear:
                    weaponSO = WeaponSO.Instantiate(spears[rand.Next(spears.Length)]);
                    break;
                case WeaponTags.Bow:
                    weaponSO = WeaponSO.Instantiate(bows[rand.Next(bows.Length)]);
                    break;
                case WeaponTags.Musket:
                    weaponSO = WeaponSO.Instantiate(muskets[rand.Next(muskets.Length)]);
                    break;
                default:
                    Debug.LogError("Equip Roll ERROR: Main Weapon Type inválid.");
                    break;

            }

            //Roll Core Values & Affixes
            weaponSO.weaponDamage = rand.Next(weaponSO.minWeaponDamage[equipmentTier], weaponSO.maxWeaponDamage[equipmentTier] + 1);
            weaponSO.weaponActionSpeed = (float)Math.Round(rand.NextDouble() * (weaponSO.maxActionSpeed[equipmentTier] - weaponSO.minActionSpeed[equipmentTier]) + weaponSO.minActionSpeed[equipmentTier],1);

            foreach (AffixValuePair coreStat in weaponSO.coreAffixStats)
            {
                coreStat.value = (float)Math.Round(rand.NextDouble() * (coreStat.maxAffixStat[equipmentTier] - coreStat.minAffixStat[equipmentTier]) + coreStat.minAffixStat[equipmentTier],1);
                weaponSO.SetAffixValuePair(coreStat);
            }

            newEquipmentSO = weaponSO;

        }

        //Off-Hand
        if (equimentSlotType == EquipSlotType.WeaponOffhand)
        {
            WeaponTags weaponTag = GetRandomWeightedItem<WeaponTags>(WeightedEquipmentTable.offHandWeightTable);
            WeaponSO weaponSO = null;
            //Roll Weapon Type & Variety
            switch (weaponTag)
            {
                case WeaponTags.Axe:
                    weaponSO = WeaponSO.Instantiate(axes[rand.Next(axes.Length)]);
                    break;
                case WeaponTags.Mace:
                    weaponSO = WeaponSO.Instantiate(maces[rand.Next(maces.Length)]);
                    break;
                case WeaponTags.Sword:
                    weaponSO = WeaponSO.Instantiate(swords[rand.Next(swords.Length)]);
                    break;
                case WeaponTags.Dagger:
                    weaponSO = WeaponSO.Instantiate(daggers[rand.Next(daggers.Length)]);
                    break;
                case WeaponTags.Shield:
                    weaponSO = WeaponSO.Instantiate(shields[rand.Next(shields.Length)]);
                    break;
                case WeaponTags.Orb:
                    weaponSO = WeaponSO.Instantiate(orbs[rand.Next(orbs.Length)]);
                    break;
                case WeaponTags.Tome:
                    weaponSO = WeaponSO.Instantiate(tomes[rand.Next(tomes.Length)]);
                    break;
                case WeaponTags.Quiver:
                    weaponSO = WeaponSO.Instantiate(quivers[rand.Next(quivers.Length)]);
                    break;
                default:
                    Debug.LogError("Equip Roll ERROR: Off-Hand Weapon Type inválid.");
                    break;

            }

            //Roll Core Values & Affixes
            weaponSO.weaponDamage = rand.Next(weaponSO.minWeaponDamage[equipmentTier], weaponSO.maxWeaponDamage[equipmentTier] + 1);
            weaponSO.weaponActionSpeed = (float)Math.Round(rand.NextDouble() * (weaponSO.maxActionSpeed[equipmentTier] - weaponSO.minActionSpeed[equipmentTier]) + weaponSO.minActionSpeed[equipmentTier],1);

            foreach (AffixValuePair coreStat in weaponSO.coreAffixStats)
            {
                coreStat.value = (float)Math.Round(rand.NextDouble() * (coreStat.maxAffixStat[equipmentTier] - coreStat.minAffixStat[equipmentTier]) + coreStat.minAffixStat[equipmentTier],1);
                weaponSO.SetAffixValuePair(coreStat);
            }

            newEquipmentSO = weaponSO;
        }

        //Accessory
        if (equimentSlotType == EquipSlotType.Accessory)
        {
            //Roll Accessory Type & Variety
            AccessorySO accessorySO = AccessorySO.Instantiate(accessories[rand.Next(accessories.Length)]); ;

            //Roll Core Values & Affixes
            foreach (AffixValuePair coreStat in accessorySO.coreAffixStats)
            {
                coreStat.value = (float)Math.Round(rand.NextDouble() * (coreStat.maxAffixStat[equipmentTier] - coreStat.minAffixStat[equipmentTier]) + coreStat.minAffixStat[equipmentTier],1);
                accessorySO.SetAffixValuePair(coreStat);
            }
            newEquipmentSO = accessorySO;
        }

        //Armor
        if (equimentSlotType == EquipSlotType.Head || equimentSlotType == EquipSlotType.Chest || equimentSlotType == EquipSlotType.Legs || equimentSlotType == EquipSlotType.Hands || equimentSlotType == EquipSlotType.Feet)
        {
            ArmorTags armorTag = GetRandomWeightedItem<ArmorTags>(WeightedEquipmentTable.armorTypeWeightTable);

            ArmorSO armorSO = null;
            //Roll Armor Type & Variety
            if (equimentSlotType == EquipSlotType.Head)
            {
                switch (armorTag)
                {
                    case ArmorTags.Heavy:
                        armorSO = ArmorSO.Instantiate(heavyHelmets[rand.Next(heavyHelmets.Length)]);
                        break;
                    case ArmorTags.Medium:
                        armorSO = ArmorSO.Instantiate(mediumHelmets[rand.Next(mediumHelmets.Length)]);
                        break;
                    case ArmorTags.Light:
                        armorSO = ArmorSO.Instantiate(lightHelmets[rand.Next(lightHelmets.Length)]);
                        break;
                    default:
                        Debug.LogError("Equip Roll ERROR: Head Armor Type inválid.");
                        break;
                }
            }

            if (equimentSlotType == EquipSlotType.Chest)
            {
                switch (armorTag)
                {
                    case ArmorTags.Heavy:
                        armorSO = ArmorSO.Instantiate(heavyBodyArmor[rand.Next(heavyBodyArmor.Length)]);
                        break;
                    case ArmorTags.Medium:
                        armorSO = ArmorSO.Instantiate(mediumBodyArmor[rand.Next(mediumBodyArmor.Length)]);
                        break;
                    case ArmorTags.Light:
                        armorSO = ArmorSO.Instantiate(lightBodyArmor[rand.Next(lightBodyArmor.Length)]);
                        break;
                    default:
                        Debug.LogError("Equip Roll ERROR: Chest Armor Type inválid.");
                        break;
                }
            }

            if (equimentSlotType == EquipSlotType.Legs)
            {
                switch (armorTag)
                {
                    case ArmorTags.Heavy:
                        armorSO = ArmorSO.Instantiate(heavyLegArmor[rand.Next(heavyLegArmor.Length)]);
                        break;
                    case ArmorTags.Medium:
                        armorSO = ArmorSO.Instantiate(mediumLegArmor[rand.Next(mediumLegArmor.Length)]);
                        break;
                    case ArmorTags.Light:
                        armorSO = ArmorSO.Instantiate(lightLegArmor[rand.Next(lightLegArmor.Length)]);
                        break;
                    default:
                        Debug.LogError("Equip Roll ERROR: Legs Armor Type inválid.");
                        break;
                }
            }

            if (equimentSlotType == EquipSlotType.Hands)
            {
                switch (armorTag)
                {
                    case ArmorTags.Heavy:
                        armorSO = ArmorSO.Instantiate(heavyGloves[rand.Next(heavyGloves.Length)]);
                        break;
                    case ArmorTags.Medium:
                        armorSO = ArmorSO.Instantiate(mediumGloves[rand.Next(mediumGloves.Length)]);
                        break;
                    case ArmorTags.Light:
                        armorSO = ArmorSO.Instantiate(lightGloves[rand.Next(lightGloves.Length)]);
                        break;
                    default:
                        Debug.LogError("Equip Roll ERROR: Hand Armor Type inválid.");
                        break;
                }
            }

            if (equimentSlotType == EquipSlotType.Feet)
            {
                switch (armorTag)
                {
                    case ArmorTags.Heavy:
                        armorSO = ArmorSO.Instantiate(heavyShoes[rand.Next(heavyShoes.Length)]);
                        break;
                    case ArmorTags.Medium:
                        armorSO = ArmorSO.Instantiate(mediumShoes[rand.Next(mediumShoes.Length)]);
                        break;
                    case ArmorTags.Light:
                        armorSO = ArmorSO.Instantiate(lightShoes[rand.Next(lightShoes.Length)]);
                        break;
                    default:
                        Debug.LogError("Equip Roll ERROR: Feet Armor Type inválid.");
                        break;
                }
            }

            //Roll Core Values & Affixes
            armorSO.coreArmor = rand.Next(armorSO.mincoreArmor[equipmentTier], armorSO.maxcoreArmor[equipmentTier] + 1);
            armorSO.coreArmorThreshold = (int)Math.Round(rand.NextDouble() * (armorSO.maxcoreArmorThreshold[equipmentTier] - armorSO.mincoreArmorThreshold[equipmentTier]) + armorSO.mincoreArmorThreshold[equipmentTier]);
            armorSO.coreArmorRegenRate = (float)Math.Round(rand.NextDouble() * (armorSO.maxcoreArmorRegenRate[equipmentTier] - armorSO.mincoreArmorRegenRate[equipmentTier]) + armorSO.mincoreArmorRegenRate[equipmentTier],1);

            foreach (AffixValuePair coreStat in armorSO.coreAffixStats)
            {
                coreStat.value = (float)Math.Round(rand.NextDouble() * (coreStat.maxAffixStat[equipmentTier] - coreStat.minAffixStat[equipmentTier]) + coreStat.minAffixStat[equipmentTier],1);
                armorSO.SetAffixValuePair(coreStat);
            }

            armorSO.SetCoreArmorStats();

            newEquipmentSO = armorSO;
        }

        //-----------------------------------
        //Add Affixes to Equipment SO Stats
        //-----------------------------------

        newEquipmentSO.tier = equipmentTier;

        int itemRarityInt = GetRandomWeightedItem<int>(WeightedEquipmentTable.rarityWeightTable[equipmentTier]);
        Rarity itemRarity = Rarity.Common;

        if(itemRarityInt == 0)
        {
            itemRarity = Rarity.Common;
        }
        if(itemRarityInt > 0 && itemRarityInt < 3)
        {
            itemRarity = Rarity.Magic;
        }
        if (itemRarityInt > 2 && itemRarityInt < 5)
        {
            itemRarity = Rarity.Rare;
        }

        newEquipmentSO.rarity = itemRarity;

        //Roll Equipment Affixes
        RollEquipmentRarityAffixes(itemRarityInt, equipmentTier, newEquipmentSO);


        //-----------------------------------
        //Setup Text & Descriptions
        //-----------------------------------

        //Add affixes to general item stats
        foreach (EquipmentAffix equipAffix in newEquipmentSO.affixes)
        {
            foreach(AffixValuePair affix in equipAffix.affixes)
            {
                newEquipmentSO.SetAffixValuePair(affix);
            }
        }

        //Create Item Stat Description
        newEquipmentSO.CreateEquipmentStatDescription();

        return newEquipmentSO;
    }

    public void RollEquipmentRarityAffixes(int itemRaityInt, int equipmentTier, EquipmentSO equipmentSO)
    {
        List<int> affixIndex = new List<int>(); //Index of The Affix selected in Affix Table (non repeated)
        System.Random rand = new System.Random();


        while (affixIndex.Count < itemRaityInt)
        {
            int index = rand.Next(EquipmentAffixsData.affixTable.Length);
            if (!affixIndex.Contains(index))
            {
                //Add Index to counter
                affixIndex.Add(index);

                //Select Affix to add
                EquipmentAffix newAffix = EquipmentAffixsData.affixTable[index][rand.Next(EquipmentAffixsData.affixTable[index].Length)];

                //Roll Affix
                foreach (AffixValuePair affix in newAffix.affixes)
                {
                    affix.value = (float)Math.Round(rand.NextDouble() * (affix.maxAffixStat[equipmentTier] - affix.minAffixStat[equipmentTier]) + affix.minAffixStat[equipmentTier], 1);
                }

                //Add Affix To Item
                equipmentSO.affixes.Add(newAffix);
            }
        }

        equipmentSO.affixesIndex = affixIndex;

    }

    public bool BlackSmithAddAffix(EquipmentSO equipmentSO)
    {
        System.Random rand = new System.Random();
        int affixCount = equipmentSO.affixesIndex.Count;

        //If Equipment Affixes at max dont add more affixes
        if (affixCount == 4) return false;

        //Set new item Affix
        while (equipmentSO.affixesIndex.Count == affixCount)
        {
            int index = rand.Next(EquipmentAffixsData.affixTable.Length);

            if (!equipmentSO.affixesIndex.Contains(index))
            {
                //Add Index to counter
                equipmentSO.affixesIndex.Add(index);

                //Select Affix to add
                EquipmentAffix newAffix = EquipmentAffixsData.affixTable[index][rand.Next(EquipmentAffixsData.affixTable[index].Length)];

                //Roll Affix
                foreach (AffixValuePair affix in newAffix.affixes)
                {
                    affix.value = (float)Math.Round(rand.NextDouble() * (affix.maxAffixStat[equipmentSO.tier] - affix.minAffixStat[equipmentSO.tier]) + affix.minAffixStat[equipmentSO.tier], 1);
                }

                //Add Affix To Item
                equipmentSO.affixes.Add(newAffix);
            }
        }

        return true;
    }

    public bool BlackSmithRemoveAffix(EquipmentSO equipmentSO)
    {
        System.Random rand = new System.Random();
        int affixCount = equipmentSO.affixesIndex.Count;

        //If Equipment Affixes at 0 dont remove more affixes
        if (affixCount == 0) return false;

        //Remove random item Affix
        while (equipmentSO.affixesIndex.Count == affixCount)
        {
            int index = rand.Next(equipmentSO.affixesIndex.Count);

            //Remove Index counter
            equipmentSO.affixesIndex.RemoveAt(index);

            //Remove Affix
            equipmentSO.affixes.RemoveAt(index);

        }

        //Set new Item Rarity
        if (equipmentSO.affixesIndex.Count == 0)
        {
            equipmentSO.rarity = Rarity.Common;
        }
        if (equipmentSO.affixesIndex.Count > 0 && equipmentSO.affixesIndex.Count < 3)
        {
            equipmentSO.rarity = Rarity.Magic;
        }
        if (equipmentSO.affixesIndex.Count > 2 && equipmentSO.affixesIndex.Count < 5)
        {
            equipmentSO.rarity = Rarity.Rare;
        }

        return true;
    }

    public static T GetRandomWeightedItem<T>(Dictionary<T, double> itemList)
    {
        //Calculate Total Weight
        double totalWeight = 0;

        foreach (double itemWeight in itemList.Values)
        {
            totalWeight += itemWeight;
        }

        //Get random value between 0 and total weight
        System.Random rand = new System.Random();
        double randomValue = rand.NextDouble() * totalWeight;


        //Check weight intervals for rolled value
        double cumulativeWeight = 0;
        foreach (T weightedItem in itemList.Keys)
        {
            cumulativeWeight += itemList[weightedItem];
            if (randomValue < cumulativeWeight)
            {
                return weightedItem;
            }
        }

        return default;
    }
}
