using UnityEngine;
using System;
#if UNITY_EDITOR
using UnityEditorInternal;
#endif

public class CameraController : MonoBehaviour
{
    [SerializeField] private Vector3 offset = new Vector3(-10, 20, -20); 
    [SerializeField] private float followSpeed = 10f; 

    [Header("Occlusion")]
    [SerializeField] private bool avoidOcclusion = true; 
    [SerializeField] private float occlusionRadius = 0.25f; // spherecast radius
    [SerializeField] private float targetHeight = 1.0f; // height offset for occlusion cast
    [SerializeField] private LayerMask occlusionLayers = ~0; // layers to check for occlusion
    [Header("Fade-through")]
    [SerializeField] private float fadeAlpha = 0.25f; // target alpha for faded objects
    [SerializeField] private float fadeSpeed = 6f; // how quickly objects fade/unfade
    [SerializeField] private string[] excludeTags = new string[] { "Player", "UI" };
    [SerializeField] private int maxConcurrentFades = 12;

    [Header("Bounds")]
    [SerializeField] private BoxCollider bounds; 

    [Header("Dead Zone")]
    [SerializeField] private float deadZoneRadius; // radius of dead zone around target

    [SerializeField] private string targetTag = "Player";
    private Transform target;

    [Header("Debug")]
    [SerializeField] private bool debugDraw = false;

    // non-alloc buffer for spherecasts
    private const int SphereCastBufferSize = 64;
    private readonly RaycastHit[] _sphereCastBuffer = new RaycastHit[SphereCastBufferSize];

    // debug info from the last cast
    private Vector3 lastCastOrigin;
    private Vector3 lastCastDir;
    private float lastCastDist;
    private RaycastHit[] lastCastHits = new RaycastHit[0];

    private float baseZoomDistance; 
    // faded renderers -> original colors
    private readonly System.Collections.Generic.Dictionary<Renderer, Color[]> originalColors = new System.Collections.Generic.Dictionary<Renderer, Color[]>();
    private readonly System.Collections.Generic.Dictionary<Renderer, UnityEngine.Coroutine> fadeCoroutines = new System.Collections.Generic.Dictionary<Renderer, UnityEngine.Coroutine>();
    // material property blocks for fades
    private readonly System.Collections.Generic.Dictionary<Renderer, UnityEngine.MaterialPropertyBlock> propBlocks = new System.Collections.Generic.Dictionary<Renderer, UnityEngine.MaterialPropertyBlock>();

    // shader property IDs (cached for performance)
    private static readonly int PropColor = Shader.PropertyToID("_Color");
    private static readonly int PropMode = Shader.PropertyToID("_Mode");
    private static readonly int PropSrcBlend = Shader.PropertyToID("_SrcBlend");
    private static readonly int PropDstBlend = Shader.PropertyToID("_DstBlend");
    private static readonly int PropZWrite = Shader.PropertyToID("_ZWrite");

    private void Start()
    {
        GameObject targetObj = GameObject.FindWithTag(targetTag);
        if (targetObj != null)
        {
            target = targetObj.transform;
        }
        else
        {
            Debug.LogWarning($"CameraController: No GameObject found with tag '{targetTag}'");
        }

        baseZoomDistance = offset.magnitude;
    }

    private void OnDrawGizmos()
    {
        if (!debugDraw) return;

        // draw spherecast path
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(lastCastOrigin, occlusionRadius);
        Gizmos.DrawLine(lastCastOrigin, lastCastOrigin + lastCastDir * lastCastDist);

        // draw hits
        if (lastCastHits != null)
        {
            Gizmos.color = Color.red;
            foreach (var h in lastCastHits)
            {
                Gizmos.DrawSphere(h.point, Mathf.Max(0.05f, occlusionRadius * 0.3f));
                Gizmos.DrawLine(h.point, h.point + h.normal * 0.25f);
            }
        }

        // highlight faded renderers
        Gizmos.color = new Color(1f, 1f, 0f, 0.5f);
        foreach (var kv in originalColors)
        {
            var rend = kv.Key;
            if (rend == null) continue;
            try
            {
                // renamed local variable to avoid hiding serialized field 'bounds'
                var rendBounds = rend.bounds;
                Gizmos.DrawWireCube(rendBounds.center, rendBounds.size);
            }
            catch (Exception ex)
            {
                if (debugDraw)
                    Debug.LogWarning($"CameraController: failed drawing gizmo for renderer {rend} - {ex.Message}");
            }
        }
    }

