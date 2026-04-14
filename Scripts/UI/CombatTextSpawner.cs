using UnityEngine;
using TMPro;

public class CombatTextSpawner : MonoBehaviour
{
    public static CombatTextSpawner Instance { get; private set; }

    public GameObject worldTextCanvasPrefab;

    void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;
    }

    public void ShowWorldText(string message, Color color, Vector3 worldPos)
    {
        var canvasGO = Instantiate(worldTextCanvasPrefab, worldPos, Quaternion.identity);
        var textUI = canvasGO.GetComponentInChildren<TextMeshProUGUI>();
        var ftext = textUI.GetComponent<WorldFloatingText>();
        ftext.Setup(message, color);
    }
}
