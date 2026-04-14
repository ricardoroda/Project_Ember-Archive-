using Unity.VisualScripting;
using UnityEngine;

public class AbilityTagsData
{
    public enum AbilityTags
    {
        //Form Type
        Melee,
        Projectile,
        AOE,
        //Refresh Type
        FireRate,
        Cooldown
    }

    public string[] abilityTagsString =
    {
        //Form Type
        "Melee",
        "Projectile",
        "AOE",
        //Refresh Type
        "FireRate",
        "Cooldown"
    };
}
