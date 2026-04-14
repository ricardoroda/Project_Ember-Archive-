using UnityEngine;

[CreateAssetMenu(fileName = "BossConfig", menuName = "Scriptable Objects/BossConfig")]
public class BossConfig : ScriptableObject
{
	[Header("Prefab & Pooling")]
	[Tooltip("Prefab used for the boss. Can contain a BossPoolLink component to return to pool.")]
	public GameObject bossPrefab;

	[Tooltip("Use an internal object pool for this boss (recommended for performance)")]
	public bool usePooling = true;

	[Tooltip("Initial pool size when prewarming. Will grow if needed.")]
	[Min(0)]
	public int initialPoolSize = 2;

	[Header("Spawn Rules")]
	[Tooltip("Minimum time in seconds between spawns from a single spawner using this config.")]
	[Min(0f)]
	public float spawnCooldown = 10f;

	[Tooltip("Delay in seconds before the boss is actually spawned after TrySpawn is called.")]
	[Min(0f)]
	public float preSpawnDelay = 0f;

	[Tooltip("Maximum simultaneous active bosses from a single spawner using this config.")]
	[Min(1)]
	public int maxSimultaneous = 1;

	[Header("Optional Metadata")]
	public string displayName;
	public Sprite icon;

	[Header("Companion Enemies")]
	[Tooltip("Optional regular enemy prefab to spawn alongside the boss.")]
	public GameObject regularEnemyPrefab;

	[Tooltip("How many regular enemies to spawn with the boss (always spawns this many).")]
	[Min(0)]
	public int regularEnemyCount = 2;

	/// <summary>
	/// Validate basic config at runtime.
	/// </summary>
	public bool IsValid(out string reason)
	{
		if (bossPrefab == null)
		{
			reason = "bossPrefab is null";
			return false;
		}

		if (maxSimultaneous < 1)
		{
			reason = "maxSimultaneous must be at least 1";
			return false;
		}

		if (spawnCooldown < 0f)
		{
			reason = "spawnCooldown cannot be negative";
			return false;
		}

		reason = null;
		return true;
	}
}