    private void LateUpdate()
    {
        MoveCamera();
    }

    private void MoveCamera()
    {
        if (target == null) return;
        Vector3 focus = target.position;

        // calc camera pos based on offset
        Vector3 desiredOffset = offset.normalized * Mathf.Max(0.01f, baseZoomDistance);
        Vector3 finalPos = focus + desiredOffset;

        // only move camera if target leaves the dead zone
        Vector3 cameraToTarget = focus - (transform.position - desiredOffset);
        float distance = cameraToTarget.magnitude;
        if (deadZoneRadius > 0f && distance < deadZoneRadius)
        {
            finalPos = transform.position;
        }
        else
        {
            finalPos = Vector3.Lerp(transform.position, finalPos, followSpeed * Time.deltaTime);
        }

        // back off camera if something blocks the view
        if (avoidOcclusion)
        {
            Vector3 castOrigin = target.position + Vector3.up * targetHeight;
            Vector3 toCam = finalPos - castOrigin;
            float dist = toCam.magnitude;
            if (dist > 0.001f)
            {
                Vector3 dir = toCam / dist;
                // Instead of moving the camera back, fade any objects between the target and camera so the player remains visible.
                HandleOcclusionFading(castOrigin, dir, dist);

                // store debug info
                lastCastOrigin = castOrigin;
                lastCastDir = dir;
                lastCastDist = dist;
                if (debugDraw)
                {
                    // non-alloc spherecast into the preallocated buffer
                    int hitCount = Physics.SphereCastNonAlloc(castOrigin, occlusionRadius, dir, _sphereCastBuffer, dist, occlusionLayers, QueryTriggerInteraction.Ignore);
                    if (hitCount > SphereCastBufferSize)
                    {
                        // unlikely but warn in debug
                        Debug.LogWarning("CameraController: sphere cast hit buffer overflowed; consider increasing SphereCastBufferSize");
                        hitCount = SphereCastBufferSize;
                    }
                    // copy to lastCastHits for drawing
                    lastCastHits = new RaycastHit[hitCount];
                    Array.Copy(_sphereCastBuffer, lastCastHits, hitCount);
                }
            }
        }

        // make sure camera stays within bounds
        if (bounds != null)
        {
            Bounds b = bounds.bounds;
            finalPos.x = Mathf.Clamp(finalPos.x, b.min.x, b.max.x);
            finalPos.z = Mathf.Clamp(finalPos.z, b.min.z, b.max.z);
        }

        transform.position = finalPos;
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }

    private void HandleOcclusionFading(Vector3 origin, Vector3 dir, float maxDist)
    {
    // find all hits between origin and camera (non-alloc)
    int hitCount = Physics.SphereCastNonAlloc(origin, occlusionRadius, dir, _sphereCastBuffer, maxDist, occlusionLayers, QueryTriggerInteraction.Ignore);

        // mark all currently hit renderers to be faded
        var hitRenderers = new System.Collections.Generic.HashSet<Renderer>();
        for (int hitIndex = 0; hitIndex < hitCount; hitIndex++)
        {
            var hit = _sphereCastBuffer[hitIndex];
            var hitGo = hit.collider.gameObject;

            // Collect candidate renderers from the collider's children and parents.
            var candidates = new System.Collections.Generic.List<Renderer>();
            candidates.AddRange(hitGo.GetComponentsInChildren<Renderer>(true));

            var parentRenderer = hit.collider.GetComponentInParent<Renderer>();
            if (parentRenderer != null && !candidates.Contains(parentRenderer))
                candidates.Add(parentRenderer);

            // If still nothing, try the root of the hierarchy (useful for large building prefabs)
            if (candidates.Count == 0)
            {
                var root = hitGo.transform.root;
                candidates.AddRange(root.GetComponentsInChildren<Renderer>(true));
            }

            // For each candidate renderer, check its proximity to the hit point and decide whether to fade it.
            foreach (var rend in candidates)
            {
                if (rend == null) continue;
                // don't fade particle systems or UI
                if (rend is UnityEngine.ParticleSystemRenderer) continue;

                // exclude by tag on the renderer's GameObject (guard against undefined tags)
                bool excluded = false;
                foreach (var t in excludeTags)
                {
                    if (string.IsNullOrEmpty(t)) continue;
#if UNITY_EDITOR
                    if (!IsTagValid(t)) continue;
#endif
                    if (rend.gameObject.CompareTag(t)) { excluded = true; break; }
                }
                if (excluded) continue;

                // proximity test: check if hit point is inside renderer bounds or close to it
                var bounds = rend.bounds;
                bool inside = bounds.Contains(hit.point);
                float sqrDist = (bounds.ClosestPoint(hit.point) - hit.point).sqrMagnitude;
                float threshold = occlusionRadius * occlusionRadius * 4f; // allow some slack for large objects
                if (!inside && sqrDist > threshold)
                {
                    // too far from the hit point; skip this renderer
                    continue;
                }

                // cap concurrent fades
                if (fadeCoroutines.Count >= maxConcurrentFades && !originalColors.ContainsKey(rend))
                    continue;

                // If renderer/material already has alpha <= fadeAlpha, skip starting a fade
                bool alreadyFaded = false;
                var sharedMats = rend.sharedMaterials;
                for (int mi = 0; mi < sharedMats.Length; mi++)
                {
                    var sm = sharedMats[mi];
                    if (sm == null || !sm.HasProperty(PropColor)) continue;
                    if (sm.color.a <= fadeAlpha + 0.01f) { alreadyFaded = true; break; }
                }
                if (alreadyFaded) continue;

                hitRenderers.Add(rend);
                StartFade(rend, fadeAlpha);
            }
        }

        // any renderer we previously faded but not in current hits should be restored
        var toRestore = new System.Collections.Generic.List<Renderer>();
        foreach (var kv in originalColors)
        {
            var rend = kv.Key;
            if (!hitRenderers.Contains(rend))
                toRestore.Add(rend);
        }

        foreach (var rend in toRestore)
            StartFade(rend, 1f);
    }

    private void StartFade(Renderer rend, float targetAlpha)
    {
        if (rend == null) return;

        // ensure we have stored original colors
        if (!originalColors.ContainsKey(rend))
        {
            var mats = rend.sharedMaterials;
            var cols = new Color[mats.Length];
            for (int i = 0; i < mats.Length; i++)
            {
                if (mats[i] != null && mats[i].HasProperty(PropColor))
                    cols[i] = mats[i].color;
                else
                    cols[i] = Color.white;
            }
            originalColors[rend] = cols;

            // init property block
            if (!propBlocks.ContainsKey(rend))
                propBlocks[rend] = new UnityEngine.MaterialPropertyBlock();
        }

        // stop existing coroutine
        if (fadeCoroutines.TryGetValue(rend, out var existing) && existing != null)
            StopCoroutine(existing);

        var coro = StartCoroutine(FadeRendererCoroutine(rend, targetAlpha));
        fadeCoroutines[rend] = coro;
    }

