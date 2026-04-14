using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

public class EnemyAI : MonoBehaviour
{
    //Enemy General Variables
    public EnemyStats enemyStats;
    private NavMeshAgent _agent;
    private Transform _player;
    private float _lastAttack;
    private AnimationHandler _animHandler;

    //Abilities Associated Variables
    private Vector3 _currentPosition;
    private Vector3 _hitPoint;
    public GameObject[] abilitiesPrefab;
    public List<GameObject> abilities;
    public List<AbilitySO> abilityData;
    private readonly List<bool> _abilityRefreshedFlag = new List<bool>();
    private readonly List<float> _cooldownTimer = new List<float>();
    public GameObject enemyInstance;
    public GameObject abilitiesInstancesHolder;

    //Enemy State Variables
    enum State { Idle, Chase, Attack, Paralyzed }
    private State _state = State.Idle;

    // AI settings
    [Header("AI Settings")]
    [Tooltip("Optional ScriptableObject configuration. If assigned, values will be loaded from it unless per-instance overrides are provided.")]
    public EnemyAiConfigSo aiConfig;

    [Tooltip("Optional detection radius. If <= 0 the enemy will use EnemySO.sightRange or the SO value if provided.")]
    public float detectionRadius = 0f;

    [Tooltip("Field of view angle in degrees. Set to 360 for omnidirectional detection.")]
    [Range(0f, 360f)]
    public float fieldOfView = 360f;

    [Tooltip("How long (seconds) the enemy will remember the player's last seen position after losing sight")]
    public float memoryDuration = 3f;

    [Tooltip("Eye height used for line-of-sight checks (world units)")]
    public float eyeHeight = 1f;

    [Tooltip("Layer mask used to consider obstacles that block vision")]
    public LayerMask obstacleMask = Physics.DefaultRaycastLayers;

    [Tooltip("Boss detection multiplier (bosses can have larger detection radius)")]
    public float bossDetectionMultiplier = 1.5f;

    // Debug gizmo options (toggleable in inspector)
    [Header("Debug (Gizmos)")]
    [Tooltip("Show detection radius and FOV in the Scene view")]
    public bool debugDrawVision = true;
    [Tooltip("Show last known player position and sight line")]
    public bool debugDrawLastKnown = true;

    // runtime sight memory
    private bool _hasSeenPlayer;
    private Vector3 _lastKnownPlayerPos;
    private float _lastSeenTime = -Mathf.Infinity;

    // avoid spamming console when player not yet present
    private bool _loggedNoPlayer;

    // Patrol runtime
    private Vector3 _spawnPosition;
    private readonly List<Vector3> _patrolPoints = new List<Vector3>();
    private int _currentPatrolIndex;
    private float _patrolWaitTimer;
    private bool _isPatrolling;

    // Cached base speed
    private float _baseAgentSpeed;

    // Internal single-time warnings to avoid console spam
    private bool _loggedSamplePositionFailure;

    // ---------------------------------------------------------------------------------------------------------------------------------------
    // ON RUNNING FUNCTIONS
    // ---------------------------------------------------------------------------------------------------------------------------------------
    private void Awake()
    {
        enemyStats = GetComponent<EnemyStats>();
        _agent = GetComponent<NavMeshAgent>();

        // find AnimationHandler on this enemy (root or child) so AI can drive animations
        _animHandler = GetComponent<AnimationHandler>();
        if (_animHandler == null) _animHandler = GetComponentInChildren<AnimationHandler>();
        if (_animHandler != null) _animHandler.useInput = false;

        // Setup Enemy Data (Target & SO info)
        if (PlayerController.InstanceTransform != null)
        {
            SetPlayerTarget(PlayerController.InstanceTransform);
        }

        ApplyConfigNavMesh();
    }

#if UNITY_EDITOR
    // Draw helpful debug gizmos when the enemy is selected in the Scene view
    private void OnDrawGizmosSelected()
    {
        if (!debugDrawVision && !debugDrawLastKnown) return;

        // draw detection radius (use effective radius considering boss multiplier)
        float effectiveRadius = detectionRadius > 0f ? detectionRadius : (enemyStats != null ? enemyStats.sightRange : 0f);
        if (enemyStats != null && enemyStats.isBoss) effectiveRadius *= bossDetectionMultiplier;

        if (debugDrawVision && effectiveRadius > 0f)
        {
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.15f);
            Gizmos.DrawSphere(transform.position, effectiveRadius);

            // draw field of view lines if not full 360
            if (fieldOfView < 360f)
            {
                Vector3 origin = transform.position + Vector3.up * eyeHeight;
                float halfFov = fieldOfView * 0.5f;
                Quaternion leftRot = Quaternion.Euler(0f, -halfFov, 0f);
                Quaternion rightRot = Quaternion.Euler(0f, halfFov, 0f);
                Vector3 leftDir = leftRot * transform.forward;
                Vector3 rightDir = rightRot * transform.forward;

                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(origin, origin + leftDir.normalized * effectiveRadius);
                Gizmos.DrawLine(origin, origin + rightDir.normalized * effectiveRadius);
            }
        }

