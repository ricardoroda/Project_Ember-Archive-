using System.Collections.Generic;
using UnityEngine;
using static EquipmentTagsData;

[CreateAssetMenu(fileName = "AccessorySO", menuName = "Scriptable Objects/AccessorySO")]
public class AccessorySO : EquipmentSO
{
    [Header("Acessory Core bonuses")]
    public AccessoryTags accessoryType;
    public int accessoryEffectID;
    public List<AffixValuePair> coreAffixStats;

    public override void CreateEquipmentStatDescription()
    {
        //Accessory Types
        List<string> typesLabels = new List<string>
        {
            "Rarity: ",
            "Tier: ",
            "Slot: "
        };

        List<string> typesValues = new List<string>
        {
            rarity.ToString(),
            tier.ToString(),
            equipSlot.ToString()
        };

        //Accessory CoreStats
        List<string> coreStatsLabels = new List<string>
        {};

        List<string> coreStatsValues = new List<string>
        {};

        //Accessory Core Affixes
        List<string> coreAffixes = new List<string>();

        foreach (AffixValuePair affix in coreAffixStats)
        {
            coreAffixes.Add(affix.statString);
        }

        //Accessory Rarity Affixes
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
