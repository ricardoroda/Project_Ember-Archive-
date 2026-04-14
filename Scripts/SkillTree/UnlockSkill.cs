using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UnlockSkill : MonoBehaviour
{
    [Header("Assign in Inspector")]
    public PlayerStats stats;
    public Button unlockButton;
    public Button infButton;
    public TextMeshProUGUI costText;
    public GameObject unlockPanelPrefab;
    public GameObject infPanelPrefab;

    [Header("Settings")]
    public int cost = 1;
    public string yesName = "YesButton";
    public string noName = "NoButton";
    public string closeName = "CloseButton";
    public float shakeDuration = 0.25f;
    public float shakeMagnitude = 8f;

    GameObject activeUnlockPanel;
    GameObject activeInfPanel;

    static GameObject globalUnlockPanel;
    static GameObject globalInfPanel;

    void OnValidate() => UpdateCostText();

    void Start()
    {
        UpdateCostText();
        if (unlockButton) unlockButton.onClick.AddListener(OpenUnlockPanel);
        if (infButton)    infButton.onClick.AddListener(OpenInfPanel);
    }

    // ensure panels are cleaned when this object is disabled
    void OnDisable()
    {
        if (globalUnlockPanel != null) DestroyAnyPanelImmediate(globalUnlockPanel);
        if (globalInfPanel != null)    DestroyAnyPanelImmediate(globalInfPanel);
    }

    void Update()
    {
        if (activeUnlockPanel && (!unlockButton || !unlockButton.gameObject.activeInHierarchy || !unlockButton.interactable))
            DestroyPanel(ref activeUnlockPanel);

        if (activeInfPanel && (!infButton || !infButton.gameObject.activeInHierarchy || !unlockButton.interactable))
            DestroyPanel(ref activeInfPanel);
    }

    void UpdateCostText()
    {
        if (costText) costText.text = cost.ToString();
    }

    void OpenUnlockPanel()
    {
        if (globalUnlockPanel != null) DestroyAnyPanelImmediate(globalUnlockPanel);
        if (activeUnlockPanel != null) DestroyPanel(ref activeUnlockPanel);
        if (unlockPanelPrefab == null || unlockButton == null) return;

        activeUnlockPanel = Instantiate(unlockPanelPrefab, unlockButton.transform.parent, false);
        globalUnlockPanel = activeUnlockPanel;

        PositionOverButton(activeUnlockPanel, unlockButton);
        BringPanelOnTop(activeUnlockPanel);

        var yes = FindButtonByName(activeUnlockPanel, yesName);
        if (yes) yes.onClick.AddListener(() => ConfirmUnlock(yes));

        var no = FindButtonByName(activeUnlockPanel, noName);
        if (no) no.onClick.AddListener(() => DestroyPanel(ref activeUnlockPanel));
    }

    void OpenInfPanel()
    {
        if (globalInfPanel != null) DestroyAnyPanelImmediate(globalInfPanel);
        if (activeInfPanel != null) DestroyPanel(ref activeInfPanel);
        if (infPanelPrefab == null || infButton == null) return;

        activeInfPanel = Instantiate(infPanelPrefab, infButton.transform.parent, false);
        globalInfPanel = activeInfPanel;

        PositionOverButton(activeInfPanel, infButton);
        BringPanelOnTop(activeInfPanel);

        var close = FindButtonByName(activeInfPanel, closeName);
        if (close) close.onClick.AddListener(() => DestroyPanel(ref activeInfPanel));

        // find SkillItem
        var slot = GetComponentInParent<SkillTreeSlot>();
        SkillItem skillItem = null;

        if (slot != null)
        {
            skillItem = slot.GetComponentInChildren<SkillItem>(true);
        }
        else
        {
            skillItem = GetComponentInParent<SkillItem>();
        }

        var panelScript = activeInfPanel.GetComponentInChildren<AbilityInfoPanel>(true);
        if (panelScript != null)
            panelScript.PopulateFromSkillItem(skillItem);
        else
            Debug.LogWarning("[UnlockSkill] infPanelPrefab not assigned");
    }


    void ConfirmUnlock(Button yes)
    {
        if (!stats) stats = FindAnyObjectByType<PlayerStats>();
        if (!stats) { Shake(yes); return; }

        int spent = 0;
        for (int i = 0; i < cost; i++)
        {
            if (stats.SpendSkillPoints()) spent++;
            else break;
        }

        if (spent >= cost)
        {
            if (unlockButton) Destroy(unlockButton.gameObject);
            DestroyPanel(ref activeUnlockPanel);
        }
        else Shake(yes);
    }

    void PositionOverButton(GameObject panel, Button btn)
    {
        var panelRT  = panel.GetComponent<RectTransform>();
        var buttonRT = btn.GetComponent<RectTransform>();
        if (panelRT && buttonRT) panelRT.anchoredPosition = buttonRT.anchoredPosition;
        panel.transform.SetAsLastSibling();
    }

    void BringPanelOnTop(GameObject panel)
    {
        var c = panel.GetComponent<Canvas>();
        if (!c) c = panel.AddComponent<Canvas>();
        c.overrideSorting = true;
        c.sortingOrder = 9999;

        if (!panel.GetComponent<GraphicRaycaster>()) panel.AddComponent<GraphicRaycaster>();
    }

    // destroy panel created by this instance
    void DestroyPanel(ref GameObject panel)
    {
        if (panel == null) return;

        foreach (var b in panel.GetComponentsInChildren<Button>(true))
            b.onClick.RemoveAllListeners();

        panel.transform.SetParent(null, false);
        panel.SetActive(false);

        if (panel == globalUnlockPanel) globalUnlockPanel = null;
        if (panel == globalInfPanel)    globalInfPanel = null;

        var toDestroy = panel;
        panel = null;
        Destroy(toDestroy);
    }

    // immediate destroy for any panel (used for global cleanup)
    static void DestroyAnyPanelImmediate(GameObject panel)
    {
        if (panel == null) return;

        foreach (var b in panel.GetComponentsInChildren<Button>(true))
            b.onClick.RemoveAllListeners();

        panel.transform.SetParent(null, false);
        panel.SetActive(false);

        if (panel == globalUnlockPanel) globalUnlockPanel = null;
        if (panel == globalInfPanel)    globalInfPanel = null;

        Object.Destroy(panel);
    }

    Button FindButtonByName(GameObject root, string name)
    {
        foreach (var b in root.GetComponentsInChildren<Button>(true))
            if (b.gameObject.name == name) return b;
        return null;
    }

    void Shake(Button btn)
    {
        if (!btn) return;
        var rt = btn.GetComponent<RectTransform>();
        StartCoroutine(ShakeCR(rt));
    }

    IEnumerator ShakeCR(RectTransform rt)
    {
        if (!rt) yield break;
        Vector2 start = rt.anchoredPosition;
        float t = 0f;
        while (t < shakeDuration)
        {
            float x = Mathf.Sin(t * 60f) * shakeMagnitude * (1f - t / shakeDuration);
            rt.anchoredPosition = start + new Vector2(x, 0f);
            t += Time.unscaledDeltaTime;
            yield return null;
        }
        rt.anchoredPosition = start;
    }
}
