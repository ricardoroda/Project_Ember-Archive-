using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static AbilityTagsData;
using static ClassListData;
using static EquipmentTagsData;
using static DamageTagsData;

[CreateAssetMenu(fileName = "AbilitySO", menuName = "Scriptable Objects/AbilitySO")]
public abstract class AbilitySO : ScriptableObject
{
    [Header("UI & Icons")]
    public Sprite abilityIcon;
    public enum SkillType { Normal, Dash }

    [Header("Ability Tags")]
    public UserClasses userClass;
    public string skillName;
    public bool isTriggerred = false;
    public string description;
    public SkillType skillType = SkillType.Normal;

    [Header("Resources")]
    public int manaCost = 0;

    [Header("Base Ability Damage")]
    public float flatDamageMultiplier = 1f;
    public int baseFireDamage = 0;
    public int baseWaterDamage = 0;
    public int baseAirDamage = 0;
    public int baseEarthDamage = 0;
    public int baseArcaneDamage = 0;
    public float[] abilityDamage = new float[5] { 0f, 0f, 0f, 0f, 0f };

    [Header("Weapon Dependent Damage")]
    [SerializeField, Tooltip("Add a Percentage (%) of the Weapon Based Damage")]
    public bool weaponBasedDamageFlag = false;
    public List<WeaponTags> weaponTypesUsable;
    [SerializeField, Tooltip("Percentage (%) Value of Weapon Based Damage.")]
    public float weaponBaseDamage = 0f; //Percentage

    [Header("Ailment Attributes")]
    [SerializeField, Tooltip("Base Ailment multiplier percent (%).")]
    public float[] baseAilmentMultiplier = new float[5] { 0f, 0f, 0f, 0f, 0f }; //Percentage

    [Header("Ailment Attributes")]
    [SerializeField, Tooltip("Base Critical Chance probability percent (%).")]
    public float baseCritChance = 0f; //Percentage

    [Header("Time Related Attributes")]
    [SerializeField, Tooltip("Attacks per Second.")]
    public float fireRate = 0.0f;
    [SerializeField, Tooltip("Overrides Base Action Speed for Weapon Based Attack Speed")]
    public bool weaponBasedActionSpeedFlag = false;
    [SerializeField, Tooltip("Percentage (%) Value of Weapon Based Attack Speed.")]
    public float weaponBaseAttackSpeed = 0f;    //Needs weapon based flag
    public bool cooldownFlag = false;
    public float cooldown = 0; //Seconds
    public float lifeTime = 0;  //Seconds

    [Header("Target Tags")]
    public string targetTags;
}
