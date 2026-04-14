using UnityEngine;
using static EquipmentTagsData;
using static DamageTagsData;
using static EquipmentAffix;
using System.Collections.Generic;
using NUnit.Framework.Constraints;

[CreateAssetMenu(fileName = "WeaponSO", menuName = "Scriptable Objects/WeaponSO")]
public class WeaponSO : EquipmentSO
{
    [Header("Weapon Core bonuses")]
    public WeaponTags weaponType;
    public DamageTags damageType;
    public int weaponDamage;
    public float weaponActionSpeed;
    public List<AffixValuePair> coreAffixStats;

    [Header("Weapon Rolling Limits")]
    public int[] minWeaponDamage = new int[] { 0, 0, 0 };
    public int[] maxWeaponDamage = new int[] { 0, 0, 0 };
    public float[] minActionSpeed = new float[] { 0, 0, 0 };
    public float[] maxActionSpeed = new float[] { 0, 0, 0 };

    public override void CreateEquipmentStatDescription()
    {
        //Weapon Types
        List<string> typesLabels = new List<string>
        {
            "Rarity: ",
            "Tier: ",
            "Weapon Type: ",
            "Slot: "
        };

        List<string> typesValues = new List<string>
        {
            rarity.ToString(),
            tier.ToString(),
            weaponType.ToString(),
            equipSlot.ToString()
        };

        //Weapon CoreStats
        List<string> coreStatsLabels = new List<string>
        {
            "Damage Type: ",
            "Damage: ",
            "Action Speed: "
        };

        List<string> coreStatsValues = new List<string>
        {
            damageType.ToString(),
            weaponDamage.ToString(),
            weaponActionSpeed.ToString()
        };

        //Weapon Core Affixes
        List<string> coreAffixes = new List<string>();

        foreach(AffixValuePair affix in coreAffixStats)
        {
            coreAffixes.Add(affix.statString);
        }

        //Weapon Rarity Affixes
        List<string> rarityAffixes = new List<string>();

        foreach (EquipmentAffix equipAffix in affixes)
        {
            string affixString = equipAffix.affixes[0].statString;
            if (equipAffix.affixes.Length > 1)
            {
                affixString += "\n" + equipAffix.affixes[1].statString;
            }

            rarityAffixes.Add(affixString);
        }


        equipmentStatDescription = new EquipmentStatDescription(itemName, typesLabels, typesValues, coreStatsLabels, coreStatsValues, coreAffixes, rarityAffixes);

    }
}
