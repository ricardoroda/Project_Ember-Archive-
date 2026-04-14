using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class OptionsHUD : MonoBehaviour
{
    [Header("Buttons and Panels")]
    public Button optionsButton;
    public Button backHubButton;
    public Button closeButton;
    public GameObject optionsPanel;
    public Button yesExitButton;
    public Button yesHubExitButton;
    public Button yesMainMenuExitButton;
    public GameObject[] blockingPanels;

    void Start()
    {
        SetupYesExitButton();
        SetupYesHubExitButton();
        SetupYesMainMenuExitButton();
        HideBackHubButton();

         // audio sfx
        if (optionsButton != null)
            optionsButton.onClick.AddListener(() => AudioManager.Instance.PlaySfx(SfxType.Pause));
       
        if (closeButton != null)
            closeButton.onClick.AddListener(() => AudioManager.Instance.PlaySfx(SfxType.Resume));
    }

    void Update()
    {
        PauseGameOnOptionsPanel();

        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (optionsPanel != null && optionsPanel.activeSelf)
            {
                closeButton?.onClick.Invoke();
            }
            else
            {
                // if any blocking panels are open, do not open the skill tree
                if (IsAnyBlockingPanelOpen()) return;

                optionsButton?.onClick.Invoke();
            }
        }
    }
    private bool IsAnyBlockingPanelOpen()
    {
        if (blockingPanels == null) return false;
        foreach (var g in blockingPanels)
        {
            if (g != null && g.activeInHierarchy) return true;
        }
        return false;
    }
    
        private void SetupYesExitButton()
    {
        if (yesExitButton == null)
            return;

        yesExitButton.onClick.RemoveAllListeners();
        yesExitButton.onClick.AddListener(() =>
        {
#if UNITY_EDITOR
            GameObject.FindWithTag("Player").GetComponent<PlayerStats>().SavePLayerDataToSaveSO();
            UnityEditor.EditorApplication.isPlaying = false;
#else
        GameObject.FindWithTag("Player").GetComponent<PlayerStats>().SavePLayerDataToSaveSO();
        Application.Quit();
#endif
        });
    }

    private void PauseGameOnOptionsPanel()
    {

        if (optionsButton != null && optionsButton.gameObject.activeSelf)
        {
            Time.timeScale = 1f; 
        }
        else
        {
            Time.timeScale = 0f; 
        }
    }
    private void HideBackHubButton()
    {
        if (SceneManager.GetActiveScene().buildIndex == 1 && backHubButton != null)
        {
            backHubButton.gameObject.SetActive(false);
        }
    }

    private void SetupYesHubExitButton()
    {
        if (yesHubExitButton == null)
            return;
        yesHubExitButton.onClick.RemoveAllListeners();
        yesHubExitButton.onClick.AddListener(() => { try { ProjectEmber.Loading.SceneLoader.Load(1); } catch { SceneManager.LoadScene(1); } });
    }

    private void SetupYesMainMenuExitButton()
    {
        if (yesMainMenuExitButton == null)
            return;

        yesMainMenuExitButton.onClick.RemoveAllListeners();
        yesMainMenuExitButton.onClick.AddListener(() => { try {  ProjectEmber.Loading.SceneLoader.Load(0); } catch { SceneManager.LoadScene(0); } });
    }
}