        // draw last known player position and line-to-player visibility
        if (debugDrawLastKnown && _hasSeenPlayer)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawSphere(_lastKnownPlayerPos, 0.25f);

            if (_player != null)
            {
                Vector3 origin = transform.position + Vector3.up * eyeHeight;
                Vector3 target = _player.position + Vector3.up * eyeHeight;
                Gizmos.color = CanSeePlayer() ? Color.green : Color.red;
                Gizmos.DrawLine(origin, target);
            }
        }
    }
#endif

    private void OnEnable()
    {
        PlayerController.OnPlayerSpawned += SetPlayerTarget;
    }

    private void OnDisable()
    {
        PlayerController.OnPlayerSpawned -= SetPlayerTarget;
    }

    void Start()
    {
        // Validate runtime prerequisites and initialize
        if (!ValidateRuntimePrereqs()) return;
        InitializeRuntime();
    }

    /// <summary>
    /// Validate that required components and data are present before initializing runtime state.
    /// Extracts branching out of Start to reduce cognitive complexity.
    /// </summary>
    private bool ValidateRuntimePrereqs()
    {
        // prefer previously cached enemyStats (set in Awake), otherwise try to get it now
        if (enemyStats == null) enemyStats = GetComponent<EnemyStats>();

        if (enemyStats == null)
        {
            Debug.LogWarning($"EnemyAI: EnemyStats missing on '{gameObject.name}'. AI will not initialize.");
            return false;
        }

        return true;
    }

    private void InitializeRuntime()
    {
        // Load configuration from ScriptableObject if present, otherwise keep inspector values
        ApplyConfigFromSo();

        // initialize detection radius from stats if not set in inspector or SO
        if (detectionRadius <= 0f)
            detectionRadius = enemyStats != null ? enemyStats.sightRange : detectionRadius;

        // enlarge detection for bosses (apply multiplier last so it affects final radius)
        if (enemyStats != null && enemyStats.isBoss)
            detectionRadius *= bossDetectionMultiplier;

        // ensure ability lists are initialized so Add/ indexing is safe
        if (abilityData == null) abilityData = new List<AbilitySO>();
        if (abilities == null) abilities = new List<GameObject>();

        // record spawn position for patrol generation
        _spawnPosition = transform.position;
        _baseAgentSpeed = _agent != null ? _agent.speed : 0f;

        // generate patrol points if configured via SO
        if (aiConfig != null && aiConfig.patrolEnabled)
        {
            GeneratePatrolPoints();
            if (_patrolPoints.Count > 0)
            {
                _isPatrolling = true;
                // set initial patrol speed
                if (_agent != null) _agent.speed = _baseAgentSpeed * Mathf.Max(0.01f, aiConfig.patrolSpeedMultiplier);
            }
        }

        // If we're not patrolling, make sure the agent is not moving right away
        if (_agent != null && !_isPatrolling)
        {
            try
            {
                _agent.ResetPath();
                _agent.isStopped = true;
            }
            catch { }
        }

        // Setup ability instances
        SetupAbilitiesFromPrefabs();
    }

    /// <summary>
    /// Apply values from the assigned AI config ScriptableObject.
    /// Inspector values can still override by being set after this runs.
    /// </summary>
    private void ApplyConfigFromSo()
    {
        if (aiConfig == null) return;

        // Detection radius precedence:
        // 1) explicit inspector detectionRadius (> 0)
        // 2) AI SO detectionRadius (> 0)
        // 3) enemyStats.sightRange (applied later if still <= 0)
        if (detectionRadius <= 0f && aiConfig.detectionRadius > 0f)
            detectionRadius = aiConfig.detectionRadius;

        // Always prefer SO for fields that are intended to be centralized
        fieldOfView = aiConfig.fieldOfView;

        if (aiConfig.memoryDuration > 0f)
            memoryDuration = aiConfig.memoryDuration;

        if (aiConfig.eyeHeight > 0f)
            eyeHeight = aiConfig.eyeHeight;

        obstacleMask = aiConfig.obstacleMask;

        if (aiConfig.bossDetectionMultiplier > 0f)
            bossDetectionMultiplier = aiConfig.bossDetectionMultiplier;

        // Note: movement multipliers (patrol/chase/kite) are applied at runtime when switching states.
    }

