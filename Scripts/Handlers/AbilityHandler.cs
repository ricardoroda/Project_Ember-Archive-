// Scene gating moved to GameController; SceneManagement not needed here
using System;
using System.Collections;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor.Overlays;
#endif
using UnityEngine;
using UnityEngine.InputSystem;
using static EquipmentTagsData;

public class AbilityHandler : MonoBehaviour {
    // Scene gating is handled by GameController. AbilityHandler only exposes enable/disable methods.
    [Header("UI")]
    [Tooltip("Optional UI GameObject (e.g., an icon or panel) that will be active when abilities are disabled.")]
    public GameObject abilityDisabledIndicator;
    // Event emitted when abilities enabled state changes. Parameter = abilitiesEnabled
    public event Action<bool> OnAbilitiesEnabledChanged;
    // Runtime toggle for whether ability input is allowed

    //Player Ability Calculation Variables
    public Camera MainCamera;
    private Plane hitPlane = new Plane(Vector3.up, Vector3.up);
    private Vector3 playerCurrentPosition;
    private Vector3 hitPoint;
    private PlayerStats playerStats;

    //Abilities & UseAbility variables
    public GameObject[] abilitiesPrefab = new GameObject[8];
    public GameObject[] abilities = new GameObject[8];
    public AbilitySO[] abilityData = new AbilitySO[8];
    private bool[] abilityRefreshedFlag = new bool[8];
    private float[] cooldownTimer = { 0, 0, 0, 0, 0, 0, 0, 0 };

    //Triggered Abilities & UseTriggeredAbility variables
    public GameObject lightningStrikePrefab;
    //public GameObject lightningStrike;
    public GameObject burnExplosionPrefab;
    public GameObject burnExplosion;
    public List<GameObject> triggerAbilitiesPrefab;
    public List<GameObject> triggerabilities;

    //Ability Instance Holder Variables
    private GameObject abilitiesInstancesHolder;
    private GameObject[] lastKnownTemplates = new GameObject[8];
    private bool abilitiesEnabled = true;

    //Input Variables
    private InputSystemActions inputActions;
    private InputAction[] abilityUse = new InputAction[8];

    // ----------------------------------------------
    // ON RUNTIME FUNCTIONS
    // ----------------------------------------------
    private void Awake() {
        inputActions = new InputSystemActions();
    }

    private void OnEnable()
    {
        //Bind Input Actions to Abilities
        abilityUse[0] = inputActions.Player.Dash;
        abilityUse[1] = inputActions.Player.Ability1;
        abilityUse[2] = inputActions.Player.Ability2;
        abilityUse[3] = inputActions.Player.Ability3;
        abilityUse[4] = inputActions.Player.Ability4;
        abilityUse[5] = inputActions.Player.Ability5;
        abilityUse[6] = inputActions.Player.Item1;
        abilityUse[7] = inputActions.Player.Item2;

        foreach (InputAction abilityCast in abilityUse)
        {
            if (abilityCast != null)
            {
                abilityCast.Enable();
            }
        }
    }

    private void OnDisable() {
        foreach (InputAction abilityCast in abilityUse)
        {
            if (abilityCast != null)
            {
                abilityCast.Disable();
            }
        }
    }

    public void DisableAbilities()
    {
        abilitiesEnabled = false;
        foreach (var a in abilityUse) if (a != null) a.Disable();
        if (abilityDisabledIndicator != null) abilityDisabledIndicator.SetActive(true);
        OnAbilitiesEnabledChanged?.Invoke(false);
    }

    public void EnableAbilities()
    {
        abilitiesEnabled = true;
        foreach (var a in abilityUse) if (a != null) a.Enable();
        if (abilityDisabledIndicator != null) abilityDisabledIndicator.SetActive(false);
        OnAbilitiesEnabledChanged?.Invoke(true);
    }


