using System;
using System.Collections;
using Unity.VisualScripting;
// using UnityEditor.Overlays;
using UnityEngine;
using UnityEngine.UIElements;

public class ProjectileBehaviour : AbilityBehaviour
{
    // Projectile Ability specific Variables
    public ProjectileSO projectileData;

    private float spawnTime;

    // ----------------------------------------------
    // RUNTIME FUNCTIONS (SETUP & ABILITY ACTIONS)
    // ----------------------------------------------
    void Start()
    {
        spawnTime = Time.time;
        ProcTriggeredAbillitiesOnCast(abilityHitPoint);
    }

    void Update()
    {
        //Projectile Movement
        HommingMovement(projectileData.homming);
        ProjectileMovement();

        //Projectile States
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

        if (projectileData == null) return;

        if (other.CompareTag(validTargetTag))
        {
            if (!targetsHit.ContainsKey(other))
            {
                targetsHit.Add(other, 1);
                ApplyAbilityDamage(other.gameObject);

                if (!projectileData.penetrate)
                {
                    Destroy(gameObject);
                }
            }
            
        }
    }

    // ----------------------------------------------
    // ABILITY INSTANCE SETUP
    // ----------------------------------------------

    public override AbilitySO GetAbilitySO()
    {
        return projectileData;
    }

    // ----------------------------------------------
    // GENERAL ABILITY FUNCTIONS
    // ----------------------------------------------

    //Spawn Ability - Shoot Projectile
    public override IEnumerator SpawnAbility(Vector3 userCurrentPosition, Vector3 hitPoint)
    {

        //Spawning projectiles
        for (int burstCount = 0; burstCount < projectileData.burst; burstCount++)
        {
            for (int multiCount = 0; multiCount < projectileData.numberOfProjectiles; multiCount++)
            {
                // set spawn position and rotation
                Vector3 spawnPosition = GetAbilitySpawnPosition(userCurrentPosition, hitPoint, burstCount, multiCount);
                Quaternion spawnRotation = GetAbilitySpawnRotation(userCurrentPosition, hitPoint, multiCount);

                if (this.gameObject == null)
                {
                    Debug.LogWarning("ProjectileBehaviour.SpawnAbility: source gameObject is null, skipping spawn.");
                }
                else
                {
                    // Instantiate a fresh instance from the template/prefab.
                    GameObject projectileInstance = Instantiate(this.gameObject, spawnPosition, spawnRotation);
                    var abilityBehaviour = projectileInstance.GetComponent<ProjectileBehaviour>();
                    if (abilityBehaviour != null)
                    {

                        abilityBehaviour.user = this.user;
                        abilityBehaviour.validTargetTag = this.validTargetTag;
                        abilityBehaviour.abilityHitPoint = hitPoint;
                        abilityBehaviour.SetProjectileSpawnScale();
                    }
                    projectileInstance.SetActive(true);

                }

            }
        }
        yield return new WaitForSeconds(0);
    }

    public override void CalculateAbilityDamage()
    {
        // Calculate damage when Templated & User stats change so it matches the current user stats.
        if (user != null && projectileData != null)
            projectileData.abilityDamage = Calculator.CalculateUserAbilityDamage(projectileData, user.GetComponent<CharacterStats>());
        else
        {
            projectileData.abilityDamage = new float[5];
            Debug.LogWarning($"ProjectileBahaviour.CalculateAbilityDamage: User not Set OR Projectile SO missing on '{gameObject.name}'.");
        }

    }

    public override void ApplyAbilityDamage(GameObject target)
    {
        if (target == null) return;
        if (projectileData == null) return;
        if (!target.TryGetComponent<CharacterStats>(out var stats)) return;
        // Don't apply damage to targets that are dead or currently dashing
        if (stats.isDead || stats.isDashing) return;


        critFlag = false;
        float[] abilityDamageTypedCrit = Calculator.CalculateUserAbilityCrit(projectileData, user.GetComponent<CharacterStats>(), projectileData.abilityDamage, critFlag);
        float[] abiliyAilmentDamage = Calculator.CalculateUserAbilityAilmentDamage(abilityDamageTypedCrit, projectileData, user.GetComponent<CharacterStats>());
        float abilityFullDamage = Calculator.CalculateAbilityFullDamage(abilityDamageTypedCrit);

        target.GetComponent<CharacterStats>().TakeDamage(abilityFullDamage);
        target.GetComponent<CharacterStats>().TakeAilmentDamage(abiliyAilmentDamage);
    }

