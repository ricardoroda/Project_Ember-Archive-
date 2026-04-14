using UnityEngine;

/// <summary>
/// Small debug helper you can attach to a Spawner in the scene. Press Space to TrySpawn.
/// Displays basic pool and active counts in the debug log. Non-invasive: does not change any runtime state.
/// </summary>
public class BossSpawnerDebugger : MonoBehaviour
{
    public BossSpawner spawner;
    public KeyCode spawnKey = KeyCode.Space;
    public bool autoSpawn = false;
    public float autoSpawnInterval = 5f;

    private float nextAuto;

    void Reset()
    {
        spawner = GetComponent<BossSpawner>();
    }

    void Update()
    {
        if (spawner == null) return;

        if (Input.GetKeyDown(spawnKey))
        {
            TryAndReport();
        }

        if (autoSpawn && Time.time >= nextAuto)
        {
            nextAuto = Time.time + autoSpawnInterval;
            TryAndReport();
        }
    }

    private void TryAndReport()
    {
        bool ok = spawner.TrySpawn();
        Debug.Log($"BossSpawnerDebugger: TrySpawn returned {ok}");
    }
}
