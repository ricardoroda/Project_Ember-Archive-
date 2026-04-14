using UnityEngine;
using UnityEngine.AI;
using TMPro;
using UnityEngine.UI;


public class EnemyStats : CharacterStats
{
    public EnemySO enemyData;

    // Opt-in pooling flag. When true, Die() will not Destroy the GameObject; instead the spawner/pool
    // will handle returning the instance to the pool. Set by the spawner when spawning pooled instances.
    [HideInInspector]
    public bool poolingEnabled = false;

    // Event fired when this enemy dies. Spawners/pools can subscribe to react and reclaim the instance.
    public event System.Action OnDeath;

    // (poolPrefab removed) No object pooling is used in this project version.

    //UI & UX
    [Header("Bar prefab")]
    public GameObject healthBarPrefab; 
    public Vector3 healthBarLocalPos = new Vector3(0f, 3f, 0f); // local position above head
    public GameObject armorBarPrefab;
    public Vector3 armorBarLocalPos = new Vector3(0f, 3.5f, 0f);

    [Header("Death Settings")]
    [Tooltip("Time in seconds before the enemy GameObject is destroyed after death (allows death animations/FX).")]
    public float deathDelay = 2f;

    //runtime references
    private GameObject healthBarGO;
    private Slider healthFill;
    private GameObject armorBarGO;
    private Slider armorFill;

    // ---------------------------
    // ENEMY SPECIFIC STATS
    // ---------------------------
    //Enemy Type
    public bool isBoss = false;

    //NaveMesh Agent Settings
    public float movementAcceleration;
    public float stoppingDistance;

    [SerializeField]public float sightRange = 1f;
    public float attackRange;

    //Enemy Logistic Stats
    [SerializeField] private float regenInterval = 2f;
    private bool armorBroken = false;
    private float lastDamageTime;
    private float armorRegenTime;

    //Ailments
    //IN ORDER: BURN MAX HP TICK / CHILL MAX HP PER STACK / SHOCK MAX HP LIGHTNING TRIGGER / DAZE MAX HP PER STACK / VOID MAX HP PER STACK
    public static float[] ailmentStackThresholdMinion = new float[5] { 0.01f, 0.03f, 0.01f, 0.025f, 0.02f };
    public static float[] ailmentStackThresholdBoss = new float[5] { 0.002f, 0.015f, 0.005f, 0.01f, 0.01f };
    public static float cleanseTime = 8f;

    //Player Rewards
    public int xpReward;
    public int goldReward;
    public int soulReward;

    // ---------------------------------------------------------------------------------------------------------------------------------------
    // ON RUNNING FUNCTIONS
    // ---------------------------------------------------------------------------------------------------------------------------------------
    void Start()
    {
        if(enemyData == null)
        {
            Debug.LogWarning($"EnemyStats.Start: enemyData not assigned on '{gameObject.name}'. Creating default instance.");
            //enemyData = ScriptableObject.CreateInstance<EnemySO>();
        }

        SetupEnemyStats(enemyData);

        if (healthBarPrefab != null)
        {
            healthBarGO = Instantiate(healthBarPrefab, transform);
            healthBarGO.transform.localPosition = healthBarLocalPos;
            healthBarGO.transform.localRotation = Quaternion.identity;

            healthFill = healthBarGO.GetComponentInChildren<Slider>();
        }

        // Armor bar: instantiate only if enemy actually has armor (maxArmor > 0)
        if (armorBarPrefab != null && maxArmor > 0f)
        {
            armorBarGO = Instantiate(armorBarPrefab, transform);
            armorBarGO.transform.localPosition = armorBarLocalPos;
            armorBarGO.transform.localRotation = Quaternion.identity;

            armorFill = armorBarGO.GetComponentInChildren<Slider>();
        }


        if (healthFill != null)
        {
            UpdateHealth(enemyData != null ? currentHealth : 0f, enemyData != null ? maxHealth : 0f);
        }

        if (armorFill != null)
        {
            UpdateArmor(currentArmor, maxArmor);
        }

        InvokeRepeating(nameof(RegenTick), regenInterval, regenInterval);
    }

    // ---------------------------------------------------------------------------------------------------------------------------------------
    // LOGISTIC FUNCTIONS
    // ---------------------------------------------------------------------------------------------------------------------------------------

