using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum AffixStat
{
    //Attributes
    Strength,
    Dexterity,
    Intelligence,
    Vitality,

    //Resources Stats (Health, Mana & Armor)
    MaxHealth,
    MaxMana,
    ManaRegen,
    MaxArmor,
    ArmorRegenRate,   //Only given by Core Armor stats
    ArmorRegenRateMultiplier,
    ArmorRegenDelay,  //Defined by Body Armor
    ArmorThreshold, //Only given by Core Armor stats
    ArmorThresholdMultiplier,
    ArmorBreakDelay,  //Defined by Body Armor
    BaseMoveSpeed,    //Defined by Body Armor
    MoveSpeedMultiplier,    //Only given by Core Boots stats

    //Offensive Stats
    CritChance,
    CritMultiplier,
    FireDamageMultiplier,
    WaterDamageMultiplier,
    AirDamageMultiplier,
    EarthDamageMultiplier,
    ArcaneDamageMultiplier,
    FireDamageFlat,
    WaterDamageFlat,
    AirDamageFlat,
    EarthDamageFlat,
    ArcaneDamageFlat,
    AilmentMultiplier,
    ActionSpeed,    
    CooldownReduction,
    AbilityDuration,

    //Specific Abillity Stats
    ProjectileIncrease,
    ProjectilePenetration,
    AoeIncrease
}




[System.Serializable]
public class AffixValuePair
{
    public AffixStat statType;
    public float value;
    public float[] minAffixStat = new float[] {0, 0, 0};
    public float[] maxAffixStat = new float[] { 0, 0, 0 };
    public string statString = "";

    public AffixValuePair(AffixStat Stat, float Value, float[] MinAffixStat, float[] MaxAffixStat)
    {
        statType = Stat;
        value = Value;
        minAffixStat = MinAffixStat;
        maxAffixStat = MaxAffixStat;
    }

    public static string StatToString(AffixStat stat)
    {
        switch (stat)
        {
            //Attributes
            case AffixStat.Strength:
                return " Strength";
            case AffixStat.Dexterity:
                return " Dexterity";
            case AffixStat.Intelligence:
                return " Intelligence";
            case AffixStat.Vitality:
                return " Vitality";

            //Resources Stats (Health, Mana & Armor)
            case AffixStat.MaxHealth:
                return " Max Health";
            case AffixStat.MaxMana:
                return " Max Mana";
            case AffixStat.ManaRegen:
                return "/s Mana Regen";
            case AffixStat.MaxArmor:
                return " Max Armor";
            case AffixStat.ArmorRegenRate:
                return "/s Armor Regen";   //Only given by Core Armor stats
            case AffixStat.ArmorRegenRateMultiplier:
                return "% Armor Regen Multiplier";
            case AffixStat.ArmorRegenDelay:
                return "s Armor Regen Delay";  //Defined by Body Armor
            case AffixStat.ArmorThreshold:
                return " Armor Threshold"; //Only given by Core Armor stats
            case AffixStat.ArmorThresholdMultiplier:
                return "% Armor Threshold Multiplier";
            case AffixStat.ArmorBreakDelay:
                return "s Armor Break Delay";  //Defined by Body Armor
            case AffixStat.BaseMoveSpeed:
                return " Base Move Speed";    //Defined by Body Armor
            case AffixStat.MoveSpeedMultiplier:
                return "% Move Speed Multiplier";    //Only given by Core Boots stats

            //Offensive Stats
            case AffixStat.CritChance:
                return "% Critical Chance";
            case AffixStat.CritMultiplier:
                return "% Critical Multiplier";
            case AffixStat.FireDamageMultiplier:
                return "% Fire Damage Multiplier";
            case AffixStat.WaterDamageMultiplier:
                return "% Water Damage Multiplier";
            case AffixStat.AirDamageMultiplier:
                return "% Air Damage Multiplier";
            case AffixStat.EarthDamageMultiplier:
                return "% Earth Damage Multiplier";
            case AffixStat.ArcaneDamageMultiplier:
                return "% Arcane Damage Multiplier";
            case AffixStat.FireDamageFlat:
                return " Flat Fire Damage";
            case AffixStat.WaterDamageFlat:
                return " Flat Water Damage";
            case AffixStat.AirDamageFlat:
                return " Flat Air Damage";
            case AffixStat.EarthDamageFlat:
                return " Flat Earth Damage";
            case AffixStat.ArcaneDamageFlat:
                return " Flat Arcane Damage";
            case AffixStat.AilmentMultiplier:
                return "% Ailment Multiplier";
            case AffixStat.ActionSpeed:
                return "% Action Speed";
            case AffixStat.CooldownReduction:
                return "% Cooldown Reduction";
            case AffixStat.AbilityDuration:
                return "% Ability Duration";

            //Specific Abillity Stats
            case AffixStat.ProjectileIncrease:
                return " more Projectiles";
            case AffixStat.ProjectilePenetration:
                return " Projectile Penetration";
            case AffixStat.AoeIncrease:
                return "% Aoe Increase";
        }

        return "Error";
    }

}

