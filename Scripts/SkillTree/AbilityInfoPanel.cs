using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class AbilityInfoPanel : MonoBehaviour
{
    [Header("UI - assign in inspector")]
    public Image icon;
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI descAndCooldownText;

    public void PopulateFromSkillItem(SkillItem skillItem)
    {
        // segurança
        if (skillItem == null)
        {
            Debug.LogWarning("[AbilityInfoPanel] skillItem is null");
            gameObject.SetActive(false);
            return;
        }

        var prefab = skillItem.abilityPrefab;
        if (prefab == null)
        {
            Debug.LogWarning("[AbilityInfoPanel] skillItem.abilityPrefab is null");
            gameObject.SetActive(false);
            return;
        }

        var info = prefab.GetComponentInChildren<AbilityInfo>(true);
        if (info == null || info.abilitySO == null)
        {
            Debug.LogWarning("[AbilityInfoPanel] AbilityInfo or AbilitySO missing on prefab: " + prefab.name);
            gameObject.SetActive(false);
            return;
        }

        var so = info.abilitySO;

        // Icon
        if (icon != null && so.abilityIcon != null)
        {
            icon.sprite = so.abilityIcon;
            icon.enabled = true;
        }

        // Title
        if (titleText != null)
        {
            titleText.text = so.skillName;
        }

        // Description + cooldown
        if (descAndCooldownText != null)
        {
            string desc = string.IsNullOrEmpty(so.description) ? "" : so.description;
            string cd = so.cooldownFlag ? $"{so.cooldown:0.##}s cooldown" : $"No cooldown";
            // podes alterar o formato como preferires
            descAndCooldownText.text = string.IsNullOrEmpty(desc) ? cd : $"{desc}\n\n{cd}";
        }
    }
}
