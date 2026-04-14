using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System;
using System.IO;
using Handlers;
using TMPro;

public class MainMenu : MonoBehaviour
{
    [Header("Toggles and Buttons")]
    [SerializeField] private Toggle warriorToggle;
    [SerializeField] private Toggle mageToggle;
    [SerializeField] private Toggle rogueToggle;
    [SerializeField] private Button startButton;
    [SerializeField] private Button exitButton;
    [SerializeField] private GameObject newGameMenu;
    [SerializeField] private GameObject LoadGameMenu;

    [SerializeField] private Button loadSave1;
    [SerializeField] private Button loadSave2;
    [SerializeField] private Button loadSave3;
    [SerializeField] private Button deleteSave1;
    [SerializeField] private Button deleteSave2;
    [SerializeField] private Button deleteSave3;

    // Save File Logistics
    [Header("Save File Logistics")]
    private GameController gameController;
    private string saveDirectoryPath => Application.persistentDataPath;
    [SerializeField] private PlayerSaveSO[] playerSaveSOList;
    [SerializeField] private GameObject[] playerPrefabs;

    // Txt save Info
    [SerializeField] private TMP_Text tmpSave1;
    [SerializeField] private TMP_Text tmpSave2;
    [SerializeField] private TMP_Text tmpSave3;
    

    // internal
    private int pendingSaveIndex = -1; // -1 = none

    public static event Action OnNewGameStarted; // For AudioManager

    void Start()
    {
        // find game controller (ensure tag matches in your scene)
        gameController = GameObject.FindGameObjectWithTag("GameController")?.GetComponent<GameController>();

        // wire toggles
        if (warriorToggle != null) warriorToggle.onValueChanged.AddListener(isOn => OnClassToggled(warriorToggle, isOn));
        if (mageToggle != null) mageToggle.onValueChanged.AddListener(isOn => OnClassToggled(mageToggle, isOn));
        if (rogueToggle != null) rogueToggle.onValueChanged.AddListener(isOn => OnClassToggled(rogueToggle, isOn));

        // Verify existence & load save files from disk into the PlayerSaveSO objects
        if (playerSaveSOList != null)
        {
            foreach (PlayerSaveSO playerSaveSO in playerSaveSOList)
            {
                if (playerSaveSO == null) continue;
                string saveFilePath = Path.Combine(saveDirectoryPath, playerSaveSO.saveFileName);
                if (playerSaveSO.CheckSaveFileExistence(saveDirectoryPath) && File.Exists(saveFilePath))
                {
                    string playerSaveString = File.ReadAllText(saveFilePath);
                    JsonUtility.FromJsonOverwrite(playerSaveString, playerSaveSO);
                    playerSaveSO.saveFileExists = true;
                }
                else playerSaveSO.ResetSave();
            }
        }

        SetupStartButton();
        SetupExitButton();
        SetupLoadButtons();
        startButton?.gameObject.SetActive(AnyClassToggleOn());

        // atualizar textos dos 3 slots
        UpdateAllSaveTexts();
    }

    private void OnClassToggled(Toggle changed, bool isOn)
    {
        // when one turns on, disable the others and show start; when off, re-enable others and hide start if none selected
        if (isOn)
        {
            if (changed != warriorToggle && warriorToggle != null) { warriorToggle.SetIsOnWithoutNotify(false); warriorToggle.interactable = false; }
            if (changed != mageToggle && mageToggle != null) { mageToggle.SetIsOnWithoutNotify(false); mageToggle.interactable = false; }
            if (changed != rogueToggle && rogueToggle != null) { rogueToggle.SetIsOnWithoutNotify(false); rogueToggle.interactable = false; }
            if (changed != null) changed.interactable = true;
            startButton?.gameObject.SetActive(true);
        }
        else
        {
            bool any = AnyClassToggleOn();
            if (!any)
            {
                if (warriorToggle != null) warriorToggle.interactable = true;
                if (mageToggle != null) mageToggle.interactable = true;
                if (rogueToggle != null) rogueToggle.interactable = true;
                startButton?.gameObject.SetActive(false);
            }
        }
    }

