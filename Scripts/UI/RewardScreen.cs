using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using Handlers;

public class RewardScreen : MonoBehaviour
{
    [Header("References")]
    public Button continueButton;

    [Header("UI Text")]
    public TMP_Text rewardText;

    [Header("Reward Settings")]
    public int minXP = 100;
    public int maxXP = 300;
    public int xpMultiplier = 1;
    public int goldMin = 10;
    public int goldMax = 50;
    public int goldMultiplier = 1;
    public int soulsMin = 100;
    public int soulsMax = 250;
    public int soulsMultiplier = 1;
    [SerializeField] private EquipmentSO[] rewardEquip = new EquipmentSO[5];
    [SerializeField] private int rollTier = 1;

    private int rewardXP;


    void OnEnable()
    {
        SetupContinueButton();
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

        //give xp 
        int rand2 = Random.Range(minXP, maxXP + 1);
        int xpAmount = rand2 * Mathf.Max(1, xpMultiplier);

        //give souls
        int rand3 = Random.Range(soulsMin, soulsMax + 1);
        int soulsAmount = rand3 * Mathf.Max(1, soulsMultiplier);

        PlayerStats stats = FindFirstObjectByType<PlayerStats>();
        if (stats != null) stats.GainGold(goldAmount);
        stats.GainXP(xpAmount);
        stats.GainSouls(soulsAmount);

        // update TMP text
        if (rewardText != null)
            rewardText.text = $"You received {goldAmount} gold! \nYou received {xpAmount} XP! \nYou received {xpAmount} souls! \nYou received new equipment: {finalEquip.itemName}";


    }

    private void SetupContinueButton()
    {
        if (continueButton == null)
        {
            Debug.LogError("RewardScreen: continueButton not assigned", this);
            return;
        }
        continueButton.onClick.RemoveAllListeners();
        continueButton.onClick.AddListener(() => {
            AudioManager.Instance.PlaySfx(SfxType.Button);
            try { ProjectEmber.Loading.SceneLoader.Load(1); } catch { SceneManager.LoadScene(1); }
        });
    }
}


