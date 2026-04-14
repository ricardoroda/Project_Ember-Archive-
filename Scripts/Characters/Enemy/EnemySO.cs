using UnityEngine;

[CreateAssetMenu(menuName = "Game/Enemy Config")]
public class EnemySO : ScriptableObject
{
    [Header("Enemy Type")]
    public bool isBoss = false;

    [Header("NavMesh Agent Settings")]
    public float moveSpeed = 3.5f;
    public float acceleration = 8f;
    public float stoppingDistance = 2f;

    [Header("Resources")]
    public float health = 100f;
    public float armor = 0f;
    public float armorRegenRate = 0;
    public float armorRegenDelay = 2f;
    public float armorThreshold = 0f;

    [Header("Combat Movement")]
    public float sightRange = 10f;
    public float attackRange = 2f;

    [Header("Weapon Based Stats")]
    public int[] weaponDamage = new int[5] { 0, 0, 0, 0, 0 };
    public float weaponActionSpeed = 1f;

    //Generic Offensive Stats
    [Header("Damage Type Stats")]
    public float fireDamageMultiplier;
    public float waterDamageMultiplier;
    public float airDamageMultiplier;
    public float earthDamageMultiplier;
    public float arcaneDamageMultiplier;
    public int fireDamageFlat;
    public int waterDamageFlat;
    public int airDamageFlat;
    public int earthDamageFlat;
    public int arcaneDamageFlat;
    public float ailmentMultiplier = 1;
    public float damageReceivedMultiplier = 1;

    [Header("Critical Stats")]
    public float critChance;
    public float critMultiplier;

    [Header("Action Speed Stats")]
    public float actionSpeed;
    public float cooldownReduction;
    public float abilityDuration;

    //Specific Abillity Stats
    [Header("Specific Abillity Stats")]
    public int projectileIncrease;
    public int projectilePenetration;
    public float aoeIncrease;

    [Header("Rewards")]
    public int xpReward = 5;
    public int goldReward = 10;
    public int soulReward = 2;

}