    void Start()
    {
        MainCamera = Camera.main;
        playerStats = GetComponent<PlayerStats>(); 

        //Get Abilities SO's & Setup Abilities
        abilitiesInstancesHolder = GameObject.FindGameObjectWithTag("AbilitiesInstancesHolder");

        var actionBar = FindFirstObjectByType<ActionBar>();
        if (actionBar != null)
        {
            actionBar.SyncWithAbilityHandler();
        }

        // Initial ability state is controlled by GameController; just notify listeners that abilities are enabled by default.
        OnAbilitiesEnabledChanged?.Invoke(abilitiesEnabled);
    }

    void Update()
    {
        playerCurrentPosition = new Vector3(transform.position.x, 1, transform.position.z);
        hitPoint = GetHitPoint(playerCurrentPosition);

        for (int i = 0; i < abilities.Length; i++)
        {
            // monitor changes to ability template references
            if (lastKnownTemplates.Length > i)
            {
                if (lastKnownTemplates[i] != null && abilities[i] == null)
                {
                    // Template reference changed for slot i. Log a stack trace to help find the code path that caused it.
                    Debug.LogWarning($"AbilityHandler: Template for slot {i} went missing at runtime. Stack:\n" + System.Environment.StackTrace);
                    lastKnownTemplates[i] = null;
                }
                else if (lastKnownTemplates[i] != abilities[i])
                {
                    lastKnownTemplates[i] = abilities[i];
                }
            }

            if (abilityUse[i] != null && abilityUse[i].ReadValue<float>() != 0f)
            {
                if (!abilitiesEnabled) continue;

                if (!playerStats.isFrozen && !playerStats.isFrozen)
                {
                    UseAbility(i);
                }
            }

            AbilityRefreshTimer(i);
        }
    }

    // ----------------------------------------------
    // GENERAL HANDLE ABILITIES FUNCTIONS
    // ----------------------------------------------
    Vector3 GetHitPoint(Vector3 playerCurrentPosition) {
        float enter = 0.0f;
        Vector3 hitPoint = Vector3.zero;
        if (MainCamera == null) return hitPoint;
        Ray ray = MainCamera.ScreenPointToRay(Input.mousePosition);
        if (hitPlane.Raycast(ray, out enter)) {
            hitPoint = ray.GetPoint(enter);
            Debug.DrawLine(playerCurrentPosition, hitPoint, Color.red);
        }
        return hitPoint;
    }

    private void UseAbility(int index) {

        //Fire Rate Ability
        if (abilityRefreshedFlag[index] && !(abilityData[index].cooldownFlag) && !(abilityData[index].weaponBasedActionSpeedFlag) && playerStats.currentMana >= abilityData[index].manaCost)
        {
            abilityRefreshedFlag[index] = false;
            playerStats.UseMana(abilityData[index].manaCost);
            cooldownTimer[index] = 1 / (abilityData[index].fireRate * playerStats.currentActionSpeed);
            StartCoroutine(abilities[index].GetComponent<AbilityBehaviour>().SpawnAbility(playerCurrentPosition, hitPoint));
        }

        //Cooldown Ability
        if (abilityRefreshedFlag[index] && abilityData[index].cooldownFlag && !(abilityData[index].weaponBasedActionSpeedFlag) && playerStats.currentMana >= abilityData[index].manaCost)
        {
            abilityRefreshedFlag[index] = false;
            playerStats.UseMana(abilityData[index].manaCost);
            cooldownTimer[index] = abilityData[index].cooldown / playerStats.currentCooldownReduction;
            StartCoroutine(abilities[index].GetComponent<AbilityBehaviour>().SpawnAbility(playerCurrentPosition, hitPoint));
        }

        //Weapon Based Ability
        if (abilityRefreshedFlag[index] && !(abilityData[index].cooldownFlag) && abilityData[index].weaponBasedActionSpeedFlag && playerStats.currentMana >= abilityData[index].manaCost)
        {
            if(playerStats.weaponActionSpeed > 0)
            {
                //Check if equipped weapons correspond to right weapon types
                foreach(WeaponTags weaponType in playerStats.weaponTypesEquipped)
                {
                    bool weaponCheck = abilityData[index].weaponTypesUsable.Contains(weaponType);
                    if (!weaponCheck)
                    {
                        Debug.Log("Wrong weapon Type Equipped!");
                        return;
                    }

                }

                abilityRefreshedFlag[index] = false;
                playerStats.UseMana(abilityData[index].manaCost);
                cooldownTimer[index] = 1 / (playerStats.currentWeaponActionSpeed);
                StartCoroutine(abilities[index].GetComponent<AbilityBehaviour>().SpawnAbility(playerCurrentPosition, hitPoint));
            }
            else
            {
                Debug.Log("No Weapon Equiped!");
            }
        }
    }