    public void SetupEnemyStats(EnemySO enemySO)
    {
        //Enemy Resources
        maxHealth = enemySO.health;
        maxArmor = enemySO.armor;
        armorRegenRate = enemySO.armorRegenRate;
        armorRegenDelay = enemySO.armorRegenDelay;
        armorThreshold = enemySO.armorThreshold;
        armorBreakDelay = 5f;

        //Enemy NaveMesh Agent Settings
        baseMoveSpeed = enemySO.moveSpeed;
        movementAcceleration = enemySO.acceleration;
        stoppingDistance = enemySO.stoppingDistance;

        //Enemy AI Settings
        sightRange = enemySO.sightRange;
        attackRange = enemySO.attackRange;


        //Generic Offensive Stats
        fireDamageMultiplier = enemySO.fireDamageMultiplier;
        waterDamageMultiplier = enemySO.waterDamageMultiplier;
        airDamageMultiplier = enemySO.airDamageMultiplier;
        earthDamageMultiplier = enemySO.earthDamageMultiplier;
        arcaneDamageMultiplier = enemySO.arcaneDamageMultiplier;
        fireDamageFlat = enemySO.fireDamageFlat;
        waterDamageFlat = enemySO.waterDamageFlat;
        airDamageFlat = enemySO .airDamageFlat;
        earthDamageFlat = enemySO .earthDamageFlat;
        arcaneDamageFlat = enemySO.arcaneDamageFlat;
        ailmentMultiplier = enemySO.ailmentMultiplier;
        damageTakenMultiplier = enemySO.damageReceivedMultiplier;

        critChance = enemySO.critChance;
        critMultiplier = enemySO.critMultiplier;

        actionSpeed = enemySO.actionSpeed;
        cooldownReduction = enemySO.cooldownReduction;
        abilityDuration = enemySO.abilityDuration;

        //Specific Abillity Stats
        projectileIncrease = enemySO.projectileIncrease;
        projectilePenetration = enemySO.projectilePenetration;
        aoeIncrease = enemySO.aoeIncrease;

        //Weapon Base Stats
        weaponDamage = enemySO.weaponDamage;
        weaponActionSpeed = enemySO.weaponActionSpeed;

        //Player Rewards
        xpReward = enemySO.xpReward;
        goldReward = enemySO.goldReward;
        soulReward = enemySO.soulReward;

        //Setup Ailment Thresholds
        if (isBoss)
        {
            ailmentStackThreshold = ailmentStackThresholdBoss;
        }
        else
        {
            ailmentStackThreshold = ailmentStackThresholdMinion;
        }

        // ---------------------------
        // CHARACTER CURRENT STATS
        // ---------------------------
        currentHealth = maxHealth;
        currentArmor = maxArmor;

        SetAilmentStackValues();
        SetCurrentMoveSPeed();
        SetCurrentAbilitySpeed();
        SetAilmentDamageTakenChange();
        SetDamageTakenChange();

        // Ensure EnemyAI's NavMeshAgent is configured when enemy movement values change.
        // Subscribe to movement speed changes so the AI can react to ailment-driven slow/haste.
        OnCurrentMoveSpeedChange += HandleOnCurrentMoveSpeedChange;
        // Apply initial navmesh configuration now that stats are setup
        var ai = GetComponent<EnemyAI>(); // ai == ArtificialIntelligence == Enemy logic brain thingy
        if (ai != null)
        {
            ai.ApplyConfigNavMesh();
        }

    }

    // ---------------------------------------------------------------------------------------------------------------------------------------
    // ENEMY STATS & STATE FUNCTIONS
    // ---------------------------------------------------------------------------------------------------------------------------------------

    public override void TakeDamage(float amount)
    {
        lastDamageTime = Time.time;

        amount = amount * damageTakenMultiplier;

        float blocked = Mathf.Min(armorThreshold, amount);
        float armorDamage = Mathf.Min(currentArmor, blocked);

        float healthDamage = amount - armorDamage;

        if (armorDamage > 0)
        {
            isArmorRegenerating = false;
            currentArmor -= armorDamage;
            //OnArmorChanged?.Invoke(currentArmor, maxArmor); ----- CREATE ENEMY VERSION
        }

        if (healthDamage > 0)
        {
            currentHealth -= healthDamage;
            if (currentHealth <= 0) Die();
        }

        if (currentArmor <= 0)
        {
            //Stop armor regen when armor is depleted
            isArmorRegenerating = false;
            armorBroken = true;
        }


        Debug.Log($"Enemy took {amount} damage, current health: {currentHealth}");
        if (currentHealth <= 0)
        {
            Die();
        }

        CombatTextSpawner.Instance.ShowWorldText($"{amount}", new Color(1f, 0f, 0.6f), transform.position + Vector3.up * 1.8f);
        UpdateHealth(currentHealth, maxHealth);

    }

    void RegenTick()
    {

        //Check to start Armor Regen
        if (!isArmorRegenerating && !armorBroken && currentArmor < maxArmor && Time.time - lastDamageTime >= armorBreakDelay)
        {
            isArmorRegenerating = true;
            armorRegenTime = Time.time;
        }

        //Regen Armor 
        if (isArmorRegenerating && Time.time - armorRegenTime >= armorRegenDelay && currentArmor < maxArmor)
        {
            RegenerateArmor();
        }

        //Check if Armor is full and stop regen
        if (isArmorRegenerating && currentArmor >= maxArmor)
        {
            isArmorRegenerating = false;
        }

        //Cleanse Ailments
        if (Time.time - lastAilmentDamageTime >= cleanseTime)
        {
            CleanseAilments();
        }
    }

