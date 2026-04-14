using System;
using System.Collections.Generic;
using System.IO;
using Unity.VisualScripting;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using static ClassListData;
using static DamageTagsData;
using static EquipmentTagsData;

[RequireComponent(typeof(PlayerController))]
public class PlayerStats : CharacterStats
{
    //UI & UX Player Related
    [SerializeField] private GameObject playerHUDPrefab;

    //Player Save Logistics
    public PlayerSaveSO playerSaveSlotSO;
    public List<bool> skillTree = new List<bool>();
    public Dictionary<int, Tuple<int, ItemSO>> inventoryItems = new Dictionary<int, Tuple<int, ItemSO>>();
    public Dictionary<int, EquipmentSO> armoryEquipment = new Dictionary<int, EquipmentSO>();
    public Dictionary<int, EquipmentSO> equippedSlotsEquipment = new Dictionary<int, EquipmentSO>();

    //Class
    [Header("Player Class")]
    [SerializeField] private ClassSO classData;



    //Save File Logistics
    private string saveDirectoryPath => Application.persistentDataPath;

    // ---------------------------
    // PLAYER SPECIFIC STATS
    // ---------------------------

    //Attributes
    [Header("Attributes")]
    public int strength { get; private set; } // per 1 STR = +10 MaxArmor & +5% Crit chance
    public int dexterity { get; private set; } // per 1 DEX = +2% Action Speed & +1% Move Speed
    public int intelligence { get; private set; } //per 1 INT = +10 Max Mana & +5% Cooldown Reduction
    public int vitality { get; private set; }   //per 1 VIT = +10 Max Health & +5% Armor Threshold

    //Weapon Types
    public List<WeaponTags> weaponTypesEquipped;

    //Ailments
    //IN ORDER: BURN MAX HP TICK / CHILL MAX HP PER STACK / SHOCK MAX HP LIGHTNING TRIGGER / DAZE MAX HP PER STACK / VOID MAX HP PER STACK
    public static float[] ailmentStackThresholdPlayer = new float[5] {0.01f, 0.03f, 0.01f, 0.025f, 0.02f };
    public static float cleanseTime = 4f;

    // ---------------------------
    // PLAYER SPECIFIC CURRENT STATS
    // ---------------------------

    //Player Core Stats
    public int currentXP { get; private set; }
    public int currentLevel { get; private set; } = 1;
    public int xpToNextLevel { get; private set; } = 100;
    public int skillPoints { get; private set; } = 0;
    public int gold { get; private set; } = 0;
    public int souls { get; private set; } = 0;

    //Stats Change Events
    public event Action<float, float> OnHealthChanged;
    public event Action<float, float> OnManaChanged;
    public event Action<float, float> OnArmorChanged;
    public event Action OnStatsChanged;
    public event Action<int, int> OnXPChanged;
    public event Action<int> OnLevelUp;
    public event Action OnDeath;

    [SerializeField] private float regenInterval = 1f;
    private bool armorBroken = false;
    private float lastDamageTime;
    private float armorRegenTime;

    //LEVEL TABLE
    private Dictionary<int, int> levelTable = new Dictionary<int, int>
    {
        { 1 , 200},{ 2 , 400},{ 3 , 600},{ 4 , 800},{ 5 , 1000},{ 6 , 1200},{ 7 , 1400},{ 8 , 1600},{ 9 , 1800},{ 10 , 2000}, // Level * 200
        { 11 , 2500},{ 12 , 3000},{ 13 , 3500},{ 14 , 4000},{ 15 , 4500},{ 16 , 5000},{ 17 , 5500},{ 18 , 6000},{ 19 , 6500},{ 20 , 7000}, //Level(10) + Level * 500
        { 21 , 8000},{ 22 , 9000},{ 23 , 10000},{ 24 ,11000},{ 25 , 12000},{ 26 , 13000},{ 27 , 14000},{ 28 , 15000},{ 29 , 16000},{ 30 , 17000}, //Level(20) + Level * 1000
        { 31 , 19000},{ 32 , 21000},{ 33 , 23000},{ 34 , 25000},{ 35 , 27000},{ 36 , 29000},{ 37 , 31000},{ 38 , 33000},{ 39 , 35000},{ 40 , 37000}, //Level(30) + Level * 2000
        { 41 , 41000},{ 42 , 45000},{ 43 , 49000},{ 44 , 53000},{ 45 , 57000},{ 46 , 61000},{ 47 , 65000},{ 48 , 69000},{ 49 , 73000},{ 50 , 0}, //Level(40) + Level * 4000
    };
    // ---------------------------------------------------------------------------------------------------------------------------------------
    // ON RUNNING FUNCTIONS
    // ---------------------------------------------------------------------------------------------------------------------------------------
    private void OnEnable()
    {
        OnStatsChanged += SetAilmentStackValues;
        OnStatsChanged += SetCurrentMoveSPeed;
        OnStatsChanged += SetCurrentAbilitySpeed;
    }
    private void OnDisable()
    {
        OnStatsChanged -= SetAilmentStackValues;
        OnStatsChanged -= SetCurrentMoveSPeed;
        OnStatsChanged -= SetCurrentAbilitySpeed;
    }

