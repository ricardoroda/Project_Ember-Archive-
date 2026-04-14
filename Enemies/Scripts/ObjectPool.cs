using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Simple GameObject pool. Create one per prefab type. Thread-affine to Unity main thread.
/// Not a generic collection; keeps API minimal for games.
/// </summary>
public class ObjectPool
{
    private readonly GameObject prefab;
    private readonly Transform parent;
    private readonly Stack<GameObject> pool = new Stack<GameObject>();

    public int CountInactive => pool.Count;

    public ObjectPool(GameObject prefab, Transform parent = null)
    {
        if (prefab == null) throw new System.ArgumentNullException(nameof(prefab));

        this.prefab = prefab;
        this.parent = parent;
    }

    /// <summary>
    /// Pre-warm the pool with n instances (deactivated).
    /// </summary>
    public void Initialize(int initialSize)
    {
        for (int i = 0; i < initialSize; i++)
        {
            var instance = CreateInstance();
            instance.SetActive(false);
            pool.Push(instance);
        }
    }

    private GameObject CreateInstance()
    {
        var instance = Object.Instantiate(prefab, parent);
        // Attach a PoolReturner when missing so instances can be returned safely.
        if (instance.GetComponent<PoolReturner>() == null)
            instance.AddComponent<PoolReturner>();
        return instance;
    }

    /// <summary>
    /// Get an instance from the pool (activated). If pool empty, creates a new one.
    /// </summary>
    public GameObject Get(Vector3 position, Quaternion rotation)
    {
        GameObject instance = pool.Count > 0 ? pool.Pop() : CreateInstance();
        instance.transform.SetParent(parent, worldPositionStays: true);
        instance.transform.SetPositionAndRotation(position, rotation);
        instance.SetActive(true);
        // link returner to this pool so it can return itself
        var returnerComponent = instance.GetComponent<PoolReturner>();
        if (returnerComponent != null) returnerComponent.SetPool(this);
        return instance;
    }

    /// <summary>
    /// Release an instance back into the pool (deactivated).
    /// </summary>
    public void Release(GameObject instance)
    {
        if (instance == null) return;

        // Keep pooled instances organized under the pool parent in the hierarchy
        if (parent != null)
            instance.transform.SetParent(parent, worldPositionStays: false);

        instance.SetActive(false);
        pool.Push(instance);
    }
}

/// <summary>
/// Helper component added to pooled instances so they can return themselves to a pool.
/// </summary>
public class PoolReturner : MonoBehaviour
{
    private ObjectPool pool;

    public void SetPool(ObjectPool pool)
    {
        this.pool = pool;
    }

    /// <summary>
    /// Return this GameObject to its pool.
    /// </summary>
    public void ReturnToPool()
    {
        // Minimal cleanup to make pooled instances safe for reuse
        try
        {
            // Stop all coroutines running on this behaviour (and children) by disabling/enabling
            // Note: Unity has no API to stop coroutines across all components; clear common states instead.
            foreach (var mb in GetComponentsInChildren<MonoBehaviour>(true))
            {
                // Try to stop coroutines if the component exposes StopAllCoroutines
                try { mb.StopAllCoroutines(); } catch { }
            }

            // Zero out rigidbody velocities
            // Clear velocities on 3D rigidbodies (use pragmas because some Unity versions mark velocity obsolete)
#pragma warning disable CS0618
            foreach (var rb in GetComponentsInChildren<Rigidbody>(true))
            {
                try { rb.velocity = Vector3.zero; rb.angularVelocity = Vector3.zero; } catch { }
            }
#pragma warning restore CS0618

            // Clear velocities on 2D rigidbodies (some Unity versions mark these obsolete)
#pragma warning disable CS0618
            foreach (var rb2 in GetComponentsInChildren<Rigidbody2D>(true))
            {
                try { rb2.velocity = Vector2.zero; rb2.angularVelocity = 0f; } catch { }
            }
#pragma warning restore CS0618

            // Stop and clear particle systems
            foreach (var ps in GetComponentsInChildren<ParticleSystem>(true))
            {
                try
                {
                    ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                    ps.Clear(true);
                }
                catch { }
            }

            // Reset animator states where applicable
            foreach (var animator in GetComponentsInChildren<Animator>(true))
            {
                try { animator.Rebind(); animator.Update(0f); } catch { }
            }
        }
        catch { }

        pool?.Release(gameObject);
    }
}
