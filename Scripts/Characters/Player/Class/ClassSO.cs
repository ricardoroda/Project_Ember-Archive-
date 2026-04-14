using UnityEngine;
using static ClassListData;

[CreateAssetMenu(fileName = "ClassSO", menuName = "Game/Player Classes/Base")]
public class ClassSO : ScriptableObject
{
    [Header("Class Type")]
    public UserClasses playerClass;

    //Attributes
    [Header("Attributes")]
    public int strength = 0; // per 1 STR = +10 MaxArmor & +10% Crit chance
    public int dexterity = 0; // per 1 DEX = +2% Action Speed & +2% Move Speed
    public int intelligence = 0; //per 1 INT = +10 Max Mana & +5% Cooldown Reduction
    public int vitality = 0; //per 1 VIT = +10 Max Health & +5% Armor Threshold

    [Header("Resources Stats")]
    public float maxHealth = 0;
    public float maxMana = 0;
    public float armorThreshold = 0;
    public float manaRegen = 0;
    public float baseMoveSpeed = 0;

    [Header("Per Level Increase Stats")]
    public int levelUPStr = 0; // per 1 STR = +10 MaxArmor & +10% Crit chance
    public int levelUPDex = 0; // per 1 DEX = +2% Action Speed & +2% Move Speed
    public int levelUPInt = 0; //per 1 INT = +10 Max Mana & +5% Cooldown Reduction
    public int levelUPVit = 0; //per 1 VIT = +10 Max Health & +5% Armor Threshold
    public float levelUPMaxHP = 0;
    public float levelUPMaxMana = 0;
    public float levelUPArmorThreshold = 0;
    public float levelUPManaRegen = 0;

    [Header("Class Abilities Prefabs")]
    public GameObject[] classAbilities;

}