    private void AbilityRefreshTimer(int index) {
        if (!abilityRefreshedFlag[index]) {
            cooldownTimer[index] -= Time.deltaTime;
            if (cooldownTimer[index] < 0) cooldownTimer[index] = 0;
            if (cooldownTimer[index] <= 0) abilityRefreshedFlag[index] = true;
        }
    }

    // ----------------------------------------------
    // ABILITY INSTANCE SETUP
    // ----------------------------------------------
    public void SetUpAbilityInstance(int index)
    {
        if (abilitiesPrefab == null || index < 0 || index >= abilitiesPrefab.Length || abilitiesPrefab[index] == null) return;
        if (abilitiesInstancesHolder == null) abilitiesInstancesHolder = GameObject.FindGameObjectWithTag("AbilitiesInstancesHolder");

        GameObject abilityInstance = Instantiate(abilitiesPrefab[index]);
        if (abilityInstance == null) return;
        abilities[index] = abilityInstance;

        if (abilities[index].GetComponent<AbilityBehaviour>() != null)
        {
            abilities[index].GetComponent<AbilityBehaviour>().user = gameObject;
            abilities[index].GetComponent<AbilityBehaviour>().validTargetTag = "Enemy";
            abilities[index].GetComponent<AbilityBehaviour>().isTemplate = true;
            abilities[index].GetComponent<AbilityBehaviour>().CalculateAbilityDamage();
            playerStats.OnStatsChanged += abilities[index].GetComponent<AbilityBehaviour>().CalculateAbilityDamage;
        }

        abilities[index].name = $"AbilityTemplate_slot{index}_{abilitiesPrefab[index].name}";
        abilities[index].SetActive(false);

        if (abilitiesInstancesHolder != null)
            abilities[index].transform.SetParent(abilitiesInstancesHolder.transform, false);

        SetupTriggeredAbilities(index);
    }

    public void SetupTriggeredAbilities(int index)
    {
        if (abilitiesInstancesHolder == null) abilitiesInstancesHolder = GameObject.FindGameObjectWithTag("AbilitiesInstancesHolder");

        AbilityBehaviour abilityBehaviour = abilities[index].GetComponent<AbilityBehaviour>();
        //public List<GameObject> triggerAbillitiesOnCastPrefab;
        //public List<GameObject> triggerAbillitiesOnHitPrefab;
        //public List<GameObject> triggerAbillitiesOnDestroyPrefab;
        //public List<GameObject> triggerAbillitiesOnRepeatPrefab;
        //public List<GameObject> triggerAbillitiesOnCritPrefab;

        foreach (GameObject triggeredAbilityPrefab in abilityBehaviour.triggerAbillitiesOnCastPrefab)
        {
            if (triggeredAbilityPrefab == null) continue;

            GameObject triggeredAbilityInstance = Instantiate(triggeredAbilityPrefab);
            if (triggeredAbilityInstance.GetComponent<AbilityBehaviour>() != null)
            {
                triggeredAbilityInstance.GetComponent<AbilityBehaviour>().user = gameObject;
                triggeredAbilityInstance.GetComponent<AbilityBehaviour>().validTargetTag = "Enemy";
                triggeredAbilityInstance.GetComponent<AbilityBehaviour>().isTemplate = true;
                triggeredAbilityInstance.GetComponent<AbilityBehaviour>().CalculateAbilityDamage();
                playerStats.OnStatsChanged += triggeredAbilityInstance.GetComponent<AbilityBehaviour>().CalculateAbilityDamage;

                triggeredAbilityInstance.GetComponent<AbilityBehaviour>().isTrigger = true;
                triggeredAbilityInstance.GetComponent<AbilityBehaviour>().abilityTrigger = abilities[index];


                triggeredAbilityInstance.name = $"AbilityTemplate_slot{index}_OnCastTriggeredAbility_{triggeredAbilityInstance.name}";
                triggeredAbilityInstance.SetActive(false);

                abilityBehaviour.triggerAbillitiesOnCastInstance.Add(triggeredAbilityInstance);

                if (abilitiesInstancesHolder != null)
                    triggeredAbilityInstance.transform.SetParent(abilitiesInstancesHolder.transform, false);
            }
        }
    }

