using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

public class SkillTreeManager : MonoBehaviour
{
    public GameObject skillTreePanel;
    public Button closeSkillTreeButton;
    public Button openSkillTreeButton;
    public TextMeshProUGUI skillPointsText;
    public GameObject[] blockingPanels;

    [Header("Slots (assign in inspector)")]
    public SkillTreeSlot[] treeSlots;

    [Header("Skill item UI prefab (must have SkillItem)")]
    public GameObject skillItemPrefab;

    [Header("Ability prefab lists (assign ability PREFABS here)")]
    public GameObject[] warriorAbilityPrefabs;
    public GameObject[] mageAbilityPrefabs;
    public GameObject[] rogueAbilityPrefabs;

    public bool populateOnStart = true;

    void Start()
    {
        if (populateOnStart) PopulateSkillTreeFromPlayer();
    }

    void Update()
    {
        if (Keyboard.current.pKey.wasPressedThisFrame)
        {
            if (skillTreePanel != null && skillTreePanel.activeSelf)
                closeSkillTreeButton?.onClick.Invoke();
            else
            {
                if (IsAnyBlockingPanelOpen()) return;
                openSkillTreeButton?.onClick.Invoke();
            }
        }

        if (skillPointsText != null)
        {
            var stats = FindFirstObjectByType<PlayerStats>();
            if (stats != null) skillPointsText.text = "Skill Points: " + stats.skillPoints;
        }
    }

    bool IsAnyBlockingPanelOpen()
    {
        if (blockingPanels == null) return false;
        foreach (var g in blockingPanels) if (g != null && g.activeInHierarchy) return true;
        return false;
    }

    public void PopulateSkillTreeFromPlayer()
    {
        var player = FindFirstObjectByType<PlayerStats>();
        if (player == null) return;

        var playerClass = player.GetClass();

        GameObject[] list;
        if (playerClass == ClassListData.UserClasses.Warrior)
            list = warriorAbilityPrefabs;
        else if (playerClass == ClassListData.UserClasses.Mage)
            list = mageAbilityPrefabs;
        else if (playerClass == ClassListData.UserClasses.Rogue)
            list = rogueAbilityPrefabs;
        else
            list = mageAbilityPrefabs; // fallback

        PopulateWithAbilityPrefabs(list);
    }


    public void PopulateWithAbilityPrefabs(GameObject[] abilityPrefabs)
    {
        if (skillItemPrefab == null || treeSlots == null || treeSlots.Length == 0) return;
        if (abilityPrefabs == null || abilityPrefabs.Length == 0) return;

        int idx = 0;
        for (int i = 0; i < treeSlots.Length && idx < abilityPrefabs.Length; i++)
        {
            var slot = treeSlots[i];
            if (slot == null) continue;

            var abilityPrefab = abilityPrefabs[idx++];
            if (abilityPrefab == null) continue;

            var inst = Instantiate(skillItemPrefab, slot.transform);
            inst.transform.localPosition = Vector3.zero;
            inst.transform.localScale = Vector3.one;
            inst.transform.SetAsFirstSibling();
            inst.name = $"SkillItem - {abilityPrefab.name}";

            var skillItem = inst.GetComponent<SkillItem>();
            if (skillItem == null) continue;

            // directly set the ability prefab on the UI item
            skillItem.Initialise(abilityPrefab,skillItem.skillType);

        }
    }

    public SkillTreeSlot GetFirstEmptySlot()
    {
        if (treeSlots == null) return null;
        foreach (var s in treeSlots) if (s != null && s.IsEmpty()) return s;
        return null;
    }
}
