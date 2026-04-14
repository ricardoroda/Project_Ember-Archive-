using UnityEngine;
using UnityEngine.Rendering;

[RequireComponent(typeof(Camera))]
public class MiniMapFollow : MonoBehaviour
{
    private Transform player;
    private Camera cam;
    private bool prevFog;

    void Start()
    {
        cam = GetComponent<Camera>();

        var stats = FindFirstObjectByType<PlayerStats>();
        if (stats != null)
            player = stats.transform;
        else
            Debug.LogError("MiniMapFollow: PlayerStats not found in the scene.");
    }

    void OnEnable()
    {
        RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
        RenderPipelineManager.endCameraRendering += OnEndCameraRendering;
    }

    void OnDisable()
    {
        RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
        RenderPipelineManager.endCameraRendering -= OnEndCameraRendering;

        // restore fog just in case
        RenderSettings.fog = prevFog;
    }

    void LateUpdate()
    {
        if (player == null) return;

        Vector3 newPos = player.position;
        newPos.y = transform.position.y;
        transform.position = newPos;
    }

    void OnBeginCameraRendering(ScriptableRenderContext ctx, Camera camera)
    {
        if (camera != cam) return;
        prevFog = RenderSettings.fog;
        RenderSettings.fog = false;
    }

    void OnEndCameraRendering(ScriptableRenderContext ctx, Camera camera)
    {
        if (camera != cam) return;
        RenderSettings.fog = prevFog;
    }
}
