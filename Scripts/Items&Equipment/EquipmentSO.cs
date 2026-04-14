using UnityEngine;
using static EquipmentTagsData;
using static EquipmentAffixsData;
using System;
using System.Collections.Generic;
using JetBrains.Annotations;

[CreateAssetMenu(menuName = "Items/EquipmentSO", fileName = "NewEquipmentSO")]
public class EquipmentSO : ScriptableObject
{
    [Header("Information")]
    public string itemName;
    public Sprite icon;
    [TextArea] public string description;
    public EquipmentStatDescription equipmentStatDescription;

    [Header("Slot")]
    public int tier;
    public Rarity rarity;
    public EquipSlotType equipSlot = EquipSlotType.Head;
    public List<int> affixesIndex;
    public List<EquipmentAffix> affixes;

    [Header("Shop price")]
    public float shopBuyPrice = 0;
    public float shopSellPrice = 0;

    //Attributes
    [Header("Attributes")]
    public int strength = 0;
    public int dexterity = 0;
    public int intelligence = 0;
    public int vitality = 0;

    // Resources Stats (Health, Mana & Armor)
    [Header("Resources Stats")]
    public int maxHealth = 0;
    public int maxMana = 0;
    public float manaRegen = 0f;
    public int maxArmor = 0;
    public float armorRegenRate = 0f;
    public float armorRegenRateMultipier = 0f;
    public int armorThreshold = 0;
    public float armorThresholdMultiplier = 0f;
    public float armorRegenDelay = 0f;
    [Tooltip("If damage > this value the excess damage is dealt to Health")]
    public float moveSpeedMultiplier = 0f;

    //Offensive Stats
    [Header("Generic Offensive Stats")]
    public float critChance = 0f;
    public float critMultiplier = 0f;
    public float fireDamageMultiplier = 0f;
    public float waterDamageMultiplier = 0f;
    public float airDamageMultiplier = 0f;
    public float earthDamageMultiplier = 0f;
    public float arcaneDamageMultiplier = 0f;
    public int fireDamageFlat = 0;
    public int waterDamageFlat = 0;
    public int airDamageFlat = 0;
    public int earthDamageFlat = 0;
    public int arcaneDamageFlat = 0;
    public float ailmentMultiplier = 0f;
    public float actionSpeed = 0f;
    public float cooldownReduction = 0f;
    public float abilityDuration = 0f;

    //Specific Abillity Stats
    [Header("Specific Abillity Stats")]
    public int projectileIncrease = 0;
    public int projectilePenetration = 0;
    public float aoeIncrease = 0f;


