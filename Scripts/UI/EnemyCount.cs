using UnityEngine;
using TMPro;

public class EnemyCount : MonoBehaviour
{
    [Header("Referências UI")]
    [SerializeField] private TextMeshProUGUI waveText;
    [SerializeField] private TextMeshProUGUI enemiesText;

    [Header("Configurações")]
    [SerializeField] private string enemyTag = "Enemy";

    private int currentWave = 1;

    void Start()
    {
        UpdateWaveText();
        UpdateEnemiesText();
    }

    void Update()
    {
        UpdateEnemiesText();
    }

    public void SetWave(int waveNumber)
    {
        currentWave = waveNumber;
        UpdateWaveText();
    }

    private void UpdateWaveText()
    {
        if (waveText != null)
            waveText.text = $"Wave: {currentWave}";
    }

    private void UpdateEnemiesText()
    {
        if (enemiesText != null)
        {
            int count = GameObject.FindGameObjectsWithTag(enemyTag).Length;
            enemiesText.text = $"Enemies Left: {count}";
        }
    }
}