    public void Start()
    {
        //Load Save
        LoadPlayerDataFromSaveSO();

        xpToNextLevel = levelTable[currentLevel];

        SetupClassInPlayer();

        //Setup Current Stats for class
        currentHealth = maxHealth;
        currentMana = maxMana;
        currentArmor = maxArmor;

        //Setup Ailment Thresholds
        ailmentStackThreshold = ailmentStackThresholdPlayer;

        //Event Changes & Calculations
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
        OnManaChanged?.Invoke(currentMana, maxMana);
        OnArmorChanged?.Invoke(currentArmor, maxArmor);
        OnXPChanged?.Invoke(currentXP, xpToNextLevel);
        OnStatsChanged?.Invoke();
        SetDamageTakenChange();
        SetAilmentDamageTakenChange();

        InvokeRepeating(nameof(RegenTick), regenInterval, regenInterval);

        if (playerHUDPrefab != null)
        {
            GameObject hud = Instantiate(playerHUDPrefab);
            PlayerHUD hudScript = hud.GetComponent<PlayerHUD>();
            if (hudScript != null)
                hudScript.stats = this;
        }
        else
        {
            Debug.LogError("PlayerStats: esquecido de arrastar o playerHUDPrefab no Inspector!");
        }

        SetupPlayerSkillTree();
        SetupPlayerItems();
        SetupPlayerEquiment();
    }