    private bool AnyClassToggleOn()
    {
        return (warriorToggle != null && warriorToggle.isOn) ||
               (mageToggle != null && mageToggle.isOn) ||
               (rogueToggle != null && rogueToggle.isOn);
    }

    // --- Start button: handles both new-game-from-empty-slot and normal start ---
    private void SetupStartButton()
    {
        if (startButton == null) return;
        startButton.onClick.RemoveAllListeners();
        startButton.onClick.AddListener(() =>
        {
            AudioManager.Instance?.PlaySfx(SfxType.Button);
            // If player came from an empty slot, commit the new save with chosen class
            if (pendingSaveIndex >= 0 && playerSaveSOList != null && pendingSaveIndex < playerSaveSOList.Length)
            {
                var save = playerSaveSOList[pendingSaveIndex];
                if (save == null) return;

                int chosenClass = (int)ClassListData.UserClasses.Mage;
                if (mageToggle != null && mageToggle.isOn) chosenClass = (int)ClassListData.UserClasses.Mage;
                else if (warriorToggle != null && warriorToggle.isOn) chosenClass = (int)ClassListData.UserClasses.Warrior;
                else if (rogueToggle != null && rogueToggle.isOn) chosenClass = (int)ClassListData.UserClasses.Rogue;

                save.playerClassIndex = chosenClass;
                save.saveFileExists = true;
                if (save.level <= 0) save.level = 1;

                try { File.WriteAllText(Path.Combine(saveDirectoryPath, save.saveFileName), JsonUtility.ToJson(save, true)); }
                catch (Exception ex) { Debug.LogWarning($"Failed to write save file: {ex.Message}"); }

                SetupGameControllerWithSave(save);

                pendingSaveIndex = -1;
                newGameMenu?.SetActive(false);
                LoadGameMenu?.SetActive(false);

                OnNewGameStarted?.Invoke();
                UpdateAllSaveTexts();
                TryLoadGameScene();
                return;
            }

            // fallback: start first existing save
            if (playerSaveSOList != null)
            {
                for (int i = 0; i < playerSaveSOList.Length; i++)
                {
                    var s = playerSaveSOList[i];
                    if (s != null && s.saveFileExists)
                    {
                        string p = Path.Combine(saveDirectoryPath, s.saveFileName);
                        if (File.Exists(p)) JsonUtility.FromJsonOverwrite(File.ReadAllText(p), s);
                        SetupGameControllerWithSave(s);
                        TryLoadGameScene();
                        return;
                    }
                }
            }
        });
    }

    private void SetupExitButton()
    {
        if (exitButton == null) return;
        exitButton.onClick.RemoveAllListeners();
        exitButton.onClick.AddListener(() =>
        {
            AudioManager.Instance?.PlaySfx(SfxType.Button);
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        });
    }

    private void SetupGameControllerWithSave(PlayerSaveSO playerSaveSO)
    {
        if (gameController == null || playerSaveSO == null) return;
        gameController.playerSaveFileSO = playerSaveSO;

        switch (playerSaveSO.playerClassIndex)
        {
            case (int)ClassListData.UserClasses.Mage:
                if (playerPrefabs != null && playerPrefabs.Length > 0) gameController.playerPrefab = playerPrefabs[0];
                break;
            case (int)ClassListData.UserClasses.Warrior:
                if (playerPrefabs != null && playerPrefabs.Length > 1) gameController.playerPrefab = playerPrefabs[1];
                break;
            case (int)ClassListData.UserClasses.Rogue:
                if (playerPrefabs != null && playerPrefabs.Length > 2) gameController.playerPrefab = playerPrefabs[2];
                break;
            default:
                if (playerPrefabs != null && playerPrefabs.Length > 0) gameController.playerPrefab = playerPrefabs[0];
                break;
        }
    }

