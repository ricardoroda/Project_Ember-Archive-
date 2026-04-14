using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

[System.Serializable]
public class WaveComposition
{
    public List<GameObject> enemies = new List<GameObject>();
    public List<int> enemyNumber = new List<int>();
}

[CreateAssetMenu(fileName = "SpawnerSO", menuName = "Scriptable Objects/SpawnerSO")]
public class SpawnerSO : ScriptableObject
{
    [Header("Spawn Attributes")]
    public List<WaveComposition> wavesComposition;
    public float waveSpawnTimer;
    public bool waveCheck = true;

    //Spawner Logistics
    public GameObject[] spawnerNests;
    public GameObject enemiesSpawnedHolder;
    public int enemyCount;
    public bool spawnFlag = true;
    [Header("Spawned Enemy Animation Setup")]
    [Tooltip("If true, SpawnerSO will try to configure the spawned enemy's AnimationHandler so it doesn't read player input.")]
    public bool configureSpawnedAnimationHandler = true;
    public string spawnedMoveXParameter = "MoveX";
    public string spawnedMoveYParameter = "MoveY";
    public string spawnedSpeedParameter = "Speed";
    [Tooltip("Damp time to set on the spawned AnimationHandler (applies when configureSpawnedAnimationHandler is true)")]
    public float spawnedDampTime = 0.08f;
    [Tooltip("If true, the spawner will overwrite parameter name fields on the spawned AnimationHandler. Leave false to preserve prefab-specific parameter names.")]
    public bool overrideSpawnedParameterNames = false;

    //Spawns a Wave - returns TRUE if theres a wave & FALSE if its there are no more waves
    public bool SpawnWave(int waveNumber)
    {
        FindFirstObjectByType<EnemyCount>()?.SetWave(waveNumber);

        if(wavesComposition.Count < waveNumber)
        {
            return false;
        }

        if (wavesComposition == null || wavesComposition[waveNumber - 1] == null)
        {
            Debug.LogWarning("SpawnerSO.SpawnWave: Waves Composition not set.");
            return false;
        }

        for (int i = 0; i < wavesComposition[waveNumber - 1].enemies.Count; i++)
        {
            SpawnEnemies(wavesComposition[waveNumber - 1].enemies[i], wavesComposition[waveNumber - 1].enemyNumber[i]);
        }

        return true;
    }

    public void SpawnEnemies(GameObject enemyType, int enemyAmount)
    {
        if (enemyType == null)
        {
            Debug.LogWarning("SpawnerSO.SpawnEnemies called with null enemyType.");
            return;
        }

        if (spawnerNests == null || spawnerNests.Length == 0)
        {
            Debug.LogWarning("SpawnerSO.SpawnEnemies: No spawner nests defined.");
            return;
        }

        if (enemiesSpawnedHolder == null)
        {
            enemiesSpawnedHolder = new GameObject("EnemiesSpawnedHolder");
        }

        foreach (GameObject spawnerNest in spawnerNests)
        {
            if (spawnerNest == null)
                continue;

            BoxCollider box = spawnerNest.GetComponent<BoxCollider>();
            if (box == null)
            {
                Debug.LogWarning($"SpawnerSO: spawnerNest '{spawnerNest.name}' missing BoxCollider, skipping.");
                continue;
            }

            for(int i = 0; i < enemyAmount; i++)
            {
                var enemyInstance = Instantiate(enemyType, GetRandomSpawnPosition(spawnerNest), enemyType != null ? enemyType.transform.rotation : Quaternion.identity);

                // Setup self reference in the spawned enemy's AI and record the
                // original prefab on the stats component so the enemy can return
                // itself to the correct pool when it dies.
                var statsComp = enemyInstance.GetComponent<EnemyStats>();
                if (statsComp != null)
                {
                    // pooling removed: no prefab reference stored on EnemyStats anymore.
                }

                EnemyAI ai = enemyInstance.GetComponent<EnemyAI>();
                if (ai != null)
                {
                    ai.enemyInstance = enemyInstance;
                }
                else
                {
                    Debug.LogWarning("SpawnerSO: Instantiated enemy missing EnemyAI component.");
                }

                // Configure AnimationHandler on spawned enemies so they don't sample player input
                if (configureSpawnedAnimationHandler)
                {
                    var animHandler = enemyInstance.GetComponent<AnimationHandler>();
                    if (animHandler == null)
                        animHandler = enemyInstance.GetComponentInChildren<AnimationHandler>();

                    if (animHandler != null)
                    {
                        animHandler.useInput = false;
                        animHandler.dampTime = spawnedDampTime;

                        if (overrideSpawnedParameterNames)
                        {
                            animHandler.moveXParameter = spawnedMoveXParameter;
                            animHandler.moveYParameter = spawnedMoveYParameter;
                            animHandler.speedParameter = spawnedSpeedParameter;
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"SpawnerSO: Spawned enemy '{enemyInstance.name}' has no AnimationHandler to configure.");
                    }
                }

                if (enemyInstance != null && enemiesSpawnedHolder != null)
                    enemyInstance.transform.SetParent(enemiesSpawnedHolder.transform, true);
            }
        }
    }

    Vector3 GetRandomSpawnPosition(GameObject spawnerNest)
    {
        Vector3 spawnPositionCenter = spawnerNest.transform.position;

        float x = UnityEngine.Random.Range(-spawnerNest.GetComponent<BoxCollider>().size.x / 2, spawnerNest.GetComponent<BoxCollider>().size.x / 2);
        float z = UnityEngine.Random.Range(-spawnerNest.GetComponent<BoxCollider>().size.z / 2, spawnerNest.GetComponent<BoxCollider>().size.z / 2);

        return spawnPositionCenter + new Vector3(x, 0, z);
    }

    public IEnumerator SpawnTimer()
    {
        yield return new WaitForSeconds(waveSpawnTimer);
    }

}