    // ---------------------------------------------------------------------------------------------------------------------------------------
    // SAVE/LOAD & LOGISTIC FUNCTIONS
    // ---------------------------------------------------------------------------------------------------------------------------------------
    public void SavePLayerDataToSaveSO()
    {
        string saveFilePath = saveDirectoryPath + "/" + playerSaveSlotSO.saveFileName;


        //Base Player Stats
        playerSaveSlotSO.playerClassIndex = ClassListData.ClassEnumToInt(classData.playerClass);
        playerSaveSlotSO.level = currentLevel;
        playerSaveSlotSO.xp = currentXP;
        playerSaveSlotSO.souls = souls;
        playerSaveSlotSO.gold = gold;

        PlayerHUD playerHUD = FindFirstObjectByType<PlayerHUD>();

        //Ability & Skill Tree
        List<bool> skillTreeSave = new List<bool>();
        foreach(SkillTreeSlot skillTreeSlot in playerHUD.GetComponent<SkillTreeManager>().treeSlots)
        {
            skillTreeSave.Add(skillTreeSlot.unlocked);
        }
        playerSaveSlotSO.skillTree.list = skillTreeSave;




        //Equipment & Inventory

        int inventorySlotsIndex = 0;
        playerSaveSlotSO.inventoryItems.list = new List<PlayerItemSave>();
        foreach (var inventorySlot in playerHUD.GetComponent<InventoryManager>().inventorySlots)
        {
            if (inventorySlot.transform.childCount > 0)
            {
                InventoryItem inventoryItem = inventorySlot.transform.GetChild(0).gameObject.GetComponent<InventoryItem>();
                ItemSO itemSO = inventoryItem.item;
                int itemCount = inventoryItem.count;
                playerSaveSlotSO.inventoryItems.list.Add(playerSaveSlotSO.SaveItem(inventorySlotsIndex, itemCount, itemSO));
            }
            inventorySlotsIndex++;
        }

        int armoryIndex = 0;
        playerSaveSlotSO.armoryEquipment.list = new List<PlayerItemSave>();
        foreach (var armorySlot in playerHUD.GetComponent<ArmoryManager>().armorySlots)
        {
            if (armorySlot.transform.childCount > 0)
            {
                EquipmentItem equipmentItem = armorySlot.transform.GetChild(0).gameObject.GetComponent<EquipmentItem>();
                EquipmentSO equipmentSO = equipmentItem.equipment;
                playerSaveSlotSO.armoryEquipment.list.Add(playerSaveSlotSO.SaveEquipment(armoryIndex, equipmentSO));
            }
            armoryIndex++;
        }

        int equippedSlotsIndex = 0;
        playerSaveSlotSO.equippedSlotsEquipment.list = new List<PlayerItemSave>();
        foreach (var equippedSlot in playerHUD.GetComponent<PlayerEquipController>().equipSlots)
        {

            if (equippedSlot.transform.childCount > 0)
            {
                EquipmentItem equipmentItem = equippedSlot.transform.GetChild(0).gameObject.GetComponent<EquipmentItem>();
                EquipmentSO equipmentSO = equipmentItem.equipment;
                playerSaveSlotSO.equippedSlotsEquipment.list.Add(playerSaveSlotSO.SaveEquipment(equippedSlotsIndex, equipmentSO));
            }
            equippedSlotsIndex++;
        }

        string PlayerSaveString = JsonUtility.ToJson(playerSaveSlotSO, true);
        File.WriteAllText(saveFilePath, PlayerSaveString);
        Debug.Log("Saved to: " + saveFilePath);
    }

    public void LoadPlayerDataFromSaveSO()
{
        currentLevel = playerSaveSlotSO.level;
        currentXP = playerSaveSlotSO.xp;
        souls = playerSaveSlotSO.souls;
        gold = playerSaveSlotSO.gold;

        //Ability & Skill Tree
        skillTree = playerSaveSlotSO.skillTree.list;

        //Equipment & Inventory
        foreach (var inventorySlotSave in playerSaveSlotSO.inventoryItems.list)
        {
            KeyValuePair<int, Tuple<int,ItemSO>> inventorySlot = PlayerSaveSO.LoadItem(inventorySlotSave);
            inventoryItems.Add(inventorySlot.Key, inventorySlot.Value);
        }

        foreach (var armorySlotSave in playerSaveSlotSO.armoryEquipment.list)
        {
            KeyValuePair<int, EquipmentSO> armorySlot = PlayerSaveSO.LoadEquiment(armorySlotSave);

            armoryEquipment.Add(armorySlot.Key, armorySlot.Value);
        }

        foreach (var equippedSlotSave in playerSaveSlotSO.equippedSlotsEquipment.list)
        {
            KeyValuePair<int, EquipmentSO> equippedSlot = PlayerSaveSO.LoadEquiment(equippedSlotSave);

            equippedSlotsEquipment.Add(equippedSlot.Key, equippedSlot.Value);
        }
    }

    // ---------------------------------------------------------------------------------------------------------------------------------------
    // PLAYER STATS & STATE FUNCTIONS
    // ---------------------------------------------------------------------------------------------------------------------------------------

    public override void Die()
    {
        OnDeath?.Invoke();
        gameObject.SetActive(false);
        Debug.Log("Player has died");
    }

    public override void TakeDamage(float amount)
    {
        // If the player is currently dashing, treat them as temporarily immune to damage.
        if (isDashing) return;

        amount = amount * damageTakenMultiplier;

        float blocked = Mathf.Min(armorThreshold, amount);
        float armorDamage = Mathf.Min(currentArmor, blocked);

        float healthDamage = amount - armorDamage;

        if (armorDamage > 0)
        {
            currentArmor -= armorDamage;
            OnArmorChanged?.Invoke(currentArmor, maxArmor);
        }

        if (healthDamage > 0)
        {
            ChangeHealth(-healthDamage);
            if (currentHealth <= 0) Die();
        }

        if (currentArmor <= 0)
        {
            //Stop armor regen when armor is depleted
            isArmorRegenerating = false;
            armorBroken = true;

            lastDamageTime = Time.time;
        }
        CombatTextSpawner.Instance.ShowWorldText(
            $"−{amount}", Color.red, transform.position + Vector3.up * 1.8f);
    }