    // Lucas: Update health and armor bar
    public void UpdateHealth(float current, float max)
    {
        if (healthFill != null)
        {
            healthFill.maxValue = max;
            healthFill.value = current;
        }
    }

    public void UpdateArmor(float current, float max)
    {
        if (armorFill != null)
        {
            armorFill.maxValue = max;
            armorFill.value = current;
        }
    }
    //Lucas end

    private void RegenerateArmor()
    {
        float prevArmor = currentArmor;
        currentArmor = Mathf.Clamp(currentArmor + armorRegenRate, 0, maxArmor);

        armorRegenTime = Time.time;

        //if (currentArmor != prevArmor)
        //   OnArmorChanged?.Invoke(currentArmor, maxArmor); ----- CREATE ENEMY VERSION
    }

    public override void Die()
    {
        if (isDead) return;
        isDead = true;

        // Notify listeners first so they can react (e.g. spawner returns instance to pool)
        OnDeath?.Invoke();

        var player = FindFirstObjectByType<PlayerStats>();
        if (player != null)
        {
            player.GainXP(xpReward);
            player.GainGold(goldReward);
            player.GainSouls(soulReward);
        }

        Debug.Log("Enemy died");

        // Disable colliders so projectiles and other triggers stop interacting with this corpse
        Collider[] cols = GetComponentsInChildren<Collider>(true);
        foreach (var c in cols)
        {
            c.enabled = false;
        }

        // Disable NavMeshAgent to stop movement/navigation
        var agent = GetComponent<NavMeshAgent>();
        if (agent != null)
        {
            agent.enabled = false;
        }

        // Immediately disable any scripts so no further logic runs
        MonoBehaviour[] monos = GetComponentsInChildren<MonoBehaviour>(true);
        foreach (var m in monos)
        {
            if (m == this) continue; // keep this alive until end of method
            m.enabled = false;
        }

        if (poolingEnabled)
        {
            // Prepare instance for reuse. Do not Destroy — the spawner/pool is responsible for deactivating and storing.
            OnDespawned();
            return;
        }

        // No pooling: destroy the enemy GameObject when it dies.
        Destroy(gameObject);
    }

    // Lifecycle helper - previously used by pooling. Kept for compatibility.
    public void OnSpawned()
    {
        // Reset runtime state when taken from pool
        isDead = false;

        // Reset health and armor to max so instance is fresh
        currentHealth = maxHealth;
        currentArmor = maxArmor;

        // Re-enable scripts
        MonoBehaviour[] monos = GetComponentsInChildren<MonoBehaviour>(true);
        foreach (var m in monos)
        {
            m.enabled = true;
        }

        // Re-enable colliders
        Collider[] cols = GetComponentsInChildren<Collider>(true);
        foreach (var c in cols)
        {
            c.enabled = true;
        }
        // Reset health UI. Use conditional access for the inspector-assigned enemyData
        // because pooled instances may be spawned even if the original prefab had missing data.
        float displayedMaxHealth = enemyData != null ? enemyData.health : this.maxHealth;
        UpdateHealth(currentHealth, displayedMaxHealth);
    }

    public void OnDespawned()
    {
        // Cleanup when returned to pool (reset transient state, stop timers)
        // Stop any repeating invokes (regen, burn ticks)
        try { CancelInvoke(); } catch { }

        // Clear ailment stacks and temporary state
        for (int i = 0; i < ailmentDamageStockpile.Length; i++) ailmentDamageStockpile[i] = 0f;
        for (int i = 0; i < ailmentCurrentStacks.Length; i++) ailmentCurrentStacks[i] = 0;
        isBurnning = false;
        isFrozen = false;
        isStunned = false;
        lastAilmentDamageTime = 0f;

        // Clear any death subscribers to avoid duplicate handlers on reuse
        ClearOnDeathSubscribers();

        // Unsubscribe movement speed listener to avoid duplicate subscriptions on reuse
        OnCurrentMoveSpeedChange -= HandleOnCurrentMoveSpeedChange;
    }

    private void HandleOnCurrentMoveSpeedChange()
    {
        var ai = GetComponent<EnemyAI>();
        if (ai != null)
        {
            ai.ApplyConfigNavMesh();
        }
    }

    /// <summary>
    /// Clear all subscribers to the OnDeath event. Useful when returning an instance to a pool
    /// to avoid duplicate subscriptions on reuse.
    /// </summary>
    public void ClearOnDeathSubscribers()
    {
        OnDeath = null;
    }
}
