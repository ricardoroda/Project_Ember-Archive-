using System.Collections.Generic;
using UnityEngine;
using static EquipmentTagsData;

[CreateAssetMenu(fileName = "ArmorSO", menuName = "Scriptable Objects/ArmorSO")]
public class ArmorSO : EquipmentSO
{
    [Header("Armor Core bonuses")]
    public ArmorTags armorType;
    public int coreArmor;
    public float coreArmorRegenRate;
    public int coreArmorThreshold;
    public List<AffixValuePair> coreAffixStats;

    [Header("Armor Rolling Limits")]
    public int[] mincoreArmor = new int[] { 0, 0, 0 };
    public int[] maxcoreArmor = new int[] { 0, 0, 0 };
    public float[] mincoreArmorRegenRate = new float[] { 0, 0, 0 };
    public float[] maxcoreArmorRegenRate = new float[] { 0, 0, 0 };
    public float[] mincoreArmorThreshold = new float[] { 0, 0, 0 };
    public float[] maxcoreArmorThreshold = new float[] { 0, 0, 0 };

    public void SetCoreArmorStats()
    {
        maxArmor += coreArmor;
        armorRegenRate += coreArmorRegenRate;
        armorThreshold += coreArmorThreshold;
    }

    public override void CreateEquipmentStatDescription()
    {
        //Armor Types
        List<string> typesLabels = new List<string>
        {
            "Rarity: ",
            "Tier: ",
            "Armor Type: ",
            "Slot: "
        };

        List<string> typesValues = new List<string>
        {
            rarity.ToString(),
            tier.ToString(),
            armorType.ToString(),
            equipSlot.ToString()
        };

        //Armor CoreStats
        List<string> coreStatsLabels = new List<string>
        {
            "Armor: ",
            "Armor Regen: ",
            "Armor Threshold: "
        };

        List<string> coreStatsValues = new List<string>
        {
            coreArmor.ToString(),
            coreArmorRegenRate.ToString(),
            coreArmorThreshold.ToString()
        };


        //Armor Core Affixes
        List<string> coreAffixes = new List<string>();

        foreach (AffixValuePair affix in coreAffixStats)
        {
            coreAffixes.Add(affix.statString);
        }

        //Armor Rarity Affixes
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