    void RegenTick()
    {
        ChangeMana(Mathf.FloorToInt(manaRegen));

        //Armor Broken Status Clear
        if (armorBroken && Time.time - lastDamageTime >= armorBreakDelay)
        {
            armorBroken = false;
        }

        //Check to start Armor Regen
        if (!isArmorRegenerating && !armorBroken && currentArmor < maxArmor)
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

    // ---------------------------
    // Class, Level & Skill tree logic
    // ---------------------------

    public void SetClassSO(ClassSO classSO)
    {
        classData = classSO;
    }

    public UserClasses GetClass()
    {
        return classData.playerClass;
    }

    public void SetupClassInPlayer()
    {
        //Setup Base Class Stats
        strength = classData.strength;
        dexterity = classData.dexterity;
        intelligence = classData.intelligence;
        vitality = classData.vitality;
        maxHealth = classData.maxHealth;
        maxMana = classData.maxMana;
        armorThreshold = classData.armorThreshold;
        manaRegen = classData.manaRegen;
        baseMoveSpeed = classData.baseMoveSpeed;

        //Setup Level UP Class Stats
        strength += classData.levelUPStr * (currentLevel - 1);
        dexterity += classData.levelUPDex * (currentLevel - 1);
        intelligence += classData.levelUPInt * (currentLevel - 1);
        vitality += classData.levelUPVit * (currentLevel - 1);
        maxHealth += classData.levelUPMaxHP * (currentLevel - 1);
        maxMana += classData.levelUPMaxMana * (currentLevel - 1);
        manaRegen += classData.levelUPManaRegen * (currentLevel - 1);

        //Skill points
        skillPoints = currentLevel - 1;

        AddAttributesEffects(strength, dexterity, intelligence, vitality);
    }

    public void GainXP(int amount)
    {
        currentXP += amount;
        CombatTextSpawner.Instance.ShowWorldText(
            $"+{amount} XP", Color.yellow, transform.position + Vector3.up * 2.2f);
        while (currentXP >= xpToNextLevel)
        {
            currentXP -= xpToNextLevel;
            LevelUp();
        }
        OnXPChanged?.Invoke(currentXP, xpToNextLevel);
    }
    private void LevelUp()
    {
        currentLevel++;
        xpToNextLevel = levelTable[currentLevel];

        //Add Class Stats on Level UP
        strength += classData.levelUPStr;
        dexterity += classData.levelUPDex;
        intelligence += classData.levelUPInt;
        vitality += classData.levelUPVit;
        maxHealth += classData.levelUPMaxHP;
        maxMana += classData.levelUPMaxMana;
        armorThreshold += classData.levelUPArmorThreshold;
        manaRegen += classData.levelUPManaRegen;

        AddAttributesEffects(classData.levelUPStr, classData.levelUPDex, classData.levelUPInt, classData.levelUPVit);

        //Change current Stats
        currentArmor = maxArmor;
        currentHealth = maxHealth;
        currentMana = maxMana;

        //Skill Points
        skillPoints += 1;

        OnArmorChanged?.Invoke(currentArmor, maxArmor);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
        OnManaChanged?.Invoke(currentMana, maxMana);
        OnLevelUp?.Invoke(currentLevel);
        OnStatsChanged?.Invoke();

        // SavePLayerDataToSaveSO();
    }

    public void GainGold(int amount)
    {
        gold += amount;
        CombatTextSpawner.Instance.ShowWorldText(
            $"+{amount} Gold", new Color(1f, 0.843f, 0f), transform.position + Vector3.up * 1.5f);
    }

    public void SpendGold(int amount)
    {
        gold -= amount;
    }

    public void GainSouls(int amount)
    {
        souls += amount;
        CombatTextSpawner.Instance.ShowWorldText(
            $"+{amount} Souls", Color.cyan, transform.position + Vector3.up * 1f);
    }

    public void SpendSouls(int amount)
    {
        souls -= amount;
    }
    public bool SpendSkillPoints()
    {
        if (skillPoints > 0)
        {
            skillPoints--;
            return true;
        }
        return false;
    }

    public void SetupPlayerSkillTree()
    {
        PlayerHUD playerHUD = FindFirstObjectByType<PlayerHUD>();

        if (playerHUD == null)
        {
            Debug.LogError("PlayerStats.SetupPlayerItems: Could not find PlayerHUD.");
        }
        else
        {
            SkillTreeManager skillTreeManager = playerHUD.GetComponent<SkillTreeManager>();
            if (skillTree == null) return;
            for (int i = 0; i < skillTree.Count; i++)
            {
                skillTreeManager.treeSlots[i].unlocked = skillTree[i];
            }
        }
    }

    // ---------------------------
    // Health, Mana & Armor Logic
    // ---------------------------
    private void ChangeHealth(float delta)
    {
        currentHealth = Mathf.Clamp(currentHealth + delta, 0, maxHealth);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }
    private void ChangeMana(float delta)
    {
        currentMana = Mathf.Clamp(currentMana + delta, 0, maxMana);
        OnManaChanged?.Invoke(currentMana, maxMana);
    }

    private void ChangeArmor(float delta)
    {
        currentArmor = Mathf.Clamp(currentArmor + delta, 0, maxArmor);
        OnArmorChanged?.Invoke(currentArmor, maxArmor);
    }

    private void RegenerateArmor()
    {
        float prevArmor = currentArmor;
        currentArmor = Mathf.Clamp(currentArmor + armorRegenRate, 0, maxArmor);

        armorRegenTime = Time.time;

        if (currentArmor != prevArmor)
            OnArmorChanged?.Invoke(currentArmor, maxArmor);
    }

    public bool UseMana(float amount)
    {
        if (currentMana < amount) return false;
        ChangeMana(-amount);
        return true;
    }
    // ---------------------------
    // Attributes logic
    // ---------------------------
    public void AddAttributesEffects(int addedStr, int addedDex, int addedInt, int addedVit)
    {
        //strength  // per 1 STR = +10 MaxArmor & +5% Crit chance
        //dexterity // per 1 DEX = +2% Action Speed & +1% Move Speed
        //intelligence  //per 1 INT = +10 Max Mana & +5% Cooldown Reduction
        //vitality   //per 1 VIT = +10 Max Health & +5% Armor Threshold Multiplier

        //Strength
        maxArmor += addedStr * 10f;
        critChance += addedStr * 0.05f;

        //Dexterity
        actionSpeed += addedDex * 0.02f;
        moveSpeedMultiplier += addedDex * 0.01f;

        //Inteligence
        maxMana += addedInt * 10f;
        cooldownReduction += addedInt * 0.05f;

        //Vitality
        maxHealth += addedVit * 10f;
        armorThresholdMultiplier += addedVit * 0.05f;
    }

    public void RemoveAttributesEffects(int removedStr, int removedDex, int removedInt, int removedVit)
    {
        //Strength
        maxArmor -= removedStr * 10f;
        critChance -= removedStr * 0.05f;

        //Dexterity
        actionSpeed -= removedDex * 0.02f;
        moveSpeedMultiplier -= removedDex * 0.01f;

        //Inteligence
        maxMana -= removedInt * 10f;
        cooldownReduction -= removedInt * 0.05f;

        //Vitality
        maxHealth -= removedVit * 10f;
        armorThresholdMultiplier -= removedVit * 0.05f;
    }


    // ---------------------------
    // Consumables logic
    // ---------------------------

    public void SetupPlayerItems()
    {
        PlayerHUD playerHUD = FindFirstObjectByType<PlayerHUD>();

        if (playerHUD == null)
        {
            Debug.LogError("PlayerStats.SetupPlayerItems: Could not find PlayerHUD.");
        }
        else
        {
            InventoryManager inventoryManager = playerHUD.GetComponent<InventoryManager>();
            if (inventoryItems == null) return;
            foreach (var item in inventoryItems)
            {
                inventoryManager.AddItem(item.Value.Item2, item.Value.Item1);
            }
        }

    }


    // Health/Mana pots
    public void Heal(float amount)
    {
        ChangeHealth(amount);
        CombatTextSpawner.Instance.ShowWorldText(
            $"+{amount} Health", Color.green, transform.position + Vector3.up * 2.2f);
    }

    public void EnergyDrink(float amount)
    {
        ChangeMana(amount);
        CombatTextSpawner.Instance.ShowWorldText(
            $"+{amount} Mana", Color.blue, transform.position + Vector3.up * 2.2f);
    }

    public void ShieldForce(float amount)
    {
        ChangeArmor(amount);
        CombatTextSpawner.Instance.ShowWorldText(
            $"+{amount} Armor", Color.gray, transform.position + Vector3.up * 2.2f);
    }

    // ---------------------------
    // Equipment Bonuses logic
    // ---------------------------
    public void SetupPlayerEquiment()
    {
        PlayerHUD playerHUD = FindFirstObjectByType<PlayerHUD>();

        //Armory Setup
        if (playerHUD == null)
        {
            Debug.LogError("PlayerStats.SetupPlayerItems: Could not find PlayerHUD.");
        }
        else
        {
            ArmoryManager armoryManager = playerHUD.GetComponent<ArmoryManager>();
            if (armoryEquipment == null) return;
            foreach (var armoryEquipment in armoryEquipment)
            {
                armoryManager.SpawnEquipmentItemInSlot(armoryEquipment.Value, armoryManager.armorySlots[armoryEquipment.Key]);
                armoryManager.currentBySlot[armoryEquipment.Key] = armoryEquipment.Value;
            }
        }

        //Player Equipment Setup
        if (playerHUD == null)
        {
            Debug.LogError("PlayerStats.SetupPlayerItems: Could not find PlayerHUD.");
        }
        else
        {
            PlayerEquipController playerEquipController = playerHUD.GetComponent<PlayerEquipController>();
            if (equippedSlotsEquipment == null) return;
            foreach (var equippedEquipment in equippedSlotsEquipment)
            {
                playerEquipController.SpawnEquipmentItemInSlot(equippedEquipment.Value, playerEquipController.equipSlots[equippedEquipment.Key]);
                playerEquipController.currentBySlot[equippedEquipment.Key] = equippedEquipment.Value;
            }
        }
    }

    public void AddEquipmentBonus(EquipmentSO eq)
    {
        //if (eq == null || equippedBonuses.Find(eq)) return;
        if (eq == null) return;

        ////Stores old values for health, mana, armor
        float oldMaxHealth = maxHealth;
        float oldMaxMana = maxMana;
        float oldMaxArmor = maxArmor;


        // APPLY BONUS
        //Attributes
        strength += eq.strength;  // per 1 STR = +10 MaxArmor & +10% Crit chance
        dexterity += eq.dexterity;  // per 1 DEX = +2% Action Speed & +1% Move Speed
        intelligence += eq.intelligence;  //per 1 INT = +10 Max Mana & +5% Cooldown Reduction
        vitality += eq.vitality;  //per 1 VIT = +10 Max Health & +5% Armor Threshold

        AddAttributesEffects(eq.strength, eq.dexterity, eq.intelligence, eq.vitality);

        //Resources Stats
        maxHealth += eq.maxHealth;
        maxMana += eq.maxMana;
        manaRegen += eq.manaRegen;
        maxArmor += eq.maxArmor;
        armorRegenRate += eq.armorRegenRate;
        armorRegenDelay += eq.armorRegenDelay;
        armorThreshold += eq.armorThreshold;

        //Offensive Stats
        critChance += eq.critChance;
        critMultiplier += eq.critMultiplier;
        fireDamageMultiplier += eq.fireDamageMultiplier;
        waterDamageMultiplier += eq.waterDamageMultiplier;
        airDamageMultiplier += eq.airDamageMultiplier;
        earthDamageMultiplier += eq.earthDamageMultiplier;
        arcaneDamageMultiplier += eq.arcaneDamageMultiplier;
        fireDamageFlat += eq.fireDamageFlat;
        waterDamageFlat += eq.waterDamageFlat;
        airDamageFlat += eq.airDamageFlat;
        earthDamageFlat += eq.earthDamageFlat;
        arcaneDamageFlat += eq.arcaneDamageFlat;
        ailmentMultiplier += eq.ailmentMultiplier;
        actionSpeed += eq.actionSpeed;
        cooldownReduction += eq.cooldownReduction;
        abilityDuration += eq.abilityDuration;

        //Specific Abillity Stats
        projectileIncrease += eq.projectileIncrease;
        projectilePenetration += eq.projectilePenetration;
        aoeIncrease += eq.aoeIncrease;

        //Check for Equipment Specific Stats
        if (eq is WeaponSO)
        {
            switch((eq as WeaponSO).damageType)
            {
                case DamageTags.Fire:
                    weaponDamage[0] += (eq as WeaponSO).weaponDamage;
                    break;
                case DamageTags.Water:
                    weaponDamage[1] += (eq as WeaponSO).weaponDamage;
                    break;
                case DamageTags.Air:
                    weaponDamage[2] += (eq as WeaponSO).weaponDamage;
                    break;
                case DamageTags.Earth:
                    weaponDamage[3] += (eq as WeaponSO).weaponDamage;
                    break;
                case DamageTags.Arcane:
                    weaponDamage[4] += (eq as WeaponSO).weaponDamage;
                    break;
            }

            //ADD Weapon ActionSpeed & Check for dualwielding logic
            if (weaponActionSpeed > 0)
            {
                weaponActionSpeed = (weaponActionSpeed + (eq as WeaponSO).actionSpeed)/2;
            }
            else
            {
                weaponActionSpeed = (eq as WeaponSO).actionSpeed;
            }

            //ADD Weapon Type equipped
            weaponTypesEquipped.Add((eq as WeaponSO).weaponType);
        }

        if (eq is ArmorSO)
        {
            if (eq.equipSlot == EquipSlotType.Chest)
            {
                switch ((eq as ArmorSO).armorType)
                {
                    case ArmorTags.Light:
                        armorRegenDelay = EquipmentAffixsData.lightStats[0];
                        armorBreakDelay = EquipmentAffixsData.lightStats[1];
                        baseMoveSpeed = EquipmentAffixsData.lightStats[2];
                        break;
                    case ArmorTags.Medium:
                        armorRegenDelay = EquipmentAffixsData.mediumStats[0];
                        armorBreakDelay = EquipmentAffixsData.mediumStats[1];
                        baseMoveSpeed = EquipmentAffixsData.mediumStats[2];
                        break;
                    case ArmorTags.Heavy:
                        armorRegenDelay = EquipmentAffixsData.heavyStats[0];
                        armorBreakDelay = EquipmentAffixsData.heavyStats[1];
                        baseMoveSpeed = EquipmentAffixsData.heavyStats[2];
                        break;
                }
            }
        }

        // CURRENT VALUES
        //Current values: keeps current values, but clamps to new max
        currentHealth = Mathf.Min(currentHealth, maxHealth);
        currentMana = Mathf.Min(currentMana, maxMana);
        currentArmor = Mathf.Min(currentArmor, maxArmor);

        OnHealthChanged?.Invoke(currentHealth, maxHealth);
        OnManaChanged?.Invoke(currentMana, maxMana);
        OnArmorChanged?.Invoke(currentArmor, maxArmor);
        OnStatsChanged?.Invoke();
    }

    public void RemoveEquipmentBonus(EquipmentSO eq)
    {
        if (eq == null) return;

        // REMOVE BONUS
        //Attributes
        strength -= eq.strength;  // per 1 STR = +10 MaxArmor & +5% Crit chance
        dexterity -= eq.dexterity;  // per 1 DEX = +2% Action Speed & +1% Move Speed
        intelligence -= eq.intelligence;  //per 1 INT = +10 Max Mana & +5% Cooldown Reduction
        vitality -= eq.vitality;  //per 1 VIT = +10 Max Health & +5% Armor Threshold

        AddAttributesEffects(eq.strength, eq.dexterity, eq.intelligence, eq.vitality);

        //Resources Stats
        maxHealth -= eq.maxHealth;
        maxMana -= eq.maxMana;
        manaRegen -= eq.manaRegen;
        maxArmor -= eq.maxArmor;
        armorRegenRate -= eq.armorRegenRate;
        armorRegenDelay -= eq.armorRegenDelay;
        armorThreshold -= eq.armorThreshold;

        //Offensive Stats
        critChance -= eq.critChance;
        critMultiplier -= eq.critMultiplier;
        fireDamageMultiplier -= eq.fireDamageMultiplier;
        waterDamageMultiplier -= eq.waterDamageMultiplier;
        airDamageMultiplier -= eq.airDamageMultiplier;
        earthDamageMultiplier -= eq.earthDamageMultiplier;
        arcaneDamageMultiplier -= eq.arcaneDamageMultiplier;
        fireDamageFlat -= eq.fireDamageFlat;
        waterDamageFlat -= eq.waterDamageFlat;
        airDamageFlat -= eq.airDamageFlat;
        earthDamageFlat -= eq.earthDamageFlat;
        arcaneDamageFlat -= eq.arcaneDamageFlat;
        ailmentMultiplier -= eq.ailmentMultiplier;
        actionSpeed -= eq.actionSpeed;
        cooldownReduction -= eq.cooldownReduction;
        abilityDuration -= eq.abilityDuration;

        //Specific Abillity Stats
        projectileIncrease -= eq.projectileIncrease;
        projectilePenetration -= eq.projectilePenetration;
        aoeIncrease -= eq.aoeIncrease;


        //Check for Equipment Specific Stats
        if (eq is WeaponSO)
        {
            if (eq is WeaponSO)
            {
                switch ((eq as WeaponSO).damageType)
                {
                    case DamageTags.Fire:
                        weaponDamage[0] -= (eq as WeaponSO).weaponDamage;
                        break;
                    case DamageTags.Water:
                        weaponDamage[1] -= (eq as WeaponSO).weaponDamage;
                        break;
                    case DamageTags.Air:
                        weaponDamage[2] -= (eq as WeaponSO).weaponDamage;
                        break;
                    case DamageTags.Earth:
                        weaponDamage[3] -= (eq as WeaponSO).weaponDamage;
                        break;
                    case DamageTags.Arcane:
                        weaponDamage[4] -= (eq as WeaponSO).weaponDamage;
                        break;
                }

                //REMOVE Weapon ActionSpeed & Check for dualwielding logic
                if (weaponActionSpeed != (eq as WeaponSO).actionSpeed)
                {
                    weaponActionSpeed = (weaponActionSpeed * 2) - (eq as WeaponSO).actionSpeed;
                }
                else
                {
                    weaponActionSpeed -= (eq as WeaponSO).actionSpeed;

                    if(weaponActionSpeed != 0)
                    {
                        Debug.LogError("Error in Action Speed Calculations!");
                    }
                }

                //REMOVE Weapon Type equipped
                weaponTypesEquipped.Remove((eq as WeaponSO).weaponType);

            }
        }

        if (eq is ArmorSO)
        {
            if (eq.equipSlot == EquipSlotType.Chest)
            {
                armorRegenDelay = EquipmentAffixsData.noArmorStats[0];
                armorBreakDelay = EquipmentAffixsData.noArmorStats[1];
                baseMoveSpeed = EquipmentAffixsData.noArmorStats[2];
            }
        }

        // CURRENT VALUES
        // current values: keeps current values, but clamps to new max
        currentHealth = Mathf.Min(currentHealth, maxHealth);
        currentMana = Mathf.Min(currentMana, maxMana);
        currentArmor = Mathf.Min(currentArmor, maxArmor);

        OnHealthChanged?.Invoke(currentHealth, maxHealth);
        OnManaChanged?.Invoke(currentMana, maxMana);
        OnArmorChanged?.Invoke(currentArmor, maxArmor);
        OnStatsChanged?.Invoke();
    }
}