using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.AI;

[DisallowMultipleComponent]
public class BossSpawner : MonoBehaviour
{
	[Header("Config")]
	[SerializeField] private BossConfig config;

	[Header("Debug")]
	[Tooltip("When enabled the spawner will emit detailed logs about spawn decisions.")]
	[SerializeField] private bool verboseLogging = true;

	[Tooltip("Optional scene tag to prefer when picking spawn points (defaults to 'BossNest').")]
	[SerializeField] private string preferredSpawnTag = "BossNest";

	[Tooltip("Optional explicit spawn points. If empty the spawner's transform will be used.")]
	[SerializeField] private Transform[] spawnPoints;

	[Header("Auto Spawn")]
	[Tooltip("If true the spawner will attempt to spawn when the scene starts (after an optional delay).")]
	[SerializeField] private bool spawnOnStart = true;

	[Tooltip("Delay in seconds after scene start before attempting the initial spawn (useful to let other objects initialize).")]
	[SerializeField] private float spawnOnStartDelay = 0.5f;

	[Tooltip("If true the spawner will search inactive scene objects for the preferred spawn tag. Use only if your BossNest objects start inactive.")]
	[SerializeField] private bool includeInactiveSpawnPoints = true;

	public event Action<GameObject> OnBossSpawned;

	// pending spawns waiting to complete
	private int pendingSpawns = 0;

	// Editor helpers
	public int ActiveBossCount => activeBosses.Count;
	public int InactivePoolCount => pool != null ? pool.CountInactive : 0;

	/// <summary>
	/// Returns a spawn point chosen by our logic.
	/// </summary>
	public Transform GetSpawnPoint()
	{
		return ChooseSpawnPoint();
	}

	private ObjectPool pool;
	private readonly HashSet<GameObject> activeBosses = new HashSet<GameObject>();
	// event cleanup map
	private readonly Dictionary<GameObject, System.Action> deathSubscriptions = new Dictionary<GameObject, System.Action>();
	private float lastSpawnTime = -Mathf.Infinity;

	private void Awake()
	{
		if (config != null && config.usePooling && config.bossPrefab != null)
		{
			pool = new ObjectPool(config.bossPrefab, parent: transform);
			if (config.initialPoolSize > 0)
				pool.Initialize(config.initialPoolSize);
		}
	}

	private void Start()
	{
		if (spawnOnStart)
		{
			if (spawnOnStartDelay <= 0f)
			{
				TrySpawn();
			}
			else
			{
				StartCoroutine(SpawnOnStartCoroutine());
			}
		}
	}

	private IEnumerator SpawnOnStartCoroutine()
	{
		yield return new WaitForSeconds(spawnOnStartDelay);
		// try spawning after a short delay
		TrySpawn();
	}

	/// <summary>
	/// Try to spawn a boss if rules allow.
	/// </summary>
	public bool TrySpawn()
	{
		if (config == null)
		{
			Debug.LogWarning("BossSpawner: no BossConfig assigned.");
			return false;
		}

		if (!config.IsValid(out var reason))
		{
			Debug.LogWarning($"BossSpawner: invalid BossConfig - {reason}");
			return false;
		}

		// Use policy helper so rules are in one spot and testable
		// Count pending spawns so we don't go over the limit
		if (!SpawnPolicy.CanSpawn(activeBosses.Count + pendingSpawns, config.maxSimultaneous, lastSpawnTime, config.spawnCooldown, Time.time))
		{
			if (verboseLogging)
			{
				Debug.LogFormat(this, "BossSpawner: CanSpawn denied. active={0} pending={1} max={2} lastSpawn={3} cooldown={4} now={5}",
					activeBosses.Count, pendingSpawns, config.maxSimultaneous, lastSpawnTime, config.spawnCooldown, Time.time);
			}
			return false;
		}

		// Reserve a pending slot and start the spawn (maybe delayed)
		pendingSpawns++;

		var spawnPoint = ChooseSpawnPoint();
		if (verboseLogging)
		{
			if (spawnPoint == null)
				Debug.LogFormat(this, "BossSpawner: ChooseSpawnPoint returned null (will not spawn)");
			else
				Debug.LogFormat(this, "BossSpawner: spawning at {0} (pos={1})", spawnPoint.name, spawnPoint.position);
		}
		if (spawnPoint == null)
		{
			pendingSpawns = Math.Max(0, pendingSpawns - 1);
			return false;
		}

		// Diagnostic: if we fell back to this spawner's transform because no tag or explicit points exist, warn so designers can fix scene setup
		if (spawnPoint == transform && (spawnPoints == null || spawnPoints.Length == 0))
		{
			Debug.LogWarningFormat(this, "BossSpawner: No spawn points found for tag '{0}' and no explicit spawnPoints set — falling back to spawner transform. Ensure a GameObject with tag '{0}' exists in the scene.", preferredSpawnTag);
		}

		if (config.preSpawnDelay > 0f)
		{
			StartCoroutine(DelayedSpawnCoroutine(config.preSpawnDelay, spawnPoint));
			return true;
		}

		// Spawn right away
		return PerformSpawnAt(spawnPoint);
	}

	private IEnumerator DelayedSpawnCoroutine(float delay, Transform spawnPoint)
	{
		yield return new WaitForSeconds(delay);
		// If this spawner or config got destroyed, just bail and free the slot
		if (this == null || config == null)
		{
			pendingSpawns = Math.Max(0, pendingSpawns - 1);
			yield break;
		}
		PerformSpawnAt(spawnPoint);
	}

	private bool PerformSpawnAt(Transform spawnPoint)
	{
		GameObject boss;
		if (config.usePooling && pool != null)
		{
			boss = pool.Get(spawnPoint.position, spawnPoint.rotation);
		}
		else
		{
			boss = Instantiate(config.bossPrefab, spawnPoint.position, spawnPoint.rotation, transform);
		}

		if (boss == null)
		{
			Debug.LogError("BossSpawner: Instantiate failed.");
			pendingSpawns = Math.Max(0, pendingSpawns - 1);
			return false;
		}

		lastSpawnTime = Time.time;
		// If this was a pending spawn, clear the slot
		pendingSpawns = Math.Max(0, pendingSpawns - 1);
		activeBosses.Add(boss);

		// If the boss has a PoolReturner, hook it up to our pool
		if (pool != null && boss.TryGetComponent<PoolReturner>(out var poolReturner))
			poolReturner.SetPool(pool);

		// Hook into the boss's death event, whatever type it uses
		if (boss.TryGetComponent<EnemyStats>(out var enemyStats))
		{
			// let EnemyStats know about pooling and call its spawn hook
			enemyStats.poolingEnabled = config.usePooling;
			enemyStats.OnSpawned();

			// remember delegate so we can unsubscribe
			System.Action onDeath = () => HandleBossDeath(boss);
			enemyStats.OnDeath += onDeath;
			deathSubscriptions[boss] = onDeath;
		}
		else if (boss.TryGetComponent<IBoss>(out var iboss))
		{
			System.Action onDeath = () => HandleBossDeath(boss);
			iboss.OnDeath += onDeath;
			deathSubscriptions[boss] = onDeath;
		}
		else if (boss.TryGetComponent<Health>(out var health))
		{
			System.Action onDeath = () => HandleBossDeath(boss);
			health.OnDie += onDeath;
			deathSubscriptions[boss] = onDeath;
		}
		else
		{
			// fallback notifier attached to the boss
			var notifier = boss.AddComponent<EnemyDeathNotifier>();
			System.Action<GameObject> onDeathObj = (g) => HandleBossDeath(g);
			notifier.OnDeath += onDeathObj;
			// cleanup action
			deathSubscriptions[boss] = () => notifier.OnDeath -= onDeathObj;
		}

		// Spawn regular enemies with the boss if set up
		if (config.regularEnemyPrefab != null && config.regularEnemyCount > 0)
		{
			for (int i = 0; i < config.regularEnemyCount; i++)
			{
				Vector3 offset = UnityEngine.Random.insideUnitSphere * 1.5f;
				offset.y = 0f; // keep on ground
				var pos = spawnPoint.position + offset;
				var reg = Instantiate(config.regularEnemyPrefab, pos, config.regularEnemyPrefab.transform.rotation, transform);
				// Ensure the spawned regular enemy starts idle
				ResetInstanceToIdle(reg);
			}
		}

		// Ensure the boss instance is idle (stop movement/zero anim params)
		ResetInstanceToIdle(boss);

		OnBossSpawned?.Invoke(boss);
		return true;
	}

	/// <summary>
	/// Try to put a newly spawned instance in a clean idle state.
	/// Stops NavMeshAgent, clears Rigidbody velocity, and zeros animation inputs.
	/// Safe for pooled or freshly instantiated objects.
	/// </summary>
	private void ResetInstanceToIdle(GameObject instance)
	{
		if (instance == null) return;

		// Stop all NavMeshAgents on the instance and its children
		var agents = instance.GetComponentsInChildren<NavMeshAgent>(true);
		foreach (var agentComp in agents)
		{
			if (agentComp == null) continue;
			try
			{
				agentComp.ResetPath();
				agentComp.isStopped = true;
			}
			catch { }
		}

		// Zero rigidbody velocities if present
		var rbs = instance.GetComponentsInChildren<Rigidbody>(true);
		foreach (var rb in rbs)
		{
			if (rb == null) continue;
			rb.linearVelocity = Vector3.zero;
			rb.angularVelocity = Vector3.zero;
		}

		// Clear any AnimationHandler inputs on the instance and children
		var animHandlers = instance.GetComponentsInChildren<AnimationHandler>(true);
		bool hadAnimHandler = false;
		foreach (var ah in animHandlers)
		{
			hadAnimHandler = true;
			if (ah == null) continue;
			try
			{
				ah.SetMoveInput(Vector2.zero);
				ah.useInput = false; // ensure AI mode
				// also try to zero animator params if Animator present
				var animator = ah.GetComponent<Animator>();
				if (animator != null)
				{
					if (!string.IsNullOrEmpty(ah.moveXParameter)) animator.SetFloat(ah.moveXParameter, 0f);
					if (!string.IsNullOrEmpty(ah.moveYParameter)) animator.SetFloat(ah.moveYParameter, 0f);
					if (!string.IsNullOrEmpty(ah.speedParameter)) animator.SetFloat(ah.speedParameter, 0f);
				}
			}
			catch { }
		}

		// Fallback: if no AnimationHandler found, zero common animator params directly
		if (!hadAnimHandler)
		{
			var animators = instance.GetComponentsInChildren<Animator>(true);
			foreach (var animator in animators)
			{
				if (animator == null) continue;
				try
				{
					if (AnimatorHasParameter(animator, "Speed")) animator.SetFloat("Speed", 0f);
					if (AnimatorHasParameter(animator, "MoveX")) animator.SetFloat("MoveX", 0f);
					if (AnimatorHasParameter(animator, "MoveY")) animator.SetFloat("MoveY", 0f);
				}
				catch { }
			}
		}
	}

	// Helper: check whether an Animator has a parameter with the given name
	private static bool AnimatorHasParameter(Animator animator, string paramName)
	{
		if (animator == null || string.IsNullOrEmpty(paramName)) return false;
		var ps = animator.parameters;
		for (int i = 0; i < ps.Length; i++)
		{
			if (ps[i].name == paramName) return true;
		}
		return false;
	}

	private Transform ChooseSpawnPoint()
	{
		// try tag first
		GameObject[] tagged = null;
		try
		{
			tagged = GameObject.FindGameObjectsWithTag(preferredSpawnTag);
		}
		catch (UnityException ex)
		{
			// Tag may not exist in TagManager - tell the user when verbose logging is enabled
			if (verboseLogging)
				Debug.LogWarningFormat(this, "BossSpawner: requested tag '{0}' not found in Tag Manager: {1}", preferredSpawnTag, ex.Message);
		}

		if (tagged != null && tagged.Length > 0)
		{
			if (verboseLogging)
				Debug.LogFormat(this, "BossSpawner: found {0} spawn points with tag '{1}'", tagged.Length, preferredSpawnTag);
			var pick = tagged[UnityEngine.Random.Range(0, tagged.Length)];
			if (pick != null) return pick.transform;
		}
		else
		{
			if (verboseLogging)
				Debug.LogFormat(this, "BossSpawner: no scene objects found with tag '{0}'", preferredSpawnTag);
		}

		// If we have explicit spawn points, pick one
		if (spawnPoints != null && spawnPoints.Length > 0)
		{
			// Filter out null entries (serialised empty slots can appear as fileID: 0 in scenes)
			int validCount = 0;
			for (int i = 0; i < spawnPoints.Length; i++)
			{
				if (spawnPoints[i] != null) validCount++;
			}

			if (validCount > 0)
			{
				if (verboseLogging)
					Debug.LogFormat(this, "BossSpawner: using {0} explicit spawn points (ignoring {1} null entries)", validCount, spawnPoints.Length - validCount);

				// Pick a random non-null entry without LINQ
				int pick = UnityEngine.Random.Range(0, validCount);
				for (int i = 0; i < spawnPoints.Length; i++)
				{
					if (spawnPoints[i] == null) continue;
					if (pick == 0) return spawnPoints[i];
					pick--;
				}
			}
			else
			{
				// All explicit slots were null; fall through to tag/name search instead of returning a null
				if (verboseLogging)
					Debug.LogWarningFormat(this, "BossSpawner: explicit spawnPoints contains only null entries — falling back to tag/name search");
			}
		}

		// As a convenience/fallback, search active scene GameObjects by name (useful if tag wasn't set)
		var activeNameMatch = FindActiveSceneObjectByName(preferredSpawnTag);
		if (activeNameMatch != null)
		{
			if (verboseLogging) Debug.LogFormat(this, "BossSpawner: found active GameObject named '{0}' and using it as spawn point", preferredSpawnTag);
			return activeNameMatch;
		}

		// Optionally, if we didn't find any active tagged objects, search inactive scene objects (more expensive)
		if (includeInactiveSpawnPoints && (tagged == null || tagged.Length == 0))
		{
			try
			{
				var allTransforms = Resources.FindObjectsOfTypeAll<Transform>();
				var candidates = new List<Transform>(allTransforms.Length);
				for (int i = 0; i < allTransforms.Length; i++)
				{
					var t = allTransforms[i];
					var go = t.gameObject;
					// Only consider scene objects (not prefabs/assets) and no hidden objects
					if (!go.scene.IsValid()) continue;
					if ((go.hideFlags & HideFlags.HideInHierarchy) != 0) continue;
					// Match either by tag or by name to be robust when designers forgot to tag objects
					bool matched = false;
					if (t.name == preferredSpawnTag) matched = true;
					else
					{
						try
						{
							if (go.CompareTag(preferredSpawnTag)) matched = true;
						}
						catch (UnityException ex)
						{
							if (verboseLogging) Debug.LogWarningFormat(this, "BossSpawner: CompareTag failed for tag '{0}': {1}", preferredSpawnTag, ex.Message);
						}
					}
					if (matched) candidates.Add(t);
				}
				if (candidates.Count > 0)
				{
					if (verboseLogging) Debug.LogFormat(this, "BossSpawner: found {0} (including inactive) spawn points with tag '{1}'", candidates.Count, preferredSpawnTag);
					return candidates[UnityEngine.Random.Range(0, candidates.Count)];
				}
			}
			catch (Exception ex)
			{
				if (verboseLogging) Debug.LogWarningFormat(this, "BossSpawner: error searching inactive objects for tag '{0}': {1}", preferredSpawnTag, ex.Message);
			}
		}

		// Otherwise just use this object's transform
		if (verboseLogging)
			Debug.LogFormat(this, "BossSpawner: falling back to spawner transform ({0}) as spawn point", name);
		return transform;
	}

	// Recursive helper: scans active scene root objects for a GameObject with exact name match (case-sensitive)
	private Transform FindActiveSceneObjectByName(string nameToFind)
	{
		var scene = SceneManager.GetActiveScene();
		if (!scene.IsValid()) return null;
		var roots = scene.GetRootGameObjects();
		for (int i = 0; i < roots.Length; i++)
		{
			var found = FindInHierarchyByName(roots[i].transform, nameToFind);
			if (found != null) return found;
		}
		return null;
	}

