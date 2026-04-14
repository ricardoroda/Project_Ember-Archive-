using UnityEngine;
using TMPro;
using System.Collections;

[RequireComponent(typeof(CanvasGroup))]
public class WorldFloatingText : MonoBehaviour
{
    public float riseSpeed = 2f;   
    public float lifeTime = 2f;   
    public Vector3 randomOffset = new Vector3(0.5f, 0f, 0.5f);

    private TextMeshProUGUI label;
    private CanvasGroup canvasGroup;
    private float elapsed = 0f;
    private Camera mainCamera;

    void Awake()
    {
        label = GetComponent<TextMeshProUGUI>();
        canvasGroup = GetComponent<CanvasGroup>();
        canvasGroup.alpha = 1f;

        mainCamera = Camera.main;

        Vector3 rand = new Vector3(
            Random.Range(-randomOffset.x, randomOffset.x),
            0,
            Random.Range(-randomOffset.z, randomOffset.z)
        );
        transform.position += rand;
    }

    public void Setup(string message, Color color)
    {
        label.text  = message;
        label.color = color;
    }

    void Update()
    {
        if (mainCamera != null)
        {
            transform.rotation = mainCamera.transform.rotation;
        }

        transform.Translate(Vector3.up * riseSpeed * Time.deltaTime, Space.World);

        elapsed += Time.deltaTime;
        canvasGroup.alpha = Mathf.Clamp01(1f - (elapsed / lifeTime));

        // destroy the whole canvas/text root
        if (elapsed >= lifeTime)
        {
            Destroy(transform.root.gameObject);
        }
    }
}
