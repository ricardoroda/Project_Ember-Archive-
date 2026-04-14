using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace Game.Handlers // SonarCube told me to use a namespace :)
{
    public class HubSpawner : MonoBehaviour
    {
        [Header("Player Prefab")]
        [SerializeField] private GameObject playerPrefab;
        [Header("Class SOs")]
        [SerializeField] private List<ClassSO> classOptions;
        [SerializeField] private Transform spawnPoint;

        private void Start()
        {
            if (classOptions == null || classOptions.Count == 0)
            {
                Debug.LogError("No class options assigned to HubSpawner.");
                return;
            }

            string className = PlayerPrefs.GetString("SelectedClass", classOptions[0].name);
            ClassSO chosen = classOptions.FirstOrDefault(c => c.name == className);
            if (chosen == null)
            {
                Debug.LogWarning($"No ClassSO found called \"{className}\". Defaulting to {classOptions[0].name}.");
                chosen = classOptions[0];
            }

            GameObject go = Instantiate(playerPrefab, spawnPoint.position, Quaternion.identity);
            var stats = go.GetComponent<PlayerStats>();
            if (stats != null)
            {
                stats.SetClassSO(chosen);
            }
            else
            {
                Debug.LogError("PlayerStats component not found on spawned player prefab.");
            }
        }
    }
}