using System.Collections;
using UnityEngine;

public class AoeExplosionBehaviour : AbilityBehaviour
{
    // AOE Explosion Ability specific Variables
    public AoeExplosionSO AOEData;
    private float spawnTime;

    // ----------------------------------------------
    // RUNTIME FUNCTIONS (SETUP & ABILITY ACTIONS)
    // ----------------------------------------------

    void Start()
    {
        spawnTime = Time.time;

        if (AOEData != null && AOEData.delayedAOE)
        {
            StartCoroutine(DelayedAOE(AOEData.delayTime));
        }
    }
    void Update()
    {

        //Variation of Scale during activation
        ChangeAbilityScale();

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
        if (AOEData == null) return;
        if (other.CompareTag(validTargetTag))
        {
            // ignore if the collider's GameObject has been destroyed
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
        return AOEData;
    }

    // ----------------------------------------------
    // GENERAL ABILITY FUNCTIONS
    // ----------------------------------------------

    //Spawn Ability - AOE Explosion
    public override IEnumerator SpawnAbility(Vector3 userCurrentPosition, Vector3 hitPoint)
    {

        //Spawning AOE Explosion
        for (int multiCount = 1; multiCount <= AOEData.repetitionCount; multiCount++)
        {
            //Setting Spawning Position & Rotation
            Vector3 spawnPosition = GetAbilitySpawnPosition(new Vector3 (hitPoint.x, 0.1f, hitPoint.z));
            Quaternion spawnRotation = GetAbilitySpawnRotation(userCurrentPosition, hitPoint);

            if(AOEData.centerSelf)
            {
                spawnPosition = user.transform.position;
            }

            if(multiCount > 1)
            {
                yield return new WaitForSeconds(AOEData.repetitionDelay);
            }
            if (this.gameObject == null)
            {
                Debug.LogWarning("AoeExplosionBehaviour.SpawnAbility: source gameObject is null, skipping spawn.");
            }
            else
            {
                // Instantiate the ability as a new GameObject
                GameObject AOEInstance = Instantiate(this.gameObject, spawnPosition, spawnRotation);
                var abilityBehaviour = AOEInstance.GetComponent<AbilityBehaviour>();
                if (abilityBehaviour != null)
                {
                    abilityBehaviour.user = this.user;
                    abilityBehaviour.validTargetTag = this.validTargetTag;
                    abilityBehaviour.abilityHitPoint = hitPoint;
                }
                AOEInstance.SetActive(true);

                // Set Starting Scale with Multiple AOE in mind
                SetStartingScale(AOEInstance, multiCount);
            }
        }
    }

    public override void CalculateAbilityDamage()
    {
        // Calculate damage when Templated & User stats change so it matches the current user stats.
        if (user != null && AOEData != null)
            AOEData.abilityDamage = Calculator.CalculateUserAbilityDamage(AOEData, user.GetComponent<CharacterStats>());
        else
        {
            AOEData.abilityDamage = new float[5];
            Debug.LogWarning($"AoeExplosionBahaviour.CalculateAbilityDamage: User not Set OR Projectile SO missing on '{gameObject.name}'.");
        }

    }

    public override void ApplyAbilityDamage(GameObject target)
    {
        if (target == null) return;
        if (AOEData == null) return;
        if (!target.TryGetComponent<CharacterStats>(out var stats)) return;
        // Don't apply damage to targets that are dead or currently dashing
        if (stats.isDead || stats.isDashing) return;

        critFlag = false;
        float[] abilityDamageTypedCrit = Calculator.CalculateUserAbilityCrit(AOEData, user.GetComponent<CharacterStats>(), AOEData.abilityDamage, critFlag);
        float[] abiliyAilmentDamage = Calculator.CalculateUserAbilityAilmentDamage(abilityDamageTypedCrit, AOEData, user.GetComponent<CharacterStats>());
        float abilityFullDamage = Calculator.CalculateAbilityFullDamage(abilityDamageTypedCrit);

        target.GetComponent<CharacterStats>().TakeDamage(abilityFullDamage);
        target.GetComponent<CharacterStats>().TakeAilmentDamage(abiliyAilmentDamage);
    }

    private void LifeTimeCheck(float spawnTime)
    {
        if (Time.time - spawnTime >= AOEData.lifeTime)
        {
            // destroy the instance when its lifetime ends
            Destroy(gameObject);
        }
    }

    // ----------------------------------------------
    // AOE SPECIFIC FUNCTIONS
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

    public void SetStartingScale(GameObject instance, int repetitionCounter)
    {
        instance.transform.localScale *= (AOEData.radius + AOEData.repetitionScaleIncrease * (repetitionCounter - 1)) * AOEData.startingRadiusRelative;
    }

    public void ChangeAbilityScale()
    {
        Vector3 scale = transform.localScale * (1 + (1 - AOEData.startingRadiusRelative) / (AOEData.lifeTime * AOEData.startingRadiusRelative) * Time.deltaTime);

        transform.localScale = scale;
    }

    public IEnumerator DelayedAOE(float delayTime)
    {
        gameObject.GetComponent<Collider>().enabled = false;

        yield return new WaitForSeconds(delayTime);

        gameObject.GetComponent<Collider>().enabled = true;

        yield return new WaitForSeconds(0.1f);

        gameObject.GetComponent<Collider>().enabled = false;
    }
}
