using System;
using UnityEngine;

[CreateAssetMenu(fileName = "DamageTagsData", menuName = "Scriptable Objects/DamageTagsData")]
public class DamageTagsData : ScriptableObject
{
    public enum DamageTags
    {
        Fire,       //Burn 
        Water,      //chilled
        Air,        //Shock
        Earth,      //Stun
        Arcane      //Void
    }

    public enum DamageAilmentsTags
    {
        Burn,
        Chill,
        Shock,
        Stun,
        Void
    }

    public string[] abilityTagsString =
    {
        "Fire",       //Burn 
        "Water",      //Chilled
        "Air",        //Shock
        "Earth",      //Stun
        "Arcane"      //Void
    };

    public string[] abilityAilmentsTagsString =
    {
        "Burn",
        "Chill",
        "Shock",
        "Stun",
        "Void"
    };
}