    public void SetAffixValuePair(AffixValuePair affix)
    {
        affix.statString = "+ ";

        switch (affix.statType)
        {
            case AffixStat.Strength:
                strength += (int)Math.Round(affix.value);
                affix.statString += ((int)Math.Round(affix.value));
                break;
            case AffixStat.Dexterity:
                dexterity += (int)Math.Round(affix.value);
                affix.statString += ((int)Math.Round(affix.value));
                break;
            case AffixStat.Intelligence:
                intelligence += (int)Math.Round(affix.value);
                affix.statString += ((int)Math.Round(affix.value));
                break;
            case AffixStat.Vitality:
                vitality += (int)Math.Round(affix.value);
                affix.statString += ((int)Math.Round(affix.value));
                break;

            case AffixStat.MaxHealth:
                maxHealth += (int)Math.Round(affix.value);
                affix.statString += ((int)Math.Round(affix.value));
                break;
            case AffixStat.MaxMana:
                maxMana += (int)Math.Round(affix.value);
                affix.statString += ((int)Math.Round(affix.value));
                break;
            case AffixStat.ManaRegen:
                manaRegen += affix.value;
                affix.statString += affix.value;
                break;
            case AffixStat.MaxArmor:
                maxArmor += (int)Math.Round(affix.value);
                affix.statString += ((int)Math.Round(affix.value));
                break;
            case AffixStat.ArmorRegenRate:
                armorRegenRate += affix.value;
                affix.statString += affix.value;
                break;
            case AffixStat.ArmorRegenRateMultiplier:
                armorRegenRateMultipier += affix.value;
                affix.statString += affix.value;
                break;
            case AffixStat.ArmorThreshold:
                armorThreshold += (int)Math.Round(affix.value);
                affix.statString += ((int)Math.Round(affix.value));
                break;
            case AffixStat.ArmorThresholdMultiplier:
                armorThresholdMultiplier += affix.value;
                affix.statString += affix.value;
                break;
            case AffixStat.MoveSpeedMultiplier:
                moveSpeedMultiplier += affix.value;
                affix.statString += affix.value;
                break;

            case AffixStat.CritChance:
                critChance += affix.value;
                affix.statString += affix.value;
                break;
            case AffixStat.CritMultiplier:
                critMultiplier += affix.value;
                affix.statString += affix.value;
                break;
            case AffixStat.FireDamageMultiplier:
                fireDamageMultiplier += affix.value;
                affix.statString += affix.value;
                break;
            case AffixStat.WaterDamageMultiplier:
                waterDamageMultiplier += affix.value;
                affix.statString += affix.value;
                break;
            case AffixStat.AirDamageMultiplier:
                airDamageMultiplier += affix.value;
                affix.statString += affix.value;
                break;
            case AffixStat.EarthDamageMultiplier:
                earthDamageMultiplier += affix.value;
                affix.statString += affix.value;
                break;
            case AffixStat.ArcaneDamageMultiplier:
                earthDamageMultiplier += affix.value;
                affix.statString += affix.value;
                break;
            case AffixStat.FireDamageFlat:
                fireDamageFlat += (int)Math.Round(affix.value);
                affix.statString += ((int)Math.Round(affix.value));
                break;
            case AffixStat.WaterDamageFlat:
                waterDamageFlat += (int)Math.Round(affix.value);
                affix.statString += ((int)Math.Round(affix.value));
                break;
            case AffixStat.AirDamageFlat:
                airDamageFlat += (int)Math.Round(affix.value);
                affix.statString += ((int)Math.Round(affix.value));
                break;
            case AffixStat.EarthDamageFlat:
                earthDamageFlat += (int)Math.Round(affix.value);
                affix.statString += ((int)Math.Round(affix.value));
                break;
            case AffixStat.ArcaneDamageFlat:
                arcaneDamageFlat = (int)Math.Round(affix.value);
                affix.statString += ((int)Math.Round(affix.value));
                break;
            case AffixStat.AilmentMultiplier:
                ailmentMultiplier += affix.value;
                affix.statString += affix.value;
                break;
            case AffixStat.ActionSpeed:
                actionSpeed += affix.value;
                affix.statString += affix.value;
                break;
            case AffixStat.CooldownReduction:
                cooldownReduction += affix.value;
                affix.statString += affix.value;
                break;
            case AffixStat.AbilityDuration:
                abilityDuration += affix.value;
                affix.statString += affix.value;
                break;
            case AffixStat.ProjectileIncrease:
                projectileIncrease += (int)Math.Round(affix.value);
                affix.statString += ((int)Math.Round(affix.value));
                break;
            case AffixStat.ProjectilePenetration:
                projectilePenetration += (int)Math.Round(affix.value);
                affix.statString += ((int)Math.Round(affix.value));
                break;
            case AffixStat.AoeIncrease:
                aoeIncrease += affix.value;
                affix.statString += affix.value;
                break;
            default:
                Debug.LogError("Equipment Affix stat inválid");
                break;
        }

        affix.statString += AffixValuePair.StatToString(affix.statType);
    }

    public virtual void CreateEquipmentStatDescription() { }

}

[System.Serializable]
public class EquipmentStatDescription
{
    public string name;
    public List<string> typesLabels;
    public List<string> typesValues;
    public List<string> coreStatsLabels;
    public List<string> coreStatsValues;
    public List<string> coreAffixes;
    public List<string> rarityAffixes;

    public EquipmentStatDescription(string Name, List<string> TypesLabels, List<string> TypesValues, List<string> CoreStatsLabels, List<string> CoreStatsValues, List<string> CoreAffixes, List<string> RarityAffixes)
    {
        name = Name;
        typesLabels = TypesLabels;
        typesValues = TypesValues;
        coreStatsLabels = CoreStatsLabels;
        coreStatsValues = CoreStatsValues;
        coreAffixes = CoreAffixes;
        rarityAffixes = RarityAffixes;
    }
}

