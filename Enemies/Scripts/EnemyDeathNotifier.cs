using System;
using UnityEngine;

/// <summary>
/// Notifies when the attached GameObject is destroyed (used to detect death/despawn).
/// The spawner attaches this to spawned enemies so it can release them from pools or decrement counts.
/// </summary>
public class EnemyDeathNotifier : MonoBehaviour
{
    public Action<GameObject> OnDeath;

    private void OnDestroy()
    {
        try
        {
            OnDeath?.Invoke(gameObject);
        }
        catch (Exception)
        {
            // swallow exceptions to avoid destroying the GameObject failing due to event handlers
        }
    }
}