    private System.Collections.IEnumerator FadeRendererCoroutine(Renderer rend, float targetAlpha)
    {
        if (rend == null) yield break;

        // Try to fade using MaterialPropertyBlock to avoid instancing materials where possible.
        var mats = rend.sharedMaterials;
        int mCount = mats.Length;
        if (mCount == 0) yield break; // nothing to do for renderers without materials

        // Ensure we have a stable copy of the original colors for this renderer. It's possible
        // the entry has been removed concurrently, so be defensive and reconstruct if needed.
        Color[] origColors;
        if (!originalColors.TryGetValue(rend, out origColors) || origColors == null || origColors.Length != mCount)
        {
            origColors = new Color[mCount];
            for (int i = 0; i < mCount; i++)
            {
                if (mats[i] != null && mats[i].HasProperty(PropColor))
                    origColors[i] = mats[i].color;
                else
                    origColors[i] = Color.white;
            }
            // Store back so restoration logic can find them later
            originalColors[rend] = origColors;
        }

        Color[] startColors = new Color[mCount];
        for (int i = 0; i < mCount; i++)
        {
            if (mats[i] != null && mats[i].HasProperty(PropColor))
                startColors[i] = origColors[i];
            else
                startColors[i] = Color.white;
        }

        float startTime = Time.time;
        float duration = Mathf.Max(0.01f, 1f / Mathf.Max(0.0001f, fadeSpeed));

        while (true)
        {
            float t = (Time.time - startTime) / duration;
            bool done = t >= 1f;
            float a = Mathf.Lerp(startColors[0].a, targetAlpha, Mathf.Clamp01(t));

            // try property block
            if (propBlocks.TryGetValue(rend, out var block))
            {
                for (int i = 0; i < mCount; i++)
                {
                    if (mats[i] == null || !mats[i].HasProperty(PropColor)) continue;
                    Color c = startColors[i];
                    c.a = a;
                    block.SetColor(PropColor, c);
                    // Note: Many shaders don't read per-material color from _Color in MPBs for submeshes.
                }
                rend.SetPropertyBlock(block);
            }
            else
            {
                // fallback to instancing materials if property block not appropriate
                var instanceMats = rend.materials;
                for (int i = 0; i < instanceMats.Length; i++)
                {
                    if (instanceMats[i] == null || !instanceMats[i].HasProperty(PropColor)) continue;
                    Color c = instanceMats[i].color;
                    c.a = a;
                    instanceMats[i].color = c;
                    SetupMaterialWithTransparency(instanceMats[i], a < 0.99f);
                }
            }

            if (done) break;
            yield return null;
        }

        // if targetAlpha is ~1, fully restore original stored colors
        if (Mathf.Approximately(targetAlpha, 1f) && originalColors.TryGetValue(rend, out var orig))
        {
            // remove property block and restore material colors if necessary
            if (propBlocks.TryGetValue(rend, out var block))
            {
                block.Clear();
                rend.SetPropertyBlock(null);
            }

            var instanceMats = rend.materials;
            for (int i = 0; i < instanceMats.Length && i < orig.Length; i++)
            {
                if (instanceMats[i] == null) continue;
                if (!instanceMats[i].HasProperty(PropColor)) continue;
                instanceMats[i].color = orig[i];
                SetupMaterialWithTransparency(instanceMats[i], false);
            }

            originalColors.Remove(rend);
            propBlocks.Remove(rend);
        }

        fadeCoroutines.Remove(rend);
    }

    // Try to set common standard shader properties so alpha works. This is conservative and won't
    // break non-standard shaders but helps with built-in Standard shader fade.
    private void SetupMaterialWithTransparency(Material mat, bool transparent)
    {
        if (mat == null) return;
        // Only attempt for common property names
        if (mat.HasProperty(PropMode))
        {
            // tweak Standard shader blending for transparency
            mat.SetFloat(PropMode, transparent ? 3f : 0f);
            if (transparent)
            {
                mat.SetInt(PropSrcBlend, (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                mat.SetInt(PropDstBlend, (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                mat.SetInt(PropZWrite, 0);
                mat.DisableKeyword("_ALPHATEST_ON");
                mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                mat.EnableKeyword("_ALPHABLEND_ON");
                mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            }
            else
            {
                mat.SetInt(PropSrcBlend, (int)UnityEngine.Rendering.BlendMode.One);
                mat.SetInt(PropDstBlend, (int)UnityEngine.Rendering.BlendMode.Zero);
                mat.SetInt(PropZWrite, 1);
                mat.DisableKeyword("_ALPHABLEND_ON");
                mat.renderQueue = -1;
            }
        }
    }

    private void OnDisable()
    {
        // restore any remaining materials
        foreach (var kv in originalColors)
        {
            var rend = kv.Key;
            var orig = kv.Value;
            if (rend == null) continue;
            var mats = rend.materials;
            for (int i = 0; i < mats.Length && i < orig.Length; i++)
            {
                if (mats[i] == null) continue;
                if (!mats[i].HasProperty(PropColor)) continue;
                mats[i].color = orig[i];
                SetupMaterialWithTransparency(mats[i], false);
            }
        }
        originalColors.Clear();

        foreach (var kv in fadeCoroutines)
        {
            if (kv.Value != null)
                StopCoroutine(kv.Value);
        }
        fadeCoroutines.Clear();
    }

#if UNITY_EDITOR
    // Editor-only helper to check if a tag is defined without causing warnings.
    private bool IsTagValid(string tagToCheck)
    {
        if (string.IsNullOrEmpty(tagToCheck)) return false;
        foreach (var t in UnityEditorInternal.InternalEditorUtility.tags)
        {
            if (t == tagToCheck) return true;
        }
        return false;
    }
#endif
}