    // --- Save/Load UI Section ---
    private void SetupLoadButtons()
    {
        if (loadSave1 != null) { int i = 0; loadSave1.onClick.RemoveAllListeners(); loadSave1.onClick.AddListener(() => OnLoadButtonPressed(i)); }
        if (loadSave2 != null) { int i = 1; loadSave2.onClick.RemoveAllListeners(); loadSave2.onClick.AddListener(() => OnLoadButtonPressed(i)); }
        if (loadSave3 != null) { int i = 2; loadSave3.onClick.RemoveAllListeners(); loadSave3.onClick.AddListener(() => OnLoadButtonPressed(i)); }

        if (deleteSave1 != null) { int i = 0; deleteSave1.onClick.RemoveAllListeners(); deleteSave1.onClick.AddListener(() => OnDeleteButtonPressed(i)); }
        if (deleteSave2 != null) { int i = 1; deleteSave2.onClick.RemoveAllListeners(); deleteSave2.onClick.AddListener(() => OnDeleteButtonPressed(i)); }
        if (deleteSave3 != null) { int i = 2; deleteSave3.onClick.RemoveAllListeners(); deleteSave3.onClick.AddListener(() => OnDeleteButtonPressed(i)); }
    }

    private void OnLoadButtonPressed(int index)
    {
        if (playerSaveSOList == null || index < 0 || index >= playerSaveSOList.Length) return;
        var save = playerSaveSOList[index];
        if (save == null) return;

        if (save.saveFileExists)
        {
            AudioManager.Instance?.PlaySfx(SfxType.Button);
            string path = Path.Combine(saveDirectoryPath, save.saveFileName);
            if (File.Exists(path)) JsonUtility.FromJsonOverwrite(File.ReadAllText(path), save);

            // valid save -> set up game controller and load scene
            SetupGameControllerWithSave(save);
            pendingSaveIndex = -1;
            TryLoadGameScene();
        }
        else
        {
            // empty slot -> remember this slot and show new game menu
            pendingSaveIndex = index;
            if (warriorToggle != null) warriorToggle.SetIsOnWithoutNotify(false);
            if (mageToggle != null) mageToggle.SetIsOnWithoutNotify(false);
            if (rogueToggle != null) rogueToggle.SetIsOnWithoutNotify(false);
            if (warriorToggle != null) warriorToggle.interactable = true;
            if (mageToggle != null) mageToggle.interactable = true;
            if (rogueToggle != null) rogueToggle.interactable = true;
            startButton?.gameObject.SetActive(false);

            newGameMenu?.SetActive(true);
            LoadGameMenu?.SetActive(false);
        }
    }

    private void OnDeleteButtonPressed(int index)
    {
        if (playerSaveSOList == null || index < 0 || index >= playerSaveSOList.Length) return;
        var save = playerSaveSOList[index];
        if (save == null) return;

        try { File.Delete(Path.Combine(saveDirectoryPath, save.saveFileName)); } catch { }
        save.ResetSave();
        AudioManager.Instance?.PlaySfx(SfxType.Button);

        UpdateSaveText(index);
    }

    private void TryLoadGameScene()
    {
        try { ProjectEmber.Loading.SceneLoader.Load(1); } catch { SceneManager.LoadScene(1); }
    }

    // Save info
    private void UpdateAllSaveTexts()
    {
        for (int i = 0; i < 3; i++) UpdateSaveText(i);
    }

    private void UpdateSaveText(int index)
    {
        TMP_Text target = index == 0 ? tmpSave1 : index == 1 ? tmpSave2 : tmpSave3;
        if (target == null) return;
        if (playerSaveSOList == null || index < 0 || index >= playerSaveSOList.Length) { target.text = "Empty"; return; }

        var save = playerSaveSOList[index];
        if (save == null || !save.saveFileExists) { target.text = "Empty"; return; }

        string className = GetClassName(save.playerClassIndex);
        target.text = $"Lvl: {save.level} - Class: {className}";
    }

    private string GetClassName(int idx)
    {
        switch (idx)
        {
            case (int)ClassListData.UserClasses.Mage: return "Mage";
            case (int)ClassListData.UserClasses.Warrior: return "Warrior";
            case (int)ClassListData.UserClasses.Rogue: return "Rogue";
            default: return "Unknown";
        }
    }
}
