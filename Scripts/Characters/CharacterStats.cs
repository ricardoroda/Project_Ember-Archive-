using System;
using System.Collections;
using UnityEngine;

public abstract class CharacterStats : MonoBehaviour
{
    // ---------------------------
    // CHARACTER STATS
    // ---------------------------
    // Resources Stats (Health, Mana & Armor)
    [Header("Resources Stats")]
    public float maxHealth { get; protected set; }
    public float manaRegen { get; protected set; }
    public float maxMana { get; protected set; }
    public float maxArmor { get; protected set; }
    public float armorRegenRate { get; protected set; }
    public float armorRegenRateMultiplier { get; protected set; }
    [Tooltip("Time between armor regen ticks")]
    public float armorRegenDelay { get; protected set; } = 2f;
    [Tooltip("If damage > this value the excess damage is dealt to Health")]
    public float armorThreshold { get; protected set; }
    public float armorThresholdMultiplier { get; protected set; }
    [Tooltip("When armor breaks, takes this time without taking damage to start regening armor")]
    public float armorBreakDelay { get; protected set; } = 5f;
    public float baseMoveSpeed { get; protected set; } = 5f;
    public float moveSpeedMultiplier { get; protected set; }

    //Generic Offensive Stats
    [Header("Generic Offensive Stats")]
    public float critChance { get; protected set; }
    public float critMultiplier { get; protected set; }
    public float fireDamageMultiplier { get; protected set; }
    public float waterDamageMultiplier { get; protected set; }
    public float airDamageMultiplier { get; protected set; }
    public float earthDamageMultiplier { get; protected set; }
    public float arcaneDamageMultiplier { get; protected set; }
    public int fireDamageFlat { get; protected set; }
    public int waterDamageFlat { get; protected set; }
    public int airDamageFlat { get; protected set; }
    public int earthDamageFlat { get; protected set; }
    public int arcaneDamageFlat { get; protected set; }
    public float ailmentMultiplier { get; protected set; }
    public float actionSpeed { get; protected set; }
    public float cooldownReduction { get; protected set; }
    public float abilityDuration { get; protected set; }

    //Specific Abillity Stats
    [Header("Specific Abillity Stats")]
    public int projectileIncrease { get; protected set; }
    public int projectilePenetration { get; protected set; }
    public float aoeIncrease { get; protected set; }

    //Weapon Base Stats
    public int[] weaponDamage { get; set; } = new int[5] { 0, 0, 0, 0, 0 };
    public float weaponActionSpeed { get; set; } = 0;

    // ---------------------------
    // CHARACTER CURRENT STATS
    // ---------------------------
    public float currentHealth { get; protected set; }
    public float currentMana { get; protected set; }
    public float currentArmor { get; protected set; }
    public float currentActionSpeed { get; protected set; }
    public float currentCooldownReduction { get; protected set; }
    public float currentWeaponActionSpeed { get; protected set; }
    public float currentMovementSpeed { get; protected set; }
    public float ailmentTakenMultiplier { get; protected set; } = 1;
    public float damageTakenMultiplier { get; protected set; } = 1;


    //AILMENTS
    //IN ORDER: FIRE-BURN-EXPLODE / WATER-CHILL-FROZEN / AIR-SHOCK-LIGHTNING / EARTH-DAZE-STUN / ARCANE-VOID
    //FIRE-BURN-EXPLODE: Ticks fire damage over time acumulated in the enemy. (Needs ability to trigger explosion)
    //WATER-CHILL-FROZEN: Slows enemy movement per stack. Reaching max stacks becomes Frozen.
    //AIR-SHOCK-LIGHTNING: Increases ailment acumulation per stack. Reaching max stacks for a Lightning Bolt.
    //EARTH-DAZE-STUN: Slows enemy action/cooldown per stack. Reaching max stacks becomes Stunned.
    //ARCANE-VOID: Increases damage taken per stack.
    public float[] ailmentDamageStockpile = new float[5] { 0, 0, 0, 0, 0 }; //Ailment Damage Acumulated
    public float[] ailmentStackThreshold = new float[5] { 0, 0, 0, 0, 0 };   //MAX HP % that each ailment stack represents (Static %) (In the case of Burn is the Max Health % Burn tick damage)
    public float[] ailmentPerStackValue = new float[5] { 0, 0, 0, 0, 0 };   //MAX HP Value per Stack
    public int[] ailmentCurrentStacks = new int[5] { 0, 0, 0, 0, 0 };   //Current number of Stacks (Burn doesnt manage stacks)
    public float burnTickRate = 0.2f;   //Tick Rate that burn Damage is dealt
    public float chillMoveSpeedReductionPerStack = 0.05f;
    public float shockAilmentIncreasePerStack = 0.03f;
    public float dazeActionSpeedReductionPerStack = 0.05f;
    public float voidDamageIncreasePerStack = 0.03f;
    public float frozenDuration = 3f;
    public float frozenDamageIncrease = 0.25f;
    public float stunDuration = 5f;
    protected float lastAilmentDamageTime;

