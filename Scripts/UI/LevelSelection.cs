// LevelSelectionUI.cs
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class LevelSelectionUI : MonoBehaviour
{
    [Header("Buttons")]
    public Button loadArena;
    public Button loadForest;
    public Button closeButton;

    [Header("Config")]
    public int arenaSceneIndex = 2;
    public int forestSceneIndex = 3;

    [Header("References")]
    public GameObject playerHUD;
    public GameObject interactionUI;

    void OnEnable()
    {
        playerHUD = GameObject.Find("PlayerHUD(Clone)");
        loadArena.onClick.RemoveAllListeners();
        loadArena.onClick.AddListener(() => {
            AudioManager.Instance.PlaySfx(SfxType.Button);
            LoadArena();
        });

        loadForest.onClick.RemoveAllListeners();
        loadForest.onClick.AddListener(() => {
            AudioManager.Instance.PlaySfx(SfxType.Button);
            LoadForest();
        });

        closeButton.onClick.RemoveAllListeners();
        closeButton.onClick.AddListener(() => {
            AudioManager.Instance.PlaySfx(SfxType.Button);
            ClosePanel();
        });
    }

    private void LoadArena()
    {
        Debug.Log("Load button clicked!");
        try
        {
            ProjectEmber.Loading.SceneLoader.Load(arenaSceneIndex);
        }
        catch
        {
            SceneManager.LoadScene(arenaSceneIndex);
        }
    }
    
    private void LoadForest()
    {
        Debug.Log("Load button clicked!");
        try
        {
            ProjectEmber.Loading.SceneLoader.Load(forestSceneIndex);
        }
        catch
        {
            SceneManager.LoadScene(forestSceneIndex);
        }
    }

    private void ClosePanel()
    {
        if (interactionUI != null)
            interactionUI.SetActive(true);

        gameObject.SetActive(false);
    }
}