#if UNITY_EDITOR
    // Keep the inspector preview in sync with the SO when edited in Editor.
    private void OnValidate()
    {
        // In edit-mode only copy lightweight values suitable for preview to avoid expensive ops.
        if (aiConfig == null) return;

        // Do not overwrite explicit runtime inspector overrides for detectionRadius if set (>0)
        if (detectionRadius <= 0f && aiConfig.detectionRadius > 0f)
            detectionRadius = aiConfig.detectionRadius;

        fieldOfView = aiConfig.fieldOfView;
        if (aiConfig.memoryDuration > 0f) memoryDuration = aiConfig.memoryDuration;
        if (aiConfig.eyeHeight > 0f) eyeHeight = aiConfig.eyeHeight;
        obstacleMask = aiConfig.obstacleMask;
        if (aiConfig.bossDetectionMultiplier > 0f) bossDetectionMultiplier = aiConfig.bossDetectionMultiplier;
    }
#endif

    private void SetupAbilitiesFromPrefabs()
    {
        // find or create the holder for ability instances
        if (abilitiesInstancesHolder == null)
            abilitiesInstancesHolder = GameObject.FindGameObjectWithTag("AbilitiesInstancesHolder");

        if (abilitiesPrefab == null || abilitiesPrefab.Length == 0) return;

        // Ensure lists are sized appropriately
        for (int i = 0; i < abilitiesPrefab.Length; i++)
        {
            // ensure data lists have a slot
            if (abilityData.Count <= i) abilityData.Add(null);
            if (_abilityRefreshedFlag.Count <= i) _abilityRefreshedFlag.Add(true);
            if (_cooldownTimer.Count <= i) _cooldownTimer.Add(0f);
            if (abilities.Count <= i) abilities.Add(null);

            var prefab = abilitiesPrefab[i];
            if (prefab == null) continue;

            var behaviour = prefab.GetComponent<AbilityBehaviour>();
            if (behaviour != null)
            {
                abilityData[i] = behaviour.GetAbilitySO();
            }

            SetUpAbilityInstance(i);
        }
    }

    private void GeneratePatrolPoints()
    {
        _patrolPoints.Clear();
        if (aiConfig == null) return;

        int count = Mathf.Max(0, aiConfig.patrolPointCount);
        float radius = Mathf.Max(0.1f, aiConfig.patrolRadius);

        for (int i = 0; i < count; i++)
        {
            // try to find a valid point on the NavMesh near a random direction
            Vector3 randomPoint = _spawnPosition + Random.insideUnitSphere * radius;
            randomPoint.y = _spawnPosition.y + 1f; // lift slightly to sample
            NavMeshHit hit;
            if (NavMesh.SamplePosition(randomPoint, out hit, radius, NavMesh.AllAreas))
            {
                _patrolPoints.Add(hit.position);
            }
        }

        // fallback: if generation failed, create one point at spawn
        if (_patrolPoints.Count == 0)
        {
            _patrolPoints.Add(_spawnPosition);
        }
    }

    // ------------------------------------------
    // ENEMY AI LOGIC & FUNCTIONS
    // ------------------------------------------

    private void SetPlayerTarget(Transform playerTransform)
    {
        if (playerTransform != null)
        {
            _player = playerTransform;
            _loggedNoPlayer = false; // reset the one-time warning
        }
    }

    public void ApplyConfigNavMesh()
    {
        if (_agent == null)
        {
            Debug.LogWarning($"EnemyAI.ApplyConfigNavMesh: NavMeshAgent missing on '{gameObject.name}'. Skipping nav config.");
            return;
        }

        if (enemyStats != null)
        {
            _agent.speed = enemyStats.currentMovementSpeed;
            _agent.acceleration = enemyStats.movementAcceleration;
            _agent.stoppingDistance = enemyStats.stoppingDistance;
        }
    }

    void Update()
    {
        UpdateAI();
    }

    private void UpdateAI()
    {
        // Calculate Targeted Position and Rotation for Projectile
        _currentPosition = new Vector3(transform.position.x, 1, transform.position.z);

        // Guard against missing player reference (player may not be spawned yet)
        if (_player == null)
        {
            if (!_loggedNoPlayer)
            {
                Debug.LogWarning($"EnemyAI.Update: Player target not found on '{gameObject.name}'. Waiting for spawn.");
                _loggedNoPlayer = true;
            }
            return;
        }

        _hitPoint = new Vector3(_player.position.x, 1, _player.position.z);

        float dist = Vector3.Distance(transform.position, _player.position);

        // Update sight memory
        bool canSee = CanSeePlayer();
        if (canSee)
        {
            _hasSeenPlayer = true;
            _lastKnownPlayerPos = _player.position;
            _lastSeenTime = Time.time;
        }

        HandleState(dist, canSee);

        DriveAnimations();
    }

    private void HandleState(float dist, bool canSee)
    {
        switch (_state)
        {
            case State.Idle:
                HandleIdleState(canSee);
                break;
            case State.Chase:
                HandleChaseState(dist, canSee);
                break;
            case State.Attack:
                HandleAttackState(dist);
                break;
            case State.Paralyzed:
                HandleParalyzedState();
                break;
        }
    }

    private void HandleIdleState(bool canSee)
    {
        if (enemyStats.isFrozen || enemyStats.isStunned)
        {
            _state = State.Paralyzed;
            return;
        }

        // Patrol behavior while idle (only if not alerted)
        if (!_hasSeenPlayer && aiConfig != null && aiConfig.patrolEnabled && _isPatrolling)
        {
            UpdatePatrol();
        }

        bool shouldChase = canSee || (_hasSeenPlayer && Time.time - _lastSeenTime <= memoryDuration && Vector3.Distance(transform.position, _lastKnownPlayerPos) <= detectionRadius);
        if (shouldChase)
        {
            // when transitioning to chase, increase agent speed per config
            if (_agent != null && aiConfig != null) _agent.speed = _baseAgentSpeed * Mathf.Max(0.01f, aiConfig.chaseSpeedMultiplier);
            _state = State.Chase;
        }
    }

    private void HandlePatrolArrival()
    {
        if (_patrolWaitTimer <= 0f)
        {
            _patrolWaitTimer = aiConfig.patrolWaitTime;
            return;
        }

        _patrolWaitTimer -= Time.deltaTime;
        if (_patrolWaitTimer <= 0f)
        {
            _currentPatrolIndex = (_currentPatrolIndex + 1) % _patrolPoints.Count;
            _agent.SetDestination(_patrolPoints[_currentPatrolIndex]);
        }
    }

    private void UpdatePatrol()
    {
        if (_patrolPoints.Count == 0 || _agent == null) return;

        // If agent already has a path, let it continue. Otherwise evaluate distance and act.
        if (_agent.hasPath) return;

        Vector3 target = _patrolPoints[_currentPatrolIndex];
        float distToPoint = Vector3.Distance(transform.position, target);
        if (distToPoint <= 0.6f)
        {
            HandlePatrolArrival();
            return;
        }

        // go to the patrol target
        _agent.SetDestination(target);
    }

    private void HandleChaseMovement(bool canSee)
    {
        if (_agent == null) return;

        if (canSee)
        {
            _agent.SetDestination(_player.position);
            return;
        }

        // move toward last known position when we lost sight
        _agent.SetDestination(_lastKnownPlayerPos);
    }

    private void HandleChaseState(float dist, bool canSee)
    {
        HandleChaseMovement(canSee);

        // if we've lost sight for longer than memory, give up
        if (!canSee && Time.time - _lastSeenTime > memoryDuration)
        {
            _hasSeenPlayer = false;
            _state = State.Idle;
            if (_agent != null) _agent.ResetPath();
            return;
        }

        if (enemyStats.isFrozen || enemyStats.isStunned)
        {
            _state = State.Paralyzed;
            return;
        }

        if (dist <= enemyStats.attackRange)
        {
            _state = State.Attack;
            // Reset agent speed to chase multiplier while attacking if configured
            if (_agent != null && aiConfig != null) _agent.speed = _baseAgentSpeed * Mathf.Max(0.01f, aiConfig.chaseSpeedMultiplier);
            return;
        }

        if (!canSee && !_hasSeenPlayer)
        {
            _state = State.Idle;
            // revert patrol speed if applicable
            if (_agent != null && aiConfig != null && aiConfig.patrolEnabled && _isPatrolling)
                _agent.speed = _baseAgentSpeed * Mathf.Max(0.01f, aiConfig.patrolSpeedMultiplier);
        }
    }

    private void HandleAttackState(float dist)
    {
        // Decide between kiting (ranged) or melee attack behavior
        bool useKiting = false;
        if (aiConfig != null && aiConfig.rangedKitingEnabled && (aiConfig.forceRanged || DetermineIfRanged())) useKiting = true;

        if (useKiting)
        {
            // Kiting: keep preferred distance while attacking
            DoKitingBehavior(dist);
        }
        else
        {
            if (_agent != null) _agent.ResetPath();
            FacePlayer();

            if (!enemyStats.isFrozen && !enemyStats.isStunned)
            {
                ProcessAttackState();
            }
        }

        if (enemyStats.isFrozen || enemyStats.isStunned)
        {
            _state = State.Paralyzed;
        }
        else if (dist > enemyStats.attackRange * 0.5f)
        {
            _state = State.Chase;
            // ensure chase speed applied
            if (_agent != null && aiConfig != null) _agent.speed = _baseAgentSpeed * Mathf.Max(0.01f, aiConfig.chaseSpeedMultiplier);
        }
    }

    private bool DetermineIfRanged()
    {
        // Basic heuristic: if forced ranged in SO, or any ability has a lifetime / projectile-like property
        if (aiConfig != null && aiConfig.forceRanged) return true;
        if (abilityData == null) return false;
        for (int i = 0; i < abilityData.Count; i++)
        {
            var so = abilityData[i];
            if (so == null) continue;
            // Use lifeTime > 0 or fireRate as a hint of ranged projectiles — conservative heuristic
            if (so.lifeTime > 0f) return true;
        }
        return false;
    }

    private void DoKitingBehavior(float dist)
    {
        if (_agent == null || _player == null || aiConfig == null) return;

        float preferred = Mathf.Max(0.1f, aiConfig.preferredKitingDistance);
        float kiteSpeed = _baseAgentSpeed * Mathf.Max(0.01f, aiConfig.kiteSpeedMultiplier);

        // If too close, retreat; if too far, approach; otherwise strafe
        Vector3 dirToPlayer = (_player.position - transform.position);
        dirToPlayer.y = 0f;

        if (dist < preferred - 0.5f)
        {
            // retreat away from player
            Vector3 retreatDir = (transform.position - _player.position).normalized;
            Vector3 retreatTarget = transform.position + retreatDir * (preferred - dist + 1f);
            NavMeshHit hit;
            if (NavMesh.SamplePosition(retreatTarget, out hit, 2f, NavMesh.AllAreas))
            {
                _agent.SetDestination(hit.position);
                _agent.speed = kiteSpeed;
            }
        }
        else if (dist > preferred + 1f)
        {
            // approach to preferred distance
            Vector3 approachDir = (_player.position - transform.position).normalized;
            Vector3 approachTarget = _player.position - approachDir * preferred;
            NavMeshHit hit;
            if (NavMesh.SamplePosition(approachTarget, out hit, 2f, NavMesh.AllAreas))
            {
                _agent.SetDestination(hit.position);
                _agent.speed = kiteSpeed;
            }
        }
        else
        {
            // strafe around the player: pick a perpendicular direction
            Vector3 perp = Vector3.Cross(Vector3.up, dirToPlayer).normalized;
            Vector3 strafeTarget = transform.position + perp * 2f;
            NavMeshHit hit;
            if (NavMesh.SamplePosition(strafeTarget, out hit, 2f, NavMesh.AllAreas))
            {
                _agent.SetDestination(hit.position);
                _agent.speed = kiteSpeed;
            }
        }
        // still face the player while kiting
        FacePlayer();
        // use abilities while kiting
        if (!enemyStats.isFrozen && !enemyStats.isStunned)
        {
            ProcessAttackState();
        }
    }

    private void HandleParalyzedState()
    {
        if (_agent != null)
        {
            _agent.ResetPath();
        }

        if (!enemyStats.isFrozen && !enemyStats.isStunned)
        {
            _state = State.Idle;
        }
    }

    private void ProcessAttackState()
    {
        for (int i = 0; i < abilities.Count; i++)
        {
            // guard against missing entries
            if (_abilityRefreshedFlag.Count <= i || abilityData.Count <= i || abilities.Count <= i) continue;
            if (abilityData[i] == null || abilities[i] == null) continue;

            UseAbility(i);
            AbilityRefreshTimer(i);
        }
    }

    private void DriveAnimations()
    {
        if (_animHandler != null)
        {
            Vector3 vel = Vector3.zero;
            if (_agent != null)
                vel = _agent.velocity;

            Vector2 move2 = new Vector2(vel.x, vel.z);
            _animHandler.SetMoveInput(move2);
        }
    }

    bool CanSeePlayer()
    {
        if (_player == null || enemyStats == null) return false;

        float effectiveRadius = detectionRadius > 0f ? detectionRadius : enemyStats.sightRange;
        float dist = Vector3.Distance(transform.position, _player.position);
        if (dist > effectiveRadius) return false;

        // Field of view check (if configured)
        if (fieldOfView < 360f)
        {
            Vector3 toPlayer = (_player.position - transform.position).normalized;
            toPlayer.y = 0f;
            Vector3 forward = transform.forward;
            forward.y = 0f;
            if (Vector3.Angle(forward, toPlayer) > fieldOfView * 0.5f) return false;
        }

        Vector3 origin = transform.position + Vector3.up * eyeHeight;
        Vector3 target = _player.position + Vector3.up * eyeHeight;

        RaycastHit hit;
        if (Physics.Linecast(origin, target, out hit, obstacleMask))
        {
            if (hit.collider != null && (hit.collider.transform == _player || hit.collider.transform.IsChildOf(_player)))
            {
                return true;
            }
            return false;
        }

        return true;
    }

    void FacePlayer()
    {
        Vector3 dir = (_player.position - transform.position).normalized;
        dir.y = 0;

        if (dir.magnitude > 0f)
        {
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                Quaternion.LookRotation(dir),
                10f * Time.deltaTime
            );
        }
    }

    // ------------------------------------------
    // ABILITY USE & LOGISTIC FUNCTIONS
    // ------------------------------------------
    private void UseAbility(int index)
    {
        if (_abilityRefreshedFlag[index] && abilityData[index].cooldownFlag)
        {
            _abilityRefreshedFlag[index] = false;
            _cooldownTimer[index] = abilityData[index].cooldown;
            StartCoroutine(abilities[index].GetComponent<AbilityBehaviour>().SpawnAbility(_currentPosition, _hitPoint));
        }

        if (_abilityRefreshedFlag[index] && !(abilityData[index].cooldownFlag))
        {
            _abilityRefreshedFlag[index] = false;
            _cooldownTimer[index] = 1 / abilityData[index].fireRate;
            StartCoroutine(abilities[index].GetComponent<AbilityBehaviour>().SpawnAbility(_currentPosition, _hitPoint));
        }
    }

    private void AbilityRefreshTimer(int index)
    {
        if (!_abilityRefreshedFlag[index])
        {
            _cooldownTimer[index] -= Time.deltaTime;

            if (_cooldownTimer[index] < 0)
            {
                _cooldownTimer[index] = 0;
            }

            if (_cooldownTimer[index] <= 0)
            {
                _abilityRefreshedFlag[index] = true;
            }
        }
    }

    private void SetUpAbilityInstance(int index)
    {
        if (abilitiesPrefab == null || index < 0 || index >= abilitiesPrefab.Length || abilitiesPrefab[index] == null)
        {
            Debug.LogWarning($"EnemyAI.SetUpAbilityInstance: abilitiesPrefab[{index}] missing on '{gameObject.name}', skipping.");
            return;
        }

        if (abilitiesInstancesHolder == null)
            abilitiesInstancesHolder = GameObject.FindGameObjectWithTag("AbilitiesInstancesHolder");

        GameObject abilityInstance = Instantiate(abilitiesPrefab[index]);
        if (abilityInstance == null)
        {
            Debug.LogWarning($"EnemyAI.SetUpAbilityInstance: failed to create instance for abilitiesPrefab[{index}] on '{gameObject.name}'.");
            return;
        }

        if (index >= abilities.Count) abilities.Add(abilityInstance);
        else abilities[index] = abilityInstance;

        if (abilities[index] != null && abilities[index].GetComponent<AbilityBehaviour>() != null)
        {
            var ab = abilities[index].GetComponent<AbilityBehaviour>();
            ab.user = gameObject;
            ab.validTargetTag = "Player";
            ab.isTemplate = true;
            ab.CalculateAbilityDamage();
        }

        abilities[index].SetActive(false);

        if (abilitiesInstancesHolder != null)
            abilities[index].transform.SetParent(abilitiesInstancesHolder.transform, false);
    }
}