    private void LifeTimeCheck(float spawnTime)
    {
        if (Time.time - spawnTime >= projectileData.lifeTime)
        {
            Destroy(gameObject);
        }
    }

    // ----------------------------------------------
    // PROJECTILE SPECIFIC FUNCTIONS
    // ----------------------------------------------

    void ProjectileMovement()
    {
        //Standard direction of Movement
        Vector3 direction = Vector3.forward;

        transform.Translate(direction * projectileData.baseSpeed * Time.deltaTime);
    }

    public void SetProjectileSpawnScale()
    {
        transform.localScale *= projectileData.projectileScaleIncrease;
    }
    private void HommingMovement(bool hommingBool)
    {
        //Add homming correction if enabled
        if (hommingBool)
        {
            Collider[] enemiesInRange = Physics.OverlapSphere(transform.position, projectileData.hommingRadius);

            float angle = 0;
            float distance = 0;
            Collider enemyTargeted = null;


            //Find closest target enemy
            foreach (Collider enemy in enemiesInRange)
            {
                if (enemy.gameObject.CompareTag(projectileData.targetTags.ToString()))
                {
                    float enemyDistance = (new Vector3((enemy.transform.position.x - transform.position.x), 0, (enemy.transform.position.z - transform.position.z))).magnitude;
                    if (distance > enemyDistance || distance == 0)
                    {
                        distance = enemyDistance;
                        enemyTargeted = enemy;
                    }
                }

            }

            //Calculate angle between the direction and the target
            if (enemyTargeted != null)
            {
                angle = Vector3.SignedAngle(transform.TransformDirection(Vector3.forward), (new Vector3((enemyTargeted.transform.position.x - transform.position.x), 0, (enemyTargeted.transform.position.z - transform.position.z))), Vector3.up);

                ////Check if the angle correction is higher than the curving speed
                if (Mathf.Abs(angle) > projectileData.hommingTurnRate)
                {
                    angle = projectileData.hommingTurnRate * (angle / Mathf.Abs(angle));
                }

                //Correct rotate object according to the angle
                transform.Rotate(Vector3.up, angle);
            }
        }
    }

    public Vector3 GetAbilitySpawnPosition(Vector3 userCurrentPosition, Vector3 hitPoint, int burstCount, int multiCount)
    {
        //Calculate Spawn Position with burst iteration in mind
        Vector3 localSpawnPosition = ((hitPoint - userCurrentPosition).normalized) * (1 + projectileData.distanceBetweenBurst * burstCount);

        //Rotate Spawn Position according Multi projectile rotation
        localSpawnPosition = Quaternion.AngleAxis(-projectileData.spreadBetweenProjectiles * (((float)(projectileData.numberOfProjectiles - 1) / 2) - multiCount), Vector3.up) * localSpawnPosition;
        return localSpawnPosition + userCurrentPosition;
    }

    public Quaternion GetAbilitySpawnRotation(Vector3 userCurrentPosition, Vector3 hitPoint, int multiCount)
    {
        //Calculate Spawn Rotation
        Quaternion spawnRotation = Quaternion.LookRotation((hitPoint - userCurrentPosition).normalized, Vector3.up);

        //Rotate Spawn Rotation according Multi projectile rotation
        spawnRotation = Quaternion.AngleAxis(-projectileData.spreadBetweenProjectiles * (((float)(projectileData.numberOfProjectiles - 1) / 2) - multiCount), Vector3.up) * spawnRotation;

        return spawnRotation;
    }
}

