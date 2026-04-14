using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using UnityEngine.SceneManagement;

public class PlayerHUD : MonoBehaviour
{
    [Header("References")]
    public PlayerStats stats;
    public GameObject deathPanel;
    public Button retreatButton;
    public Button statsButton;
    public GameObject statsTab;

    [Header("UI Sliders")]
    public Slider healthFill;
    public Slider manaFill;
    public Slider xpFill;
    public Slider armorFill;

    [Header("UI Text")]
    public TextMeshProUGUI levelLabel;
    public TextMeshProUGUI healthText;
    public TextMeshProUGUI manaText;
    public TextMeshProUGUI xpText;
    public TextMeshProUGUI levelUPText;
    public TextMeshProUGUI skillPointsText;
    public TextMeshProUGUI armorText;
    public TextMeshProUGUI compactStatsText;

    public float levelMessageDuration = 3f;

    private Coroutine levelCoroutine;
    private bool subscribed = false;

    // cache for polling fallback
    float prevHealth = -1f, prevMaxHealth = -1f;
    float prevMana = -1f, prevMaxMana = -1f;
    float prevArmor = -1f, prevMaxArmor = -1f;
    int prevXP = -1, prevXPToNext = -1;
    int prevLevel = -1, prevSkillPoints = -1;

    private bool prevPlayerActive = true;
    private bool deathPanelShown = false;

    void Awake()
    {
        if (stats == null)
            stats = FindFirstObjectByType<PlayerStats>();
        if (stats != null)
        {
            SubscribeToStats();
            prevPlayerActive = stats.gameObject.activeSelf;
        }
    }

    void OnEnable()
    {
        if (stats != null && !subscribed)
            SubscribeToStats();
    }

    void Start()
    {
        if (!subscribed && stats != null)
            SubscribeToStats();

        if (stats == null)
        {
            Debug.LogError("PlayerHUD: stats not assigned", this);
            enabled = false;
            return;
        }

        prevPlayerActive = stats.gameObject.activeSelf;
        deathPanelShown = deathPanel != null && deathPanel.activeSelf;

        if (healthFill != null) { healthFill.minValue = 0; healthFill.maxValue = stats.maxHealth; }
        if (manaFill != null) { manaFill.minValue = 0; manaFill.maxValue = stats.maxMana; }
        if (armorFill != null) { armorFill.minValue = 0; armorFill.maxValue = stats.maxArmor; }
        if (xpFill != null) { xpFill.minValue = 0; xpFill.maxValue = stats.xpToNextLevel; }

        UpdateAllUI();

        if (levelUPText != null) levelUPText.gameObject.SetActive(false);
        if (skillPointsText != null) skillPointsText.gameObject.SetActive(false);

        SetupRetreatButton();

        if (stats.skillPoints > 0)
        {
            if (levelCoroutine != null) StopCoroutine(levelCoroutine);
            levelCoroutine = StartCoroutine(ShowLevelUpMessages(stats.currentLevel));
        }
    }

    void OnDisable() => UnsubscribeFromStats();
    void OnDestroy() => UnsubscribeFromStats();

    void Update()
    {
        if (!subscribed && stats != null)
            SubscribeToStats();

        if (stats != null)
        {
            PollAndRefresh();

            bool currentActive = stats.gameObject.activeSelf;
            if (prevPlayerActive && !currentActive && !deathPanelShown)
            {
                // player was just deactivated
                ShowDeathPanel();
            }
            prevPlayerActive = currentActive;
        }

        if (statsTab.activeInHierarchy)
        {
            statsButton.GetComponent<Image>().color = new Color(0.82f, 0.55f, 0.35f, 1f);
        }
        else
        {
            statsButton.GetComponent<Image>().color = new Color(0.60f, 0.40f, 0.27f, 1f);
        }
    }

    private void SubscribeToStats()
    {
        UnsubscribeFromStats();

        if (stats == null) return;

        stats.OnHealthChanged += UpdateHealth;
        stats.OnManaChanged += UpdateMana;
        stats.OnXPChanged += UpdateXP;
        stats.OnLevelUp += UpdateLevel;
        stats.OnArmorChanged += UpdateArmor;

        subscribed = true;

        // immediate UI refresh
        UpdateAllUI();
    }

    private void UnsubscribeFromStats()
    {
        if (stats == null || !subscribed) return;

        stats.OnHealthChanged -= UpdateHealth;
        stats.OnManaChanged -= UpdateMana;
        stats.OnXPChanged -= UpdateXP;
        stats.OnLevelUp -= UpdateLevel;
        stats.OnArmorChanged -= UpdateArmor;

        subscribed = false;
    }

    void UpdateAllUI()
    {
        if (stats == null) return;

        UpdateHealth(stats.currentHealth, stats.maxHealth);
        UpdateMana(stats.currentMana, stats.maxMana);
        UpdateArmor(stats.currentArmor, stats.maxArmor);
        UpdateXP(stats.currentXP, stats.xpToNextLevel);
        if (levelLabel != null) levelLabel.text = stats.currentLevel.ToString();

        // cache values
        prevHealth = stats.currentHealth; prevMaxHealth = stats.maxHealth;
        prevMana = stats.currentMana; prevMaxMana = stats.maxMana;
        prevArmor = stats.currentArmor; prevMaxArmor = stats.maxArmor;
        prevXP = stats.currentXP; prevXPToNext = stats.xpToNextLevel;
        prevLevel = stats.currentLevel; prevSkillPoints = stats.skillPoints;

        UpdateCompactText();
    }