[System.Serializable]
public class EquipmentAffix
{
    public AffixValuePair[] affixes;

    public EquipmentAffix(AffixValuePair[] Affixes)
    {
        affixes = Affixes;
    }

}

public static class EquipmentAffixsData
{
    //BODY ARMOR TYPE STATS - ARMOR REGEN DELAY/ ARMOR BREAK DELAY/ BASEMOVESPEED
    public static float[] noArmorStats = new float[] { 2f, 5f, 5f };
    public static float[] lightStats = new float[] { 2f, 2.5f, 8f };
    public static float[] mediumStats = new float[] { 3.5f, 4f, 5f };
    public static float[] heavyStats = new float[] { 5f, 5f, 4f };


    //Attributes
    private static float[] minMonoAttribute = new float[] { 1f, 4f, 10f };
    private static float[] maxMonoAttribute = new float[] { 4f, 10f, 20f };

    private static float[] minDualAttribute = new float[] { 1f, 2f, 5f };
    private static float[] maxDualAttribute = new float[] { 2f, 5f, 10f };

    //Resources Stats
    private static float[] minMonoBaseResource = new float[] { 20f, 100f, 200f };
    private static float[] maxMonoBaseResource = new float[] { 100f, 200f, 350f };

    private static float[] minDualBaseResource = new float[] { 10f, 50f, 100f };
    private static float[] maxDualBaseResource = new float[] { 50f, 100f, 175f };

    //Mana Regen
    private static float[] minMonoManaRegen = new float[] { 0.5f, 2f, 5f };
    private static float[] maxMonoManaRegen = new float[] { 2f, 5f, 10f };

    private static float[] minDualManaRegen = new float[] { 0.5f, 1f, 2.5f };
    private static float[] maxDualManaRegen = new float[] { 1f, 2.5f, 5f };

    //Armor Regen
    private static float[] minMonoArmorRegenMultiplier = new float[] { 4f, 6f, 10f };
    private static float[] maxMonoArmorRegenMultiplier = new float[] { 6f, 10f, 18f };

    private static float[] minDualArmorRegenMultiplier = new float[] { 2f, 3f, 5f };
    private static float[] maxDualArmorRegenMultiplier = new float[] { 3f, 5f, 9f };

    //Armor Threshold
    private static float[] minMonoArmorThresholdMultiplier = new float[] { 5f, 10f, 20f };
    private static float[] maxMonoArmorThresholdMultiplier = new float[] { 10f, 20f, 35f };

    private static float[] minDualArmorThresholdMultiplier = new float[] { 2f, 5f, 10f };
    private static float[] maxDualArmorThresholdMultiplier = new float[] { 5f, 10f, 18f };

    //Critical Chance
    private static float[] minMonoCriticalChance = new float[] { 5f, 10f, 20f };
    private static float[] maxMonoCriticalChance = new float[] { 10f, 20f, 35f };

    private static float[] minDualCriticalChance = new float[] { 2f, 5f, 10f };
    private static float[] maxDualCriticalChance = new float[] { 5f, 10f, 18f };

    //Critical Multiplier
    private static float[] minMonoCriticalMultiplier = new float[] { 5f, 10f, 20f };
    private static float[] maxMonoCriticalMultiplier = new float[] { 10f, 18f, 25f };

    private static float[] minDualCriticalMultiplier = new float[] { 2f, 5f, 10f };
    private static float[] maxDualCriticalMultiplier = new float[] { 5f, 9f, 12f };

