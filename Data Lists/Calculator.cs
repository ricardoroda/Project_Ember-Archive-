using UnityEngine;

public static class Calculator
{
    public static float[] CalculateUserAbilityDamage(AbilitySO abilitySO, CharacterStats userStats)
    {
        float fireDamage = 0f;
        float waterDamage = 0f;
        float airDamage = 0f;
        float earthDamage = 0f;
        float arcaneDamage = 0f;

        fireDamage = abilitySO.baseFireDamage * (userStats.fireDamageMultiplier + 1) + userStats.fireDamageFlat * abilitySO.flatDamageMultiplier;
        waterDamage = abilitySO.baseWaterDamage * (userStats.waterDamageMultiplier + 1) + userStats.waterDamageFlat * abilitySO.flatDamageMultiplier;
        airDamage = abilitySO.baseAirDamage * (userStats.airDamageMultiplier + 1) + userStats.airDamageFlat * abilitySO.flatDamageMultiplier;
        earthDamage = abilitySO.baseEarthDamage * (userStats.earthDamageMultiplier + 1) + userStats.earthDamageFlat * abilitySO.flatDamageMultiplier;
        arcaneDamage = abilitySO.baseArcaneDamage * (userStats.arcaneDamageMultiplier + 1) + userStats.arcaneDamageFlat * abilitySO.flatDamageMultiplier;

        //Specific Weapon Damage Calculations
        if (abilitySO.weaponBasedDamageFlag)
        {
            fireDamage += abilitySO.weaponBaseDamage * userStats.weaponDamage[0] * (userStats.fireDamageMultiplier + 1);
            waterDamage += abilitySO.weaponBaseDamage * userStats.weaponDamage[1] * (userStats.waterDamageMultiplier + 1);
            airDamage += abilitySO.weaponBaseDamage * userStats.weaponDamage[2] * (userStats.airDamageMultiplier + 1);
            earthDamage += abilitySO.weaponBaseDamage * userStats.weaponDamage[3] * (userStats.earthDamageMultiplier + 1);
            arcaneDamage += abilitySO.weaponBaseDamage * userStats.weaponDamage[4] * (userStats.arcaneDamageMultiplier + 1);

            if(userStats.weaponActionSpeed == 0)
            {
                Debug.Log("No weapon equipped!");
            }
        }


        float[] abilityDamageTyped = { fireDamage , waterDamage, airDamage, earthDamage, arcaneDamage };

        return abilityDamageTyped;
    }

    public static float[] CalculateUserAbilityCrit(AbilitySO abilitySO, CharacterStats userStats, float[] abilityDamageTyped, bool critFlag)
    {

        float[] abilityDamageTypedCrit = new float[5];

        float abilityCritChange = abilitySO.baseCritChance * (userStats.critChance + 1);

        critFlag = (Random.Range(0, 1f) < abilityCritChange) ? true : false;

        if (critFlag)
        {
            for (int i = 0; i < abilityDamageTyped.Length; i++)
            {
                abilityDamageTypedCrit[i] = abilityDamageTyped[i] * (userStats.critMultiplier + 2);
            }
        }
        else
        {
            abilityDamageTypedCrit = abilityDamageTyped;
        }

        return abilityDamageTypedCrit;
    }

    public static float[] CalculateUserAbilityAilmentDamage(float[] abilityDamageTypedCrit, AbilitySO abilitySO, CharacterStats userStats)
    {
        float[] ailmentDamage = new float[5] { 0, 0, 0, 0, 0 };

        for (int i = 0; i < ailmentDamage.Length; i++)
        {
          ailmentDamage[i] = abilityDamageTypedCrit[i] * (abilitySO.baseAilmentMultiplier[i]/100) * (1 + userStats.ailmentMultiplier);
        }

        return ailmentDamage;
    }

    public static float CalculateAbilityFullDamage(float[] abilityDamageTypedCrit)
    {
        float abilityFullDamage = 0f;

        foreach (float damageType in abilityDamageTypedCrit)
        {
            abilityFullDamage += damageType;
        }

        return abilityFullDamage;
    }

    
}
