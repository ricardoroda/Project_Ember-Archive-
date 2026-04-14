using System.Collections;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor.UI;
#endif
using UnityEngine;
using UnityEngine.SocialPlatforms;

public class MeleeBehaviour : AbilityBehaviour
{
    // Melee Ability specific Variables
    public MeleeSO meleeData;
    private float spawnTime;

    // ----------------------------------------------
    // RUNTIME FUNCTIONS (SETUP & ABILITY ACTIONS)
    // ----------------------------------------------
    void Start()
    {
        spawnTime = Time.time;
    }

    void Update()
    {
        //Melee Project Movement
        MeleeMovement();

        //Variation of Scale during Movement & Combo
        SetMeleeMultiScale();

        //Melee Attack States
        LifeTimeCheck(spawnTime);
    }

    public override void OnSpawned()
    {
        targetsHit.Clear();

        //// ensure collider (if any) is enabled on spawn
        //var col = GetComponent<Collider>();
        //if (col != null) col.enabled = true;
    }

    public override void OnDespawned()
    {
        // cleanup runtime state when returned to pool
        targetsHit.Clear();
        StopAllCoroutines();
        var col = GetComponent<Collider>();
        if (col != null) col.enabled = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other == null || other.gameObject == null)
            return;

        //if (meleeData == null) return;

        if (other.CompareTag(validTargetTag))
        {
            if (!targetsHit.ContainsKey(other))
            {
                targetsHit.Add(other, 1);
                ApplyAbilityDamage(other.gameObject);
            }
        }
    }

    // ----------------------------------------------
    // ABILITY INSTANCE SETUP
    // ----------------------------------------------
    public override AbilitySO GetAbilitySO()
    {
        return meleeData;
    }

    // ----------------------------------------------
    // GENERAL ABILITY FUNCTIONS
    // ----------------------------------------------

    //Spawn Ability - Melee Attack
    public override IEnumerator SpawnAbility(Vector3 userCurrentPosition, Vector3 hitPoint)
    {

        //Spawning Melee Attacks
        for (int multiCount = 0; multiCount < meleeData.multipleAttacks; multiCount++)
        {
            Vector3 spawnPosition = transform.position;
            Quaternion spawnRotation = GetAbilitySpawnRotation(userCurrentPosition, hitPoint, multiCount);
            //Setting Spawning Position & Rotation according Burst & MultiProjectiles
            if (meleeData.centerSelf)
            {
                spawnPosition = Vector3.zero;
            }
            else
            {
                spawnPosition = GetAbilitySpawnPosition(userCurrentPosition, hitPoint, multiCount);
            }

            if (this.gameObject == null)
            {
                Debug.LogWarning("MeleeBehaviour.SpawnAbility: source gameObject is null, skipping spawn.");
                continue;
            }

            // Instantiate the melee attack instance directly (no pooling).
            GameObject meleeInstance = Instantiate(this.gameObject, spawnPosition, spawnRotation);
            var abilityBehaviour = meleeInstance.GetComponent<MeleeBehaviour>();
            if (abilityBehaviour != null)
            {
                abilityBehaviour.user = this.user;
                abilityBehaviour.validTargetTag = this.validTargetTag;
                abilityBehaviour.abilityHitPoint = hitPoint;
                abilityBehaviour.SetMeleeSpawnScale();

                if (meleeData.centerSelf)
                {
                    meleeInstance.transform.SetParent(user.transform);
                }
            }
            meleeInstance.SetActive(true);

            meleeData.currentComboChain++;
            if (meleeData.currentComboChain > meleeData.maxComboChain)
            {
                meleeData.currentComboChain = 1;
            }

        }

        yield return new WaitForSeconds(0);
    }

    public override void CalculateAbilityDamage()
    {
        // Calculate damage when Templated & User stats change so it matches the current user stats.
        if (user != null && meleeData != null)
            meleeData.abilityDamage = Calculator.CalculateUserAbilityDamage(meleeData, user.GetComponent<CharacterStats>());
        else
        {
            meleeData.abilityDamage = new float[5];
            Debug.LogWarning($"MeleeBahaviour.CalculateAbilityDamage: User not Set OR Projectile SO missing on '{gameObject.name}'.");
        }

    }

    public override void ApplyAbilityDamage(GameObject target)
    {
        if (target == null) return;
        if (meleeData == null) return;
        if (!target.TryGetComponent<CharacterStats>(out var stats)) return;
        // Don't apply damage to targets that are dead or currently dashing
        if (stats.isDead || stats.isDashing) return;


        critFlag = false;
        float[] abilityDamageTypedCrit = Calculator.CalculateUserAbilityCrit(meleeData, user.GetComponent<CharacterStats>(), meleeData.abilityDamage, critFlag);
        float[] abiliyAilmentDamage = Calculator.CalculateUserAbilityAilmentDamage(abilityDamageTypedCrit, meleeData, user.GetComponent<CharacterStats>());
        float abilityFullDamage = Calculator.CalculateAbilityFullDamage(abilityDamageTypedCrit);

        target.GetComponent<CharacterStats>().TakeDamage(abilityFullDamage);
        target.GetComponent<CharacterStats>().TakeAilmentDamage(abiliyAilmentDamage);
    }

    private void LifeTimeCheck(float spawnTime)
    {
        if (Time.time - spawnTime >= meleeData.lifeTime)
        {
            Destroy(gameObject);
        }
    }

    // ----------------------------------------------
    // MELEE SPECIFIC FUNCTIONS
    // ----------------------------------------------

    void MeleeMovement()
    {
        //Standard direction of Movement
        Vector3 direction = Vector3.forward;

        transform.Translate(direction * meleeData.projectSpeed * Time.deltaTime);
    }

    public Vector3 GetAbilitySpawnPosition(Vector3 userCurrentPosition, Vector3 hitPoint, int multiCount)
    {
        //Calculate Spawn Position with reach in mind
        Vector3 localSpawnPosition = ((hitPoint - userCurrentPosition).normalized) * (1 + meleeData.reachMultiplier);

        //Rotate Spawn Position according Multiple Attack rotation
        localSpawnPosition = Quaternion.AngleAxis(-meleeData.multipleAttacksSpread * (((float)(meleeData.multipleAttacks - 1) / 2) - multiCount), Vector3.up) * localSpawnPosition;
        return localSpawnPosition + userCurrentPosition;
    }

    public Quaternion GetAbilitySpawnRotation(Vector3 userCurrentPosition, Vector3 hitPoint, int multiCount)
    {
        //Calculate Spawn Rotation
        Quaternion spawnRotation = Quaternion.LookRotation((hitPoint - userCurrentPosition).normalized, Vector3.up);

        //Rotate Spawn Rotation according Multiple Attack rotation
        spawnRotation = Quaternion.AngleAxis(-meleeData.multipleAttacksSpread * (((float)(meleeData.multipleAttacks - 1) / 2) - multiCount), Vector3.up) * spawnRotation;

        return spawnRotation;
    }

    public void SetMeleeSpawnScale()
    {
        transform.localScale *= meleeData.meleeScaleIncrease;
    }

    public void SetMeleeMultiScale()
    {
        Vector3 spawnScale = transform.localScale * (1 + ( meleeData.comboSizeScale * (meleeData.currentComboChain - 1)) / meleeData.lifeTime * Time.deltaTime);

        transform.localScale = spawnScale;
    }
}
