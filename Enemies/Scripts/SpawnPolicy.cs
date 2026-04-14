using UnityEngine;

/// <summary>
/// Small, testable policy object to decide whether a spawn should occur.
/// Keeps business rules out of MonoBehaviours.
/// </summary>
public static class SpawnPolicy
{
    public static bool CanSpawn(int currentActive, int maxSimultaneous, float lastSpawnTime, float cooldown, float now)
    {
        if (currentActive >= maxSimultaneous) return false;
        if (now < lastSpawnTime + cooldown) return false;
        return true;
    }
}