	private Transform FindInHierarchyByName(Transform root, string nameToFind)
	{
		if (root.name == nameToFind) return root;
		for (int i = 0; i < root.childCount; i++)
		{
			var c = root.GetChild(i);
			var res = FindInHierarchyByName(c, nameToFind);
			if (res != null) return res;
		}
		return null;
	}

	private void HandleBossDeath(GameObject boss)
	{
		if (boss == null) return;

		if (!activeBosses.Remove(boss))
			return;

		// remove event subscriptions
		if (deathSubscriptions.TryGetValue(boss, out var storedDelegate))
		{
			if (boss.TryGetComponent<EnemyStats>(out var enemyStatsComponent))
			{
				enemyStatsComponent.OnDeath -= storedDelegate;
			}
			else if (boss.TryGetComponent<IBoss>(out var ibossComponent))
			{
				ibossComponent.OnDeath -= storedDelegate;
			}
			else if (boss.TryGetComponent<Health>(out var healthComponent))
			{
				healthComponent.OnDie -= storedDelegate;
			}
			else
			{
				// fallback cleanup
				try
				{
					storedDelegate?.Invoke();
				}
				catch (Exception ex)
				{
					if (verboseLogging) Debug.LogWarningFormat(this, "BossSpawner: error invoking cleanup delegate for boss {0}: {1}", boss.name, ex.Message);
				}
			}
			deathSubscriptions.Remove(boss);
		}

		// return to pool or destroy
		if (config != null && config.usePooling && pool != null)
		{
			if (boss.TryGetComponent<EnemyStats>(out var enemyStatsComponent2))
				enemyStatsComponent2.OnDespawned();
			pool.Release(boss);
		}
		else
		{
			Destroy(boss);
		}
	}

	/// <summary>
	/// Release all active bosses (cleanup).
	/// </summary>
	public void ReleaseAll()
	{
		var items = new List<GameObject>(activeBosses);
		foreach (var instance in items)
		{
			if (instance == null) continue;

			// unsubscribe
			if (deathSubscriptions.TryGetValue(instance, out var storedDelegate))
			{
				if (instance.TryGetComponent<EnemyStats>(out var enemyStatsComp))
				{
					enemyStatsComp.OnDeath -= storedDelegate;
				}
				else if (instance.TryGetComponent<IBoss>(out var ibossComp))
				{
					ibossComp.OnDeath -= storedDelegate;
				}
				else if (instance.TryGetComponent<Health>(out var healthComp))
				{
					healthComp.OnDie -= storedDelegate;
				}
				else
				{
					try
					{
						storedDelegate?.Invoke();
					}
					catch (Exception ex)
					{
						if (verboseLogging) Debug.LogWarningFormat(this, "BossSpawner: error invoking cleanup delegate for instance {0}: {1}", instance.name, ex.Message);
					}
				}
				deathSubscriptions.Remove(instance);
			}

			if (config != null && config.usePooling && pool != null)
			{
				if (instance.TryGetComponent<EnemyStats>(out var enemyStatsComp2))
					enemyStatsComp2.OnDespawned();
				pool.Release(instance);
			}
			else
			{
				if (instance != null) Destroy(instance);
			}
		}
		activeBosses.Clear();
	}

	private void OnDestroy()
	{
		// cleanup
		ReleaseAll();
		deathSubscriptions.Clear();
	}
}

/// <summary>
/// Optional interface for a Boss MonoBehaviour to notify death.
/// Keeps BossSpawner decoupled from boss details.
/// </summary>
public interface IBoss
{
	event Action OnDeath;
}

// Minimal Health placeholder for BossSpawner. If your project already has a Health
// component with OnDie, this is just a safe reference. Remove if not needed.
public class Health : MonoBehaviour
{
	public event Action OnDie;

	// Example method to call when dying
	public void Die()
	{
		OnDie?.Invoke();
	}
}
