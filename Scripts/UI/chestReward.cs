using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.VisualScripting;
using Handlers;

public class ChestReward : MonoBehaviour
{
    [Header("Buttons")]
    [SerializeField] private Button closeButton;

    [Header("References")]
    [SerializeField] private GameObject interactionUI;

    [Header("Gold settings")]
    [SerializeField] private int goldMin = 10;
    [SerializeField] private int goldMax = 50;
    [SerializeField] private int goldMultiplier = 1;

    [Header("Reward list (fill all slots in Inspector)")]
    [SerializeField] private EquipmentSO[] rewardEquip = new EquipmentSO[5];

    [Header("Roll settings")]
    [SerializeField] private int rollTier = 1;

    [Header("UI")]
    [SerializeField] private TMP_Text rewardText;

    void OnEnable()
    {
        rollTier = GameObject.FindGameObjectWithTag("GameController").GetComponent<GameController>().tier;

        if (rewardEquip == null || rewardEquip.Length == 0) return;

        // pick one item from the list
        int idx = Random.Range(0, rewardEquip.Length);
        EquipmentSO chosen = rewardEquip[idx];

        // roll equipment by tier
        EquipmentRoller roller = GetComponent<EquipmentRoller>();
        EquipmentSO finalEquip = roller != null ? roller.RollEquipment(rollTier) : chosen;
        if (finalEquip == null) return;

        // add to armory
        ArmoryManager armory = FindFirstObjectByType<ArmoryManager>();
        if (armory != null) armory.AddEquipment(finalEquip);

        // give gold once
        int rand = Random.Range(goldMin, goldMax + 1);
        int goldAmount = rand * Mathf.Max(1, goldMultiplier);

        PlayerStats stats = FindFirstObjectByType<PlayerStats>();
        if (stats != null) stats.GainGold(goldAmount);

        // update TMP text
        if (rewardText != null)
            rewardText.text = $"You received {goldAmount} gold!\nYou received new equipment: {finalEquip.itemName}";

        // close button
        if (closeButton != null)
        {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(ClosePanel);
        }
    }

    private void ClosePanel()
    {
        if (interactionUI != null) interactionUI.SetActive(true);
        gameObject.SetActive(false);
    }
}