    void PollAndRefresh()
    {
        if (stats.currentHealth != prevHealth || stats.maxHealth != prevMaxHealth)
            UpdateHealth(stats.currentHealth, stats.maxHealth);

        if (stats.currentMana != prevMana || stats.maxMana != prevMaxMana)
            UpdateMana(stats.currentMana, stats.maxMana);

        if (stats.currentArmor != prevArmor || stats.maxArmor != prevMaxArmor)
            UpdateArmor(stats.currentArmor, stats.maxArmor);

        if (stats.currentXP != prevXP || stats.xpToNextLevel != prevXPToNext)
            UpdateXP(stats.currentXP, stats.xpToNextLevel);

        if (stats.currentLevel != prevLevel)
            UpdateLevel(stats.currentLevel);

        // update cache
        prevHealth = stats.currentHealth; prevMaxHealth = stats.maxHealth;
        prevMana = stats.currentMana; prevMaxMana = stats.maxMana;
        prevArmor = stats.currentArmor; prevMaxArmor = stats.maxArmor;
        prevXP = stats.currentXP; prevXPToNext = stats.xpToNextLevel;
        prevLevel = stats.currentLevel; prevSkillPoints = stats.skillPoints;
    }

    void UpdateHealth(float current, float max)
    {
        if (healthFill != null) { healthFill.maxValue = max; healthFill.value = current; }
        if (healthText != null) healthText.text = $"{current} / {max}";
        UpdateCompactText();
    }

    void UpdateMana(float current, float max)
    {
        if (manaFill != null) { manaFill.maxValue = max; manaFill.value = current; }
        if (manaText != null) manaText.text = $"{current} / {max}";
        UpdateCompactText();
    }

    void UpdateArmor(float current, float max)
    {
        if (armorFill != null) { armorFill.maxValue = max; armorFill.value = current; }
        if (armorText != null) armorText.text = $"{current} / {max}";
        UpdateCompactText();
    }

    void UpdateXP(int currentXP, int xpToNext)
    {
        if (xpFill != null) { xpFill.maxValue = xpToNext; xpFill.value = currentXP; }
        if (xpText != null) xpText.text = $"{currentXP} / {xpToNext}";
    }

    void UpdateLevel(int level)
    {
        if (levelLabel != null) levelLabel.text = level.ToString();

        if (levelCoroutine != null) StopCoroutine(levelCoroutine);
        levelCoroutine = StartCoroutine(ShowLevelUpMessages(level));
        UpdateCompactText();
    }

    private IEnumerator ShowLevelUpMessages(int level)
    {
        if (levelUPText != null)
            levelUPText.text = $"Level Up! You are now level {level}!";

        if (skillPointsText != null)
            skillPointsText.text = $"Skill Points: {stats?.skillPoints}";

        if (levelUPText != null) levelUPText.gameObject.SetActive(true);
        if (skillPointsText != null) skillPointsText.gameObject.SetActive(true);

        yield return new WaitForSeconds(levelMessageDuration);

        if (levelUPText != null) levelUPText.gameObject.SetActive(false);
        if (skillPointsText != null) skillPointsText.gameObject.SetActive(false);

        levelCoroutine = null;
    }

    // keep HUD alive and show last values;
    private void ShowDeathPanel()
    {
        // ensure the Hud has the latest values cached / shown
        UpdateAllUI();

        if (deathPanel != null)
        {
            deathPanel.SetActive(true);
            deathPanelShown = true;
        }
    }

    private void SetupRetreatButton()
    {
        if (retreatButton != null)
        {
            retreatButton.onClick.RemoveAllListeners();
            retreatButton.onClick.AddListener(() =>
            {
                try { ProjectEmber.Loading.SceneLoader.Load(1); } catch { SceneManager.LoadScene(1); }
            });
        }
    }

    private void UpdateCompactText()
    {
        if (compactStatsText == null || stats == null) return;

        int lvl = stats.currentLevel;
        int hp = Mathf.RoundToInt(stats.currentHealth);
        int hpMax = Mathf.RoundToInt(stats.maxHealth);
        int mp = Mathf.RoundToInt(stats.currentMana);
        int mpMax = Mathf.RoundToInt(stats.maxMana);
        int ar = Mathf.RoundToInt(stats.currentArmor);
        int arMax = Mathf.RoundToInt(stats.maxArmor);

        int str = stats.strength;
        int dex = stats.dexterity;
        int intel = stats.intelligence;
        int vit = stats.vitality;

        // compact single-line layout (keeps it short)
        compactStatsText.text =
            $"\nLvl: {lvl}  \nHP: {hp}/{hpMax}  \nMP: {mp}/{mpMax}  \nArmor: {ar}/{arMax}  \nSTR: {str} \nDEX: {dex} \nINT: {intel} \nVIT: {vit}";
    }
}
