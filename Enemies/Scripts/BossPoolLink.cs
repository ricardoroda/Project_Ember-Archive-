using UnityEngine;

/// <summary>
/// Convenience component for boss prefabs so boss code can return to pool without knowing pool internals.
/// It forwards to PoolReturner if present.
/// </summary>
public class BossPoolLink : MonoBehaviour
{
    private PoolReturner poolReturnerComponent;

    private void Awake()
    {
        poolReturnerComponent = GetComponent<PoolReturner>();
    }

    /// <summary>
    /// Return this boss instance to its configured pool (if any). If no pool exists, destroys.
    /// </summary>
    public void ReturnToPoolOrDestroy()
    {
        if (poolReturnerComponent != null)
        {
            poolReturnerComponent.ReturnToPool();
            return;
        }
        Destroy(gameObject);
    }
}