    //Damage Multiplier
    private static float[] minMonoDamageMultiplier = new float[] { 10f, 20f, 35f };
    private static float[] maxMonoDamageMultiplier = new float[] { 20f, 35f, 50f };

    private static float[] minDualDamageMultiplier = new float[] { 5f, 10f, 18f };
    private static float[] maxDualDamageMultiplier = new float[] { 10f, 18f, 25f };

    //Damage Flat
    private static float[] minMonoDamageFlat = new float[] { 2f, 8f, 16f };
    private static float[] maxMonoDamageFlat = new float[] { 8f, 16f, 25f };

    private static float[] minDualDamageFlat = new float[] { 1f, 4f, 8f };
    private static float[] maxDualDamageFlat = new float[] { 4f, 8f, 12f };

    //Ailment Multiplier
    private static float[] minMonoAilmentMultiplier = new float[] { 10f, 20f, 35f };
    private static float[] maxMonoAilmentMultiplier = new float[] { 20f, 35f, 50f };

    private static float[] minDualAilmentMultiplier = new float[] { 5f, 10f, 18f };
    private static float[] maxDualAilmentMultiplier = new float[] { 10f, 18f, 25f };

    //Action Speed
    private static float[] minMonoActionSpeed = new float[] { 5f, 10f, 15f };
    private static float[] maxMonoActionSpeed = new float[] { 10f, 18f, 25f };

    private static float[] minDualActionSpeed = new float[] { 2f, 5f, 8f };
    private static float[] maxDualActionSpeed = new float[] { 5f, 9f, 12f };

    //Cooldown Reduction
    private static float[] minMonoCooldownReduction = new float[] { 10f, 15f, 20f };
    private static float[] maxMonoCooldownReduction = new float[] { 15f, 20f, 30f };

    private static float[] minDualCooldownReduction = new float[] { 5f, 8f, 10f };
    private static float[] maxDualCooldownReduction = new float[] { 8f, 10f, 15f };

    //Ability Duration
    private static float[] minMonoAbilityDuration = new float[] { 10f, 18f, 30f };
    private static float[] maxMonoAbilityDuration = new float[] { 20f, 28f, 40f };



