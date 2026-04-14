using UnityEngine;
using System.Collections;

public class DashBehaviour : AbilityBehaviour
{
    // Dash Ability specific Variables
    public DashSO dashData;

    // ----------------------------------------------
    // RUNTIME FUNCTIONS (SETUP & ABILITY ACTIONS)
    // ----------------------------------------------
    public override void OnSpawned()
    {
        targetsHit.Clear();
    }

    public override void OnDespawned()
    {
        // clear runtime state when returned to the pool
        targetsHit.Clear();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other == null || other.gameObject == null)
            return;

        if (dashData == null) return;

        // Only handle valid targets we haven't hit yet
        if (other.CompareTag(validTargetTag) && !targetsHit.ContainsKey(other))
        {
            targetsHit.Add(other, 1);
            ApplyAbilityDamage(other.gameObject);
        }
    }

    // ----------------------------------------------
    // ABILITY INSTANCE SETUP
    // ----------------------------------------------
    public override AbilitySO GetAbilitySO()
    {
        return dashData;
    }

    // ----------------------------------------------
    // GENERAL ABILITY FUNCTIONS
    // ----------------------------------------------

    //Spawn Ability - Melee Attack
    public override IEnumerator SpawnAbility(Vector3 currentPosition, Vector3 hitPoint)
    {
    // Make sure the dash only pushes horizontally so we don't accidentally boost up
    Vector3 dashDirection = Vector3.ProjectOnPlane(user.transform.forward, Vector3.up).normalized;

    var pStats = user.GetComponent<PlayerStats>();
    if (pStats != null) pStats.isDashing = true;

    var rb = user.GetComponent<Rigidbody>();
    if (rb != null)
    {
        // NOTE: Player prefab has Rigidbody set to kinematic by default. Toggling isKinematic/useGravity
        // can cause solver discontinuities and the floating behavior you're seeing. Don't change those flags.

        // Use DashMover (MovePosition-based) to avoid stomping vertical velocity and to reduce solver popping
        DashMover mover = user.GetComponent<DashMover>();
        if (mover == null)
            mover = user.AddComponent<DashMover>();
        mover.Init(rb, dashDirection, dashData.dashForce, dashData.lifeTime);

        try
        {
            yield return new WaitForSeconds(dashData.lifeTime);
        }
        finally
        {
            // Ensure mover is removed
            if (mover != null)
                Object.Destroy(mover);

            // If the Rigidbody is non-kinematic, clear horizontal velocity so physics can settle naturally.
            // If it's kinematic this will be ignored, which is fine.
            Vector3 v = rb.linearVelocity;
            v.x = 0f;
            v.z = 0f;
            rb.linearVelocity = v;
        }
    }
    else
    {
        // No Rigidbody on the player, so no physics dash — just wait the lifetime out
        yield return new WaitForSeconds(dashData.lifeTime);
    }

    if (pStats != null) pStats.isDashing = false;
    }

    public override void CalculateAbilityDamage()
    {
        // Update damage numbers when user stats change so they're always accurate
        if (user != null && dashData != null)
            dashData.abilityDamage = Calculator.CalculateUserAbilityDamage(dashData, user.GetComponent<CharacterStats>());
        else
        {
            dashData.abilityDamage = new float[5];
        }

    }

    public override void ApplyAbilityDamage(GameObject target)
    {
        if (target == null) return;
        if (dashData == null) return;
        if (!target.TryGetComponent<CharacterStats>(out var stats)) return;
        // Don't hit targets that are dead or dashing themselves
        if (stats.isDead || stats.isDashing) return;


        critFlag = false;
        float[] abilityDamageTypedCrit = Calculator.CalculateUserAbilityCrit(dashData, user.GetComponent<CharacterStats>(), dashData.abilityDamage, critFlag);
        float[] abiliyAilmentDamage = Calculator.CalculateUserAbilityAilmentDamage(abilityDamageTypedCrit, dashData, user.GetComponent<CharacterStats>());
        float abilityFullDamage = Calculator.CalculateAbilityFullDamage(abilityDamageTypedCrit);

        target.GetComponent<CharacterStats>().TakeDamage(abilityFullDamage);
        target.GetComponent<CharacterStats>().TakeAilmentDamage(abiliyAilmentDamage);
    }

    // ----------------------------------------------
    // DASH SPECIFIC FUNCTIONS
    // ----------------------------------------------
}