    // ---------------------------
    // CHARACTER CURRENT STATES
    // ---------------------------
    public bool isDashing = false;
    protected bool isArmorRegenerating = false;
    // Whether the character is dead (set by Die()). Useful to avoid further damage interactions.
    public bool isDead { get; protected set; } = false;

    //AILMENTS STATES
    public bool isBurnning = false;
    public bool isFrozen = false;
    public bool isStunned = false;

    // ---------------------------
    // CHARACTER EVENTS
    // ---------------------------
    public event Action OnCurrentMoveSpeedChange;
    public event Action OnAbilitySpeedChange;
    public event Action OnDamageTakenChange;
    public event Action OnAilmentDamageTakenChange;
    public event Action OnCleanseAilments;
    public event Action OnFrozen;
    public event Action OnStunned;

    // ---------------------------
    // CHARACTER FUNCTIONS
    // ---------------------------
    public abstract void TakeDamage(float damage);
    public abstract void Die();
    public void TakeAilmentDamage(float[] ailmentDamage)
    {
        foreach(float ailmentDamageType in ailmentDamage)
        {
            if(ailmentDamageType > 0)
            {
                lastAilmentDamageTime = Time.time;
            }
        }

        ailmentDamageStockpile[0] += ailmentDamage[0] * ailmentTakenMultiplier;

        if (ailmentDamageStockpile[0] > 0 && !isBurnning)
        {
            isBurnning = true;
            InvokeRepeating(nameof(BurnEffect), 0, burnTickRate);
        }

        if (!isFrozen)
        {
            ailmentDamageStockpile[1] += ailmentDamage[1] * ailmentTakenMultiplier;
        }

        ailmentDamageStockpile[2] += ailmentDamage[2] * ailmentTakenMultiplier;

        if (!isStunned)
        {
            ailmentDamageStockpile[3] += ailmentDamage[3] * ailmentTakenMultiplier;
        }

        ailmentDamageStockpile[4] += ailmentDamage[4] * ailmentTakenMultiplier;

        //Water-Chill
        ChillEffect();
        //Air-Shock
        ShockEffect();
        //Earth-Daze
        DazeEffect();
        //Arcane-Void
        VoidEffect();

        if (this is PlayerStats)
        {
            OnCleanseAilments?.Invoke();
        }
    }

    public void SetAilmentStackValues()
    {
        for (int i = 0; i < ailmentPerStackValue.Length; i++)
        {
            ailmentPerStackValue[i] = maxHealth * ailmentStackThreshold[i];
        }
    }
    public void SetCurrentMoveSPeed()
    {
        currentMovementSpeed = baseMoveSpeed * (1 + moveSpeedMultiplier) * (1 - chillMoveSpeedReductionPerStack * ailmentCurrentStacks[1]);
        // Notify listeners that movement speed changed
        if (this is PlayerStats)
        {
            OnCurrentMoveSpeedChange?.Invoke();
        }

        // Notify listeners for enemies as well. 
        OnCurrentMoveSpeedChange?.Invoke();

    }

    public void SetCurrentAbilitySpeed()
    {
        currentActionSpeed = (1 + actionSpeed) * (1 - dazeActionSpeedReductionPerStack * ailmentCurrentStacks[3]);
        currentCooldownReduction = (1 + cooldownReduction) * (1 - dazeActionSpeedReductionPerStack * ailmentCurrentStacks[3]);
        currentWeaponActionSpeed = weaponActionSpeed * currentActionSpeed;

        // Notify listeners that action/cooldown/weapon action speed values changed
        if (this is PlayerStats)
        {
            OnAbilitySpeedChange?.Invoke();
        }
    }

    public void SetAilmentDamageTakenChange()
    {
        ailmentTakenMultiplier = (1 + shockAilmentIncreasePerStack * ailmentCurrentStacks[2]);

        if (this is PlayerStats)
        {
            OnAilmentDamageTakenChange?.Invoke();
        }
    }