    public static EquipmentAffix[][] MonoEquimentAffixes = new EquipmentAffix[][]
    {
        //Pure Attributes
        new EquipmentAffix[]
        {
            new EquipmentAffix (new AffixValuePair[]{ new AffixValuePair (AffixStat.Strength, 0, minMonoAttribute , maxMonoAttribute) }),
            new EquipmentAffix (new AffixValuePair[]{ new AffixValuePair (AffixStat.Dexterity, 0, minMonoAttribute , maxMonoAttribute) }),
            new EquipmentAffix (new AffixValuePair[]{ new AffixValuePair (AffixStat.Intelligence, 0, minMonoAttribute , maxMonoAttribute) }),
            new EquipmentAffix (new AffixValuePair[]{ new AffixValuePair (AffixStat.Vitality, 0, minMonoAttribute , maxMonoAttribute) })
        },
        //Pure Resources - Individual
        new EquipmentAffix[]
            { new EquipmentAffix (new AffixValuePair[]{ new AffixValuePair (AffixStat.MaxHealth, 0, minMonoBaseResource, maxMonoBaseResource) }) },
        new EquipmentAffix[]
            { new EquipmentAffix (new AffixValuePair[]{ new AffixValuePair (AffixStat.MaxMana, 0, minMonoBaseResource, maxMonoBaseResource) }) },
        new EquipmentAffix[]
            { new EquipmentAffix (new AffixValuePair[]{ new AffixValuePair (AffixStat.ManaRegen, 0, minMonoManaRegen, maxMonoManaRegen) }) },
        new EquipmentAffix[]
            { new EquipmentAffix (new AffixValuePair[]{ new AffixValuePair (AffixStat.MaxArmor, 0, minMonoBaseResource, maxMonoBaseResource) }) },
        new EquipmentAffix[]
            { new EquipmentAffix (new AffixValuePair[]{ new AffixValuePair (AffixStat.ArmorRegenRateMultiplier, 0, minMonoArmorRegenMultiplier, maxMonoArmorRegenMultiplier) }) },
        new EquipmentAffix[]
            { new EquipmentAffix (new AffixValuePair[]{ new AffixValuePair (AffixStat.ArmorThresholdMultiplier, 0, minMonoArmorThresholdMultiplier, maxMonoArmorThresholdMultiplier) }) },

        //Pure Critical Stats - Individual
        new EquipmentAffix[]
            { new EquipmentAffix (new AffixValuePair[]{ new AffixValuePair (AffixStat.CritChance, 0, minMonoCriticalChance, maxMonoCriticalChance) }) },
        new EquipmentAffix[]
            { new EquipmentAffix (new AffixValuePair[]{ new AffixValuePair (AffixStat.CritMultiplier, 0, minMonoCriticalMultiplier, maxMonoCriticalMultiplier) }) },

        //Pure Damage Multipliers
        new EquipmentAffix[]
        {
            new EquipmentAffix (new AffixValuePair[]{ new AffixValuePair (AffixStat.FireDamageMultiplier, 0, minMonoDamageMultiplier , maxMonoDamageMultiplier) }),
            new EquipmentAffix (new AffixValuePair[]{ new AffixValuePair (AffixStat.WaterDamageMultiplier, 0, minMonoDamageMultiplier , maxMonoDamageMultiplier) }),
            new EquipmentAffix (new AffixValuePair[]{ new AffixValuePair (AffixStat.AirDamageMultiplier, 0, minMonoDamageMultiplier , maxMonoDamageMultiplier) }),
            new EquipmentAffix (new AffixValuePair[]{ new AffixValuePair (AffixStat.EarthDamageMultiplier, 0, minMonoDamageMultiplier , maxMonoDamageMultiplier) }),
            new EquipmentAffix (new AffixValuePair[]{ new AffixValuePair (AffixStat.ArcaneDamageMultiplier, 0, minMonoDamageMultiplier , maxMonoDamageMultiplier) })
        },

        //Pure Flat Damage
        new EquipmentAffix[]
        {
            new EquipmentAffix (new AffixValuePair[]{ new AffixValuePair (AffixStat.FireDamageFlat, 0, minMonoDamageFlat, maxMonoDamageFlat) }),
            new EquipmentAffix (new AffixValuePair[]{ new AffixValuePair (AffixStat.WaterDamageFlat, 0, minMonoDamageFlat, maxMonoDamageFlat) }),
            new EquipmentAffix (new AffixValuePair[]{ new AffixValuePair (AffixStat.AirDamageFlat, 0, minMonoDamageFlat, maxMonoDamageFlat) }),
            new EquipmentAffix (new AffixValuePair[]{ new AffixValuePair (AffixStat.EarthDamageFlat, 0, minMonoDamageFlat, maxMonoDamageFlat) }),
            new EquipmentAffix (new AffixValuePair[]{ new AffixValuePair (AffixStat.ArcaneDamageFlat, 0, minMonoDamageFlat, maxMonoDamageFlat) })
        },

        //Pure Speed Stats - Individual
        new EquipmentAffix[]
            { new EquipmentAffix (new AffixValuePair[]{ new AffixValuePair (AffixStat.AilmentMultiplier, 0, minMonoAilmentMultiplier, maxMonoAilmentMultiplier) }) },
        new EquipmentAffix[]
            { new EquipmentAffix (new AffixValuePair[]{ new AffixValuePair (AffixStat.ActionSpeed, 0, minMonoActionSpeed, maxMonoActionSpeed) }) },
        new EquipmentAffix[]
            { new EquipmentAffix (new AffixValuePair[]{ new AffixValuePair (AffixStat.CooldownReduction, 0, minMonoCooldownReduction, maxMonoCooldownReduction) }) },
        new EquipmentAffix[]
            { new EquipmentAffix (new AffixValuePair[]{ new AffixValuePair (AffixStat.AbilityDuration, 0, minMonoAbilityDuration, maxMonoAbilityDuration) }) }
    };

    public static EquipmentAffix[][] DualEquimentAffixes = new EquipmentAffix[][]
    {
        //Dual Attributes
        new EquipmentAffix[]
        {
            new EquipmentAffix (new AffixValuePair[]{ new AffixValuePair (AffixStat.Strength, 0, minDualAttribute , maxDualAttribute),
                                                      new AffixValuePair (AffixStat.Dexterity, 0, minDualAttribute , maxDualAttribute)}),

            new EquipmentAffix (new AffixValuePair[]{ new AffixValuePair (AffixStat.Strength, 0, minDualAttribute , maxDualAttribute),
                                                      new AffixValuePair (AffixStat.Intelligence, 0, minDualAttribute , maxDualAttribute)}),

            new EquipmentAffix (new AffixValuePair[]{ new AffixValuePair (AffixStat.Strength, 0, minDualAttribute , maxDualAttribute),
                                                      new AffixValuePair (AffixStat.Vitality, 0, minDualAttribute , maxDualAttribute)}),

            new EquipmentAffix (new AffixValuePair[]{ new AffixValuePair (AffixStat.Dexterity, 0, minDualAttribute , maxDualAttribute),
                                                      new AffixValuePair (AffixStat.Intelligence, 0, minDualAttribute , maxDualAttribute)}),

            new EquipmentAffix (new AffixValuePair[]{ new AffixValuePair (AffixStat.Dexterity, 0, minDualAttribute , maxDualAttribute),
                                                      new AffixValuePair (AffixStat.Vitality, 0, minDualAttribute , maxDualAttribute)}),

            new EquipmentAffix (new AffixValuePair[]{ new AffixValuePair (AffixStat.Intelligence, 0, minDualAttribute , maxDualAttribute),
                                                      new AffixValuePair (AffixStat.Vitality, 0, minDualAttribute , maxDualAttribute)})
        },

        //Dual Resources
        new EquipmentAffix[]
        {
            new EquipmentAffix (new AffixValuePair[]{ new AffixValuePair (AffixStat.MaxHealth, 0, minDualBaseResource , maxDualBaseResource),
                                                      new AffixValuePair (AffixStat.MaxMana, 0, minDualBaseResource , maxDualBaseResource)}),

            new EquipmentAffix (new AffixValuePair[]{ new AffixValuePair (AffixStat.MaxHealth, 0, minDualBaseResource , maxDualBaseResource),
                                                      new AffixValuePair (AffixStat.MaxArmor, 0, minDualBaseResource , maxDualBaseResource)}),

            new EquipmentAffix (new AffixValuePair[]{ new AffixValuePair (AffixStat.MaxMana, 0, minDualBaseResource , maxDualBaseResource),
                                                      new AffixValuePair (AffixStat.MaxArmor, 0, minDualBaseResource , maxDualBaseResource)}),
        },

        //Mana & Mana Regen 
        new EquipmentAffix[]
        {
            new EquipmentAffix (new AffixValuePair[]{ new AffixValuePair (AffixStat.MaxMana, 0, minDualBaseResource , maxDualBaseResource),
                                                      new AffixValuePair (AffixStat.ManaRegen, 0, minDualManaRegen , maxDualManaRegen)})
        },

        //Armor & Armor Threshold Multiplier/Armor Regen Multiplier
        new EquipmentAffix[]
        {
            new EquipmentAffix (new AffixValuePair[]{ new AffixValuePair (AffixStat.MaxArmor, 0, minDualBaseResource , maxDualBaseResource),
                                                      new AffixValuePair (AffixStat.ArmorRegenRateMultiplier, 0, minDualArmorRegenMultiplier , maxDualArmorRegenMultiplier)}),

            new EquipmentAffix (new AffixValuePair[]{ new AffixValuePair (AffixStat.MaxArmor, 0, minDualBaseResource , maxDualBaseResource),
                                                      new AffixValuePair (AffixStat.ArmorThresholdMultiplier, 0, minDualArmorThresholdMultiplier , maxDualArmorThresholdMultiplier)})
        },

        //Crit Change & Multiplier
        new EquipmentAffix[]
        {
            new EquipmentAffix (new AffixValuePair[]{ new AffixValuePair (AffixStat.CritChance, 0, minDualCriticalChance , maxDualCriticalChance),
                                                      new AffixValuePair (AffixStat.CritMultiplier, 0, minDualCriticalMultiplier , maxDualCriticalMultiplier)})
        },

        //Atributes & Flat Damage
        new EquipmentAffix[]
        {
            new EquipmentAffix (new AffixValuePair[]{ new AffixValuePair (AffixStat.Strength, 0, minDualAttribute , maxDualAttribute),
                                                      new AffixValuePair (AffixStat.FireDamageFlat, 0, minDualDamageFlat , maxDualDamageFlat)}),

            new EquipmentAffix (new AffixValuePair[]{ new AffixValuePair (AffixStat.Strength, 0, minDualAttribute , maxDualAttribute),
                                                      new AffixValuePair (AffixStat.WaterDamageFlat, 0, minDualDamageFlat , maxDualDamageFlat)}),

            new EquipmentAffix (new AffixValuePair[]{ new AffixValuePair (AffixStat.Strength, 0, minDualAttribute , maxDualAttribute),
                                                      new AffixValuePair (AffixStat.AirDamageFlat, 0, minDualDamageFlat , maxDualDamageFlat)}),

            new EquipmentAffix (new AffixValuePair[]{ new AffixValuePair (AffixStat.Strength, 0, minDualAttribute , maxDualAttribute),
                                                      new AffixValuePair (AffixStat.EarthDamageFlat, 0, minDualDamageFlat , maxDualDamageFlat)}),

            new EquipmentAffix (new AffixValuePair[]{ new AffixValuePair (AffixStat.Strength, 0, minDualAttribute , maxDualAttribute),
                                                      new AffixValuePair (AffixStat.ArcaneDamageFlat, 0, minDualDamageFlat , maxDualDamageFlat)}),

        //--------------------------------------
            new EquipmentAffix (new AffixValuePair[]{ new AffixValuePair (AffixStat.Dexterity, 0, minDualAttribute , maxDualAttribute),
                                                      new AffixValuePair (AffixStat.FireDamageFlat, 0, minDualDamageFlat , maxDualDamageFlat)}),

            new EquipmentAffix (new AffixValuePair[]{ new AffixValuePair (AffixStat.Dexterity, 0, minDualAttribute , maxDualAttribute),
                                                      new AffixValuePair (AffixStat.WaterDamageFlat, 0, minDualDamageFlat , maxDualDamageFlat)}),

            new EquipmentAffix (new AffixValuePair[]{ new AffixValuePair (AffixStat.Dexterity, 0, minDualAttribute , maxDualAttribute),
                                                      new AffixValuePair (AffixStat.AirDamageFlat, 0, minDualDamageFlat , maxDualDamageFlat)}),

            new EquipmentAffix (new AffixValuePair[]{ new AffixValuePair (AffixStat.Dexterity, 0, minDualAttribute , maxDualAttribute),
                                                      new AffixValuePair (AffixStat.EarthDamageFlat, 0, minDualDamageFlat , maxDualDamageFlat)}),

            new EquipmentAffix (new AffixValuePair[]{ new AffixValuePair (AffixStat.Dexterity, 0, minDualAttribute , maxDualAttribute),
                                                      new AffixValuePair (AffixStat.ArcaneDamageFlat, 0, minDualDamageFlat , maxDualDamageFlat)}),

        //--------------------------------------
            new EquipmentAffix (new AffixValuePair[]{ new AffixValuePair (AffixStat.Intelligence, 0, minDualAttribute , maxDualAttribute),
                                                      new AffixValuePair (AffixStat.FireDamageFlat, 0, minDualDamageFlat , maxDualDamageFlat)}),

            new EquipmentAffix (new AffixValuePair[]{ new AffixValuePair (AffixStat.Intelligence, 0, minDualAttribute , maxDualAttribute),
                                                      new AffixValuePair (AffixStat.WaterDamageFlat, 0, minDualDamageFlat , maxDualDamageFlat)}),

            new EquipmentAffix (new AffixValuePair[]{ new AffixValuePair (AffixStat.Intelligence, 0, minDualAttribute , maxDualAttribute),
                                                      new AffixValuePair (AffixStat.AirDamageFlat, 0, minDualDamageFlat , maxDualDamageFlat)}),

            new EquipmentAffix (new AffixValuePair[]{ new AffixValuePair (AffixStat.Intelligence, 0, minDualAttribute , maxDualAttribute),
                                                      new AffixValuePair (AffixStat.EarthDamageFlat, 0, minDualDamageFlat , maxDualDamageFlat)}),

            new EquipmentAffix (new AffixValuePair[]{ new AffixValuePair (AffixStat.Intelligence, 0, minDualAttribute , maxDualAttribute),
                                                      new AffixValuePair (AffixStat.ArcaneDamageFlat, 0, minDualDamageFlat , maxDualDamageFlat)}),
        
        //--------------------------------------
            new EquipmentAffix (new AffixValuePair[]{ new AffixValuePair (AffixStat.Vitality, 0, minDualAttribute , maxDualAttribute),
                                                      new AffixValuePair (AffixStat.FireDamageFlat, 0, minDualDamageFlat , maxDualDamageFlat)}),

            new EquipmentAffix (new AffixValuePair[]{ new AffixValuePair (AffixStat.Vitality, 0, minDualAttribute , maxDualAttribute),
                                                      new AffixValuePair (AffixStat.WaterDamageFlat, 0, minDualDamageFlat , maxDualDamageFlat)}),

            new EquipmentAffix (new AffixValuePair[]{ new AffixValuePair (AffixStat.Vitality, 0, minDualAttribute , maxDualAttribute),
                                                      new AffixValuePair (AffixStat.AirDamageFlat, 0, minDualDamageFlat , maxDualDamageFlat)}),

            new EquipmentAffix (new AffixValuePair[]{ new AffixValuePair (AffixStat.Vitality, 0, minDualAttribute , maxDualAttribute),
                                                      new AffixValuePair (AffixStat.EarthDamageFlat, 0, minDualDamageFlat , maxDualDamageFlat)}),

            new EquipmentAffix (new AffixValuePair[]{ new AffixValuePair (AffixStat.Vitality, 0, minDualAttribute , maxDualAttribute),
                                                      new AffixValuePair (AffixStat.ArcaneDamageFlat, 0, minDualDamageFlat , maxDualDamageFlat)})

        },

        //Flat Damage & Multiplier (Same Type)
        new EquipmentAffix[]
        {
            new EquipmentAffix (new AffixValuePair[]{ new AffixValuePair (AffixStat.FireDamageFlat, 0, minDualDamageFlat , maxDualDamageFlat),
                                                      new AffixValuePair (AffixStat.FireDamageMultiplier, 0, minDualDamageMultiplier , maxDualDamageMultiplier)}),

            new EquipmentAffix (new AffixValuePair[]{ new AffixValuePair (AffixStat.WaterDamageFlat, 0, minDualDamageFlat , maxDualDamageFlat),
                                                      new AffixValuePair (AffixStat.WaterDamageMultiplier, 0, minDualDamageMultiplier , maxDualDamageMultiplier)}),

            new EquipmentAffix (new AffixValuePair[]{ new AffixValuePair (AffixStat.AirDamageFlat, 0, minDualDamageFlat , maxDualDamageFlat),
                                                      new AffixValuePair (AffixStat.AirDamageMultiplier, 0, minDualDamageMultiplier , maxDualDamageMultiplier)}),

            new EquipmentAffix (new AffixValuePair[]{ new AffixValuePair (AffixStat.EarthDamageFlat, 0, minDualDamageFlat , maxDualDamageFlat),
                                                      new AffixValuePair (AffixStat.EarthDamageMultiplier, 0, minDualDamageMultiplier , maxDualDamageMultiplier)}),

            new EquipmentAffix (new AffixValuePair[]{ new AffixValuePair (AffixStat.ArcaneDamageFlat, 0, minDualDamageFlat , maxDualDamageFlat),
                                                      new AffixValuePair (AffixStat.ArcaneDamageMultiplier, 0, minDualDamageMultiplier , maxDualDamageMultiplier)}),
        },

        //Action Speed & Cooldown Reduction
        new EquipmentAffix[]
        {
            new EquipmentAffix (new AffixValuePair[]{ new AffixValuePair (AffixStat.ActionSpeed, 0, minDualActionSpeed , maxDualActionSpeed),
                                                      new AffixValuePair (AffixStat.CooldownReduction, 0, minDualCooldownReduction , maxDualCooldownReduction)}),
        },

        //Ailment Multiplier & Cooldown Reduction
        new EquipmentAffix[]
        {
            new EquipmentAffix (new AffixValuePair[]{ new AffixValuePair (AffixStat.AilmentMultiplier, 0, minDualAilmentMultiplier , maxDualAilmentMultiplier),
                                                      new AffixValuePair (AffixStat.CooldownReduction, 0, minDualCooldownReduction , maxDualCooldownReduction)}),
        }

};
    public static EquipmentAffix[][] affixTable = EquipmentAffixsData.MonoEquimentAffixes.Concat(EquipmentAffixsData.DualEquimentAffixes).ToArray();
}



