using System;
using System.Collections;
#if UNITY_EDITOR
using UnityEditor.Overlays;
#endif
using UnityEngine;

public class AoePersistentBehaviour : AbilityBehaviour
{
    // AOE Persistent Ability specific Variables
    public AoePersistentSO AOEPData;
    private float spawnTime;
    private bool tickBool;

    // ----------------------------------------------
    // RUNTIME FUNCTIONS (SETUP & ABILITY ACTIONS)
    // ----------------------------------------------
    void Start()
    {
        spawnTime = Time.time;
 
        tickBool = true;
        if (AOEPData.delayedAOE == true)
        {
            DelayedAOE(AOEPData.delayTime);
        }
    }
    void Update()
    {
        //Rotation of self effects
        if (AOEPData.self == true)
        {
            transform.rotation = Quaternion.identity;
        }

        //Variation of Scale during activation
        ChangeAbilityScale();

        //EffectTick Timer
        if(tickBool)
        {
            targetsHit.Clear();
            StartCoroutine(TickRatePersistent(AOEPData.tickTime));
        }


        //AOE Explosion States
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

    private void OnTriggerStay(Collider other)
    {
        if (other == null) return;
        if (AOEPData == null) return;
        if (other.CompareTag(validTargetTag))
        {
            if (other.gameObject == null) return;
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
        return AOEPData;
    }

    // ----------------------------------------------
    // GENERAL ABILITY FUNCTIONS
    // ----------------------------------------------

    //Spawn Ability - AOE Explosion
    public override IEnumerator SpawnAbility(Vector3 userCurrentPosition, Vector3 hitPoint)
    {

        //Spawning AOE Explosion
        //Setting Spawning Position & Rotation
        Vector3 spawnPosition = GetAbilitySpawnPosition(new Vector3(hitPoint.x, 0.1f, hitPoint.z));
        Quaternion spawnRotation = GetAbilitySpawnRotation(userCurrentPosition, hitPoint);

        if (this.gameObject == null)
        {
            Debug.LogWarning("AoePersistentBehaviour.SpawnAbility: source gameObject is null, skipping spawn.");
        }
        else
        {
            // Instantiate the persistent AOE directly (no pooling).
            GameObject AOEPInstance = null;
            if (AOEPData.self == true)
            {
                AOEPInstance = Instantiate(this.gameObject, user.transform);
            }
            else if(AOEPData.triggeredAttach == true)
            {
                AOEPInstance = Instantiate(this.gameObject, abilityTrigger.transform);
            }
            else
            {
                AOEPInstance = Instantiate(this.gameObject, spawnPosition, spawnRotation);
            }

            var abilityBehaviour = AOEPInstance.GetComponent<AoePersistentBehaviour>();
            if (abilityBehaviour != null)
            {
                abilityBehaviour.user = this.user;
                abilityBehaviour.validTargetTag = this.validTargetTag;
                abilityBehaviour.abilityHitPoint = hitPoint;
            }
            AOEPInstance.SetActive(true);

            // Set Starting Scale for the spawned instance.
            SetStartingScale(AOEPInstance);
        }

        yield return new WaitForSeconds(0);
    }

    public override void CalculateAbilityDamage()
    {
        // Calculate damage when Templated & User stats change so it matches the current user stats.
        if (user != null && AOEPData != null)
        {
            AOEPData.abilityDamage = Calculator.CalculateUserAbilityDamage(AOEPData, user.GetComponent<CharacterStats>());
            for (int i = 0; i < AOEPData.abilityDamage.Length; i++)
            {
                AOEPData.abilityDamage[i] = (float)Math.Round(AOEPData.abilityDamage[i] * AOEPData.tickTime, 1);
            }
        }
            
        else
        {
            AOEPData.abilityDamage = new float[5];
            Debug.LogWarning($"AoePersistentBahaviour.CalculateAbilityDamage: User not Set OR Projectile SO missing on '{gameObject.name}'.");
        }

    }

    public override void ApplyAbilityDamage(GameObject target)
    {
        if (target == null) return;
        if (AOEPData == null) return;
        if (!target.TryGetComponent<CharacterStats>(out var stats)) return;
        // Don't apply damage to targets that are dead or currently dashing
        if (stats.isDead || stats.isDashing) return;

        critFlag = false;
        float[] abilityDamageTypedCrit = Calculator.CalculateUserAbilityCrit(AOEPData, user.GetComponent<CharacterStats>(), AOEPData.abilityDamage, critFlag);
        float[] abiliyAilmentDamage = Calculator.CalculateUserAbilityAilmentDamage(abilityDamageTypedCrit, AOEPData, user.GetComponent<CharacterStats>());
        float abilityFullDamage = Calculator.CalculateAbilityFullDamage(abilityDamageTypedCrit);

        target.GetComponent<CharacterStats>().TakeDamage(abilityFullDamage);
        target.GetComponent<CharacterStats>().TakeAilmentDamage(abiliyAilmentDamage);
    }

    private void LifeTimeCheck(float spawnTime)
    {
        if (Time.time - spawnTime >= AOEPData.lifeTime)
        {
            Destroy(gameObject);
        }
    }

    // ----------------------------------------------
    // AOEP SPECIFIC FUNCTIONS
    // ----------------------------------------------

    public Vector3 GetAbilitySpawnPosition(Vector3 hitPoint)
    {
        //Calculate Spawn Position with reach in mind
        Vector3 localSpawnPosition = hitPoint;

        return localSpawnPosition;
    }

    public Quaternion GetAbilitySpawnRotation(Vector3 userCurrentPosition, Vector3 hitPoint)
    {
        //Calculate Spawn Rotation
        Quaternion spawnRotation = Quaternion.LookRotation((hitPoint - userCurrentPosition).normalized, Vector3.up);

        return spawnRotation;
    }

    public void SetStartingScale(GameObject instance)
    {
        instance.transform.localScale *= (AOEPData.radius);
    }

    public void ChangeAbilityScale()
    {
    // Smoothly adjust the visible radius over the AOE's lifetime.
    Vector3 scale = transform.localScale * (1 + (1 - AOEPData.startingRadiusRelative) / (AOEPData.lifeTime * AOEPData.startingRadiusRelative) * Time.deltaTime);
    transform.localScale = scale;
    }

    public IEnumerator DelayedAOE(float delayTime)
    {
        gameObject.GetComponent<Collider>().enabled = false;

        yield return new WaitForSeconds(delayTime);

        gameObject.GetComponent<Collider>().enabled = true;
    }

    public IEnumerator TickRatePersistent(float tick)
    {
        tickBool = false;

        yield return new WaitForSeconds(tick);

        tickBool = true;
    }
}

