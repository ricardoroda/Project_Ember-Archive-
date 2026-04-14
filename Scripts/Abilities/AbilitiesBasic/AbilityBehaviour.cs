using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public abstract class AbilityBehaviour : MonoBehaviour
{
    //General Ability Variables
    public Dictionary<Collider, int> targetsHit = new Dictionary<Collider, int>();
    public GameObject user;

    //Damage Logistics
    public bool critFlag = false;
    public string validTargetTag;

    // Mark this instance as the template placed in the scene by AbilityHandler.
    // Templates are not part of the pool and should not be enqueued.
    public bool isTemplate = false;

    //Trigger Variables
    public GameObject abilityTrigger;
    public Vector3 abilityHitPoint;
    public bool isTrigger = false;
    
    //Trigger Abilities Prefabs
    [Header("Trigger Abilities Prefabs")]
    public List<GameObject> triggerAbillitiesOnCastPrefab;
    public List<GameObject> triggerAbillitiesOnHitPrefab;
    public List<GameObject> triggerAbillitiesOnDestroyPrefab;
    public List<GameObject> triggerAbillitiesOnRepeatPrefab;
    public List<GameObject> triggerAbillitiesOnCritPrefab;
    //Trigger Abilities Instance Reference
    [Header("Trigger Abilities Instance Reference")]
    public List<GameObject> triggerAbillitiesOnCastInstance;
    public List<GameObject> triggerAbillitiesOnHitInstance;
    public List<GameObject> triggerAbillitiesOnDestroyInstance;
    public List<GameObject> triggerAbillitiesOnRepeatInstance;
    public List<GameObject> triggerAbillitiesOnCritInstance;

    //General Ability Functions
    public abstract IEnumerator SpawnAbility(Vector3 currentPosition, Vector3 hitPoint);
    public abstract AbilitySO GetAbilitySO();
    public abstract void CalculateAbilityDamage();
    public abstract void ApplyAbilityDamage(GameObject target);

    // ----------------------------------------------
    // TRIGGER FUNCTIONS
    // ----------------------------------------------
    public void ProcTriggeredAbillitiesOnCast(Vector3 hitPoint)
    {
        foreach (GameObject triggeredAbility in triggerAbillitiesOnCastInstance)
        {
            triggeredAbility.GetComponent<AbilityBehaviour>().abilityTrigger = gameObject;
            triggeredAbility.GetComponent<AbilityBehaviour>().abilityHitPoint = abilityHitPoint;
            StartCoroutine(triggeredAbility.GetComponent<AbilityBehaviour>().SpawnAbility(new Vector3(user.transform.position.x, 1, user.transform.position.z), hitPoint));
        }
    }


    public void ProcTriggeredAbillitiesOnHit(Vector3 hitPoint)
    {
        foreach(GameObject triggeredAbility in triggerAbillitiesOnHitInstance)
        {
            triggeredAbility.GetComponent<AbilityBehaviour>().abilityTrigger = gameObject;
            triggeredAbility.GetComponent<AbilityBehaviour>().abilityHitPoint = abilityHitPoint;
            StartCoroutine(triggeredAbility.GetComponent<AbilityBehaviour>().SpawnAbility(new Vector3(user.transform.position.x, 1, user.transform.position.z), hitPoint));
        }
    }

    public void ProcTriggeredAbillitiesOnDestroy(Vector3 hitPoint)
    {
        foreach (GameObject triggeredAbility in triggerAbillitiesOnDestroyInstance)
        {
            triggeredAbility.GetComponent<AbilityBehaviour>().abilityTrigger = gameObject;
            triggeredAbility.GetComponent<AbilityBehaviour>().abilityHitPoint = abilityHitPoint;
            StartCoroutine(triggeredAbility.GetComponent<AbilityBehaviour>().SpawnAbility(new Vector3(user.transform.position.x, 1, user.transform.position.z), hitPoint));
        }
    }

    public void ProcTriggeredAbillitiesOnRepeat(Vector3 hitPoint)
    {
        foreach (GameObject triggeredAbility in triggerAbillitiesOnRepeatInstance)
        {
            triggeredAbility.GetComponent<AbilityBehaviour>().abilityTrigger = gameObject;
            triggeredAbility.GetComponent<AbilityBehaviour>().abilityHitPoint = abilityHitPoint;
            StartCoroutine(triggeredAbility.GetComponent<AbilityBehaviour>().SpawnAbility(new Vector3(user.transform.position.x, 1, user.transform.position.z), hitPoint));
        }
    }

    public void ProcTriggeredAbillitiesOnCrit(Vector3 hitPoint)
    {
        foreach (GameObject triggeredAbility in triggerAbillitiesOnCritInstance)
        {
            triggeredAbility.GetComponent<AbilityBehaviour>().abilityTrigger = gameObject;
            triggeredAbility.GetComponent<AbilityBehaviour>().abilityHitPoint = abilityHitPoint;
            StartCoroutine(triggeredAbility.GetComponent<AbilityBehaviour>().SpawnAbility(new Vector3(user.transform.position.x, 1, user.transform.position.z), hitPoint));
        }
    }

    public void ClearTriggeredAbilitiesInstances()
    {
        foreach (GameObject triggeredAbility in triggerAbillitiesOnCastInstance)
        {
            Destroy(triggeredAbility);
        }

        foreach (GameObject triggeredAbility in triggerAbillitiesOnHitInstance)
        {
            Destroy(triggeredAbility);
        }

        foreach (GameObject triggeredAbility in triggerAbillitiesOnDestroyInstance)
        {
            Destroy(triggeredAbility);
        }

        foreach (GameObject triggeredAbility in triggerAbillitiesOnRepeatInstance)
        {
            Destroy(triggeredAbility);
        }

        foreach (GameObject triggeredAbility in triggerAbillitiesOnCritInstance)
        {
            Destroy(triggeredAbility);
        }

    }

    // Hooks called by the pool. These defaults are simple — override them in
    // abilities that need to reset particles, timers, or other runtime state.
    public virtual void OnSpawned() {
        // Default reset when spawned from the pool: clear tracked hits.
        targetsHit.Clear();
    }

    public virtual void OnDespawned() {
        // Default cleanup when returned to the pool.
        targetsHit.Clear();
        ClearTriggeredAbilitiesInstances();
    }

}