    public void SetDamageTakenChange()
    {
        damageTakenMultiplier = (1 + voidDamageIncreasePerStack * ailmentCurrentStacks[4]);

        if (isFrozen)
        {
            damageTakenMultiplier += frozenDamageIncrease;
        }

        if (this is PlayerStats)
        {
            OnDamageTakenChange?.Invoke();
        }
    }

    public void CleanseAilments()
    {
        for(int i = 0; i < ailmentDamageStockpile.Length; i++)
        {
            ailmentDamageStockpile[i] = 0;
        }

        ChillEffect();
        ShockEffect();
        DazeEffect();
        VoidEffect();
    }

    // ---------------------------
    // AILMENT FUNCTIONS
    // ---------------------------
    public void BurnEffect()
    {
        float burnDamage = 0;

        if (ailmentDamageStockpile[0] < ailmentPerStackValue[0])
        {
            burnDamage = ailmentDamageStockpile[0];
        }
        else
        {
            burnDamage = ailmentPerStackValue[0];
        }

        TakeDamage(burnDamage);
        ailmentCurrentStacks[0] = (int)((ailmentDamageStockpile[0] * 100) / maxHealth);

        if (ailmentDamageStockpile[0] <= 0)
        {
            CancelInvoke(nameof(BurnEffect));
            isBurnning = false;
        }
    }

    public void ChillEffect()
    {
        int currentChillStacks = ailmentCurrentStacks[1];
        ailmentCurrentStacks[1] = (int)(ailmentDamageStockpile[1] / ailmentPerStackValue[1]);

        if(currentChillStacks != ailmentCurrentStacks[1])
        {
            SetCurrentMoveSPeed();
        }

        if (ailmentCurrentStacks[1] >= 10)
        {
            ailmentDamageStockpile[1] = 0;
            StartCoroutine(FrozenEffect());
        }

        SetCurrentMoveSPeed();
    }

    public IEnumerator FrozenEffect()
    {
        isFrozen = true;
        if (this is PlayerStats)
        {
            OnFrozen?.Invoke();
        }

        yield return new WaitForSeconds(frozenDuration);
        ChillEffect();
        isFrozen = false;
        SetDamageTakenChange();
    }

    public void ShockEffect()
    {
        int currentShockStacks = ailmentCurrentStacks[2];
        ailmentCurrentStacks[2] = (int)(ailmentDamageStockpile[2] / ailmentPerStackValue[2]);

        if (currentShockStacks != ailmentCurrentStacks[2])
        {
            SetAilmentDamageTakenChange();
        }

        if (ailmentCurrentStacks[2] >= 10)
        {
            ailmentDamageStockpile[2] -= 10 * ailmentPerStackValue[2];
            ShockEffect();
            if (this is PlayerStats)
            {
                gameObject.GetComponent<AbilityHandler>().TriggerSelfLightningStrike(10 * ailmentPerStackValue[2]);
            }

            if (this is EnemyStats)
            {
                GameObject.FindWithTag("Player").GetComponent<AbilityHandler>().TriggerEnemyLightningStrike(this.gameObject, 10 * ailmentPerStackValue[2]);
            }
        }
    }
    
    public void DazeEffect()
    {
        int currentDazeStacks = ailmentCurrentStacks[3];
        ailmentCurrentStacks[3] = (int)(ailmentDamageStockpile[3] / ailmentPerStackValue[3]);

        if (currentDazeStacks != ailmentCurrentStacks[2])
        {
            SetCurrentAbilitySpeed();
        }

        if (ailmentCurrentStacks[3] >= 10)
        {
            ailmentDamageStockpile[3] = 0;
            StartCoroutine(StunEffect());
        }

        SetCurrentAbilitySpeed();
    }
    public IEnumerator StunEffect()
    {
        isStunned = true;
        if (this is PlayerStats)
        {
            OnStunned?.Invoke();
        }

        yield return new WaitForSeconds(stunDuration);
        DazeEffect();
        isStunned = false;
    }

    public void VoidEffect()
    {
        int currentVoidStacks = ailmentCurrentStacks[4];
        ailmentCurrentStacks[4] = (int)(ailmentDamageStockpile[4] / ailmentPerStackValue[4]);

        if (currentVoidStacks != ailmentCurrentStacks[2])
        {
            SetDamageTakenChange();
        }  
    }

}