    // ----------------------------------------------
    // AILMENT EFFECT TRIGGERS
    // ----------------------------------------------

    public void TriggerSelfLightningStrike(float lightningStrikeDamage)
    {
        if (abilitiesInstancesHolder == null) abilitiesInstancesHolder = GameObject.FindGameObjectWithTag("AbilitiesInstancesHolder");
        //Setting Spawning Position & Rotation
        Vector3 spawnPosition = new Vector3(transform.position.x, 0, transform.position.z);
        Quaternion spawnRotation = transform.rotation;

        if (lightningStrikePrefab == null)
        {
            Debug.LogWarning("AbilityHandler.TriggeredSelfLightningStrike: Lightning String Template not set.");
        }
        else
        {
            // Instantiate the ability as a new GameObject
            GameObject lightningStrikeInstance = Instantiate(lightningStrikePrefab, spawnPosition, spawnRotation);
            var abilityBehaviour = lightningStrikeInstance.GetComponent<AoeExplosionBehaviour>();
            if (abilityBehaviour != null)
            {
                abilityBehaviour.user = gameObject;
                abilityBehaviour.isTrigger = true;
                abilityBehaviour.validTargetTag = "Player";
                abilityBehaviour.AOEData.abilityDamage[2] = lightningStrikeDamage;
            }
            lightningStrikeInstance.SetActive(true);
        }
    }
    public void TriggerEnemyLightningStrike(GameObject enemy, float lightningStrikeDamage)
    {
        if (lightningStrikePrefab == null)
        {
            Debug.LogWarning("AbilityHandler.TriggeredSelfLightningStrike: Lightning String Template not set.");
        }
        else
        {
            // Variation in Spawn Position
            var abilityBehaviourPrefab = lightningStrikePrefab.GetComponent<AoeExplosionBehaviour>();
            float x = UnityEngine.Random.Range(-abilityBehaviourPrefab.AOEData.radius/2, abilityBehaviourPrefab.AOEData.radius / 2);
            float z = UnityEngine.Random.Range(-abilityBehaviourPrefab.AOEData.radius / 2, abilityBehaviourPrefab.AOEData.radius / 2);

            //Setting Spawning Position & Rotation
            Vector3 spawnPosition = new Vector3(enemy.transform.position.x + x, 0, enemy.transform.position.z + z);
            Quaternion spawnRotation = transform.rotation;

            // Instantiate the ability as a new GameObject
            GameObject lightningStrikeInstance = Instantiate(lightningStrikePrefab, spawnPosition, spawnRotation);
            var abilityBehaviour = lightningStrikeInstance.GetComponent<AoeExplosionBehaviour>();
            if (abilityBehaviour != null)
            {
                abilityBehaviour.user = gameObject;
                abilityBehaviour.isTrigger = true;
                abilityBehaviour.validTargetTag = "Enemy";
                abilityBehaviour.AOEData.baseAirDamage = Mathf.CeilToInt(lightningStrikeDamage);
                abilityBehaviour.CalculateAbilityDamage();
            }
            lightningStrikeInstance.SetActive(true);
        }
    }
}
