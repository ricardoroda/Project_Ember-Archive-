using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Handlers
{
    public class GameController : MonoBehaviour
    {
        [SerializeField] private CameraController cameraController;
        [SerializeField] private AbilityHandler abilityHandler;
        [Header("Ability use")]
        [SerializeField] private bool disableAbilitiesInScenes = true;

        public enum GateMode { ByName, ByBuildIndex, ByTag }
        [SerializeField] private GateMode gateMode = GateMode.ByName;

        [Tooltip("List of scene names where abilities should be disabled")]
        [SerializeField] private string[] DisabledSceneNames = new string[] { "Hub" };

        [Tooltip("List of build indices where abilities should be disabled")]
        [SerializeField] private int[] DisabledSceneBuildIndices = new int[0];

        [Tooltip("Scene GameObject tag used to mark scenes that should disable abilities")]
        [SerializeField] private string DisabledSceneTag = "HubScene";
        public SpawnerSO arenaSpawnerConfig;
        public SpawnerSO forestSpawnerConfig;
        public SpawnerSO arenaSpawnerConfigInstance;
        public SpawnerSO forestSpawnerConfigInstance;
        public int waveNumber = 1;
        public bool abilityUse = true;

        // cached reference to the currently active scene
        private Scene currentScene;
        public int tier = 1;

        //Save Logistics
        public PlayerSaveSO playerSaveFileSO;
        public GameObject playerPrefab;


        void Awake()
        {
            DontDestroyOnLoad(gameObject);
            currentScene = SceneManager.GetActiveScene();
            // init helpers
            InitializeCameraController();

            if (PlayerController.InstanceTransform != null)
            {
                if (cameraController != null)
                {
                    SetCameraTarget(PlayerController.InstanceTransform);
                }
                else
                {
                    Debug.LogWarning("GameController: CameraController not found - cannot set camera target. Assign in inspector or add a CameraController to the scene.");
                }
            }
            arenaSpawnerConfigInstance = Instantiate(arenaSpawnerConfig);
            forestSpawnerConfigInstance =  Instantiate(forestSpawnerConfig);

            // find AbilityHandler if not assigned
            InitializeAbilityHandler();
        }

        void Start()
        {
            // make sure ability state matches the initial scene
            ApplyAbilityStateForScene(currentScene);

            SceneManager.sceneLoaded += OnSceneLoaded;
            // populate spawner references only if abilities are allowed in this scene
            InitializeSpawnerIfNeeded(currentScene);


        }

        private void Update()
        {
            // handle spawning each frame
            if(currentScene == SceneManager.GetSceneByBuildIndex(2) || currentScene == SceneManager.GetSceneByBuildIndex(3))
            HandleSpawnerUpdate();
        }

        private void OnEnable()
        {
            PlayerController.OnPlayerSpawned += SetCameraTarget;
        }

        private void OnDisable()
        {
            PlayerController.OnPlayerSpawned -= SetCameraTarget;
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void SetCameraTarget(Transform playerTransform)
        {
            if (cameraController == null)
            {
                Debug.LogWarning("SetCameraTarget called but cameraController is null. Ignoring.");
                return;
            }

            cameraController.SetTarget(playerTransform);
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (SceneManager.GetActiveScene().buildIndex != 0)
            {
                playerPrefab.GetComponent<PlayerStats>().playerSaveSlotSO = playerSaveFileSO;
                SetTierLevel();
                GameObject playerSpawnPoint = GameObject.FindGameObjectWithTag("PlayerSpawnPoint");
                Instantiate(playerPrefab, new Vector3(playerSpawnPoint.transform.position.x, playerPrefab.transform.position.y, playerSpawnPoint.transform.position.z), playerSpawnPoint.transform.rotation);
            }

            currentScene = SceneManager.GetActiveScene();
            ApplyAbilityStateForScene(scene);
            // re-init spawner when scene changes
            InitializeSpawnerIfNeeded(scene);
        }

        /// Update the ability handler to either allow or disallow abilities for the scene

        private void ApplyAbilityStateForScene(Scene scene)
        {
            bool shouldDisable = IsAbilityDisabledInScene(scene);

            if (abilityHandler != null)
            {
                if (shouldDisable) abilityHandler.DisableAbilities();
                else abilityHandler.EnableAbilities();
            }
            else if (shouldDisable)
            {
                Debug.LogWarning($"GameController: Abilities should be disabled for scene '{scene.name}', but AbilityHandler is not assigned.");
            }
        }


        /// Return true when abilities should be disabled for the scene according to the configured gate mode

        private bool IsAbilityDisabledInScene(Scene scene)
        {
            if (!disableAbilitiesInScenes) return false;

            switch (gateMode)
            {
                case GateMode.ByName:
                    if (DisabledSceneNames != null)
                    {
                        foreach (var sceneName in DisabledSceneNames)
                        {
                            if (!string.IsNullOrEmpty(sceneName) && sceneName == scene.name) return true;
                        }
                    }
                    return false;
                case GateMode.ByBuildIndex:
                    if (DisabledSceneBuildIndices != null)
                    {
                        foreach (var buildIndex in DisabledSceneBuildIndices)
                            if (buildIndex == scene.buildIndex) return true;
                    }
                    return false;
                case GateMode.ByTag:
                    if (!string.IsNullOrEmpty(DisabledSceneTag))
                    {
                        var taggedObject = GameObject.FindWithTag(DisabledSceneTag);
                        return taggedObject != null;
                    }
                    return false;
                default:
                    return false;
            }
        }

        // find or cache the camera controller
        private void InitializeCameraController()
        {
            if (cameraController != null) return;

            cameraController = GetComponent<CameraController>();
            if (cameraController == null)
            {
                cameraController = FindFirstObjectByType<CameraController>();
            }
        }

        // find ability handler if not assigned
        private void InitializeAbilityHandler()
        {
            if (abilityHandler != null) return;
            abilityHandler = FindFirstObjectByType<AbilityHandler>();
        }

        // populate spawner references when entering a scene that allows abilities
        private void InitializeSpawnerIfNeeded(Scene scene)
        {
            if (IsAbilityDisabledInScene(scene)) return;

            if (GameObject.FindGameObjectWithTag("EnemiesSpawnedHolder"))
            {
                if (currentScene == SceneManager.GetSceneByBuildIndex(2))
                {
                    arenaSpawnerConfigInstance.spawnerNests = GameObject.FindGameObjectsWithTag("Spawner");
                    arenaSpawnerConfigInstance.enemiesSpawnedHolder = GameObject.FindGameObjectWithTag("EnemiesSpawnedHolder");
                    arenaSpawnerConfigInstance.waveCheck = true;
                }

                if (currentScene == SceneManager.GetSceneByBuildIndex(3))
                {
                   
                    forestSpawnerConfigInstance.spawnerNests = GameObject.FindGameObjectsWithTag("Spawner");
                    forestSpawnerConfigInstance.enemiesSpawnedHolder = GameObject.FindGameObjectWithTag("EnemiesSpawnedHolder");
                    arenaSpawnerConfigInstance.waveCheck = true;
                }
            }
            else
            {
                Debug.LogWarning("GameController: spawnerConfig is not assigned. Spawner-related behaviour will be disabled.");
            }
        }

        // handle spawn checks and wave progression
        private void HandleSpawnerUpdate()
        {
            if (IsAbilityDisabledInScene(currentScene)) return;
            if (GameObject.FindGameObjectWithTag("EnemiesSpawnedHolder") == null) return;

            if (currentScene == SceneManager.GetSceneByBuildIndex(2))
            {
                arenaSpawnerConfigInstance.enemyCount = GameObject.FindGameObjectsWithTag("Enemy").Length;

                if (arenaSpawnerConfigInstance.enemyCount == 0 && arenaSpawnerConfigInstance.spawnFlag && arenaSpawnerConfigInstance.waveCheck)
                {
                    StartCoroutine(arenaSpawnerConfigInstance.SpawnTimer());
                    arenaSpawnerConfigInstance.waveCheck = arenaSpawnerConfigInstance.SpawnWave(waveNumber);
                    waveNumber++;
                }
            }

            if (currentScene == SceneManager.GetSceneByBuildIndex(3))
            {
                forestSpawnerConfigInstance.enemyCount = GameObject.FindGameObjectsWithTag("Enemy").Length;

                if (forestSpawnerConfigInstance.spawnFlag && forestSpawnerConfigInstance.waveCheck)
                {
                    StartCoroutine(forestSpawnerConfigInstance.SpawnTimer());
                    forestSpawnerConfigInstance.waveCheck = forestSpawnerConfigInstance.SpawnWave(waveNumber);
                    waveNumber++;
                }
            }
        }

        public void SetTierLevel()
        {
            int playerLevel = playerSaveFileSO.level;

            if (playerLevel > 0 && playerLevel <= 20)
            {
                tier = 1;
            }
            else if (playerLevel > 20 && playerLevel < 41)
            {
                tier = 2;
            }
            else
            {
                tier = 3;
            }
        }
    }
}
