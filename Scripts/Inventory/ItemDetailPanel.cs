using System.Text;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ItemDetailPanel : MonoBehaviour
{
    [Header("Root")] public GameObject root;
    [Header("Main")] public TMP_Text nameText; public Image iconImage;
    [Header("Single TMP for everything else")] public TMP_Text extraText;
    [Header("Styling (colors)")] public Color labelColor = new Color(.9f, .9f, .9f);
    public Color valueColor = new Color(1f, .84f, 0f);

    // runtime state
    private ItemSO shownItem;
    private EquipmentSO shownEq;
    private string labelHex;
    private string valueHex;

    void Awake()
    {
        labelHex = ColorUtility.ToHtmlStringRGB(labelColor);
        valueHex = ColorUtility.ToHtmlStringRGB(valueColor);
        if (root) root.SetActive(false);
    }

    void OnValidate()
    {
        labelHex = ColorUtility.ToHtmlStringRGB(labelColor);
        valueHex = ColorUtility.ToHtmlStringRGB(valueColor);
    }

    // Rebuild every frame while visible (robust)
    void Update()
    {
        if (shownEq != null) BuildEquipment(shownEq);
        else if (shownItem != null) BuildItem(shownItem);
    }

    // public API
    public void Show(ItemSO item)
    {
        if (item == null) { Hide(); return; }
        shownItem = item; shownEq = null;
        if (root) root.SetActive(true);
        BuildItem(item);
    }

    public void Show(EquipmentSO eq)
    {
        if (eq == null) { Hide(); return; }
        shownEq = eq; shownItem = null;
        if (root) root.SetActive(true);
        eq.CreateEquipmentStatDescription(); // allow SO to prepare its lists
        BuildEquipment(eq);
    }

    // call from external code if you prefer an explicit refresh after changes
    public void RefreshShown() { if (shownEq != null) { shownEq.CreateEquipmentStatDescription(); BuildEquipment(shownEq); } }

    public void Hide()
    {
        shownItem = null; shownEq = null;
        if (root) root.SetActive(false);
    }

    // builders
    void BuildItem(ItemSO it)
    {
        if (it == null) { Hide(); return; }
        if (nameText) nameText.text = string.IsNullOrEmpty(it.itemName) ? "No Name" : it.itemName;
        if (iconImage) iconImage.sprite = it.icon;

        var sb = new StringBuilder();
        sb.AppendLine(Pair("Buy Price:", it.shopBuyPrice.ToString()));
        sb.AppendLine(Pair("Sell Price:", it.shopSellPrice.ToString()));
        sb.AppendLine("-----------------------------");
        AppendIfPos(sb, it.restoreHealth, "Health");
        AppendIfPos(sb, it.restoreMana, "Mana");
        AppendIfPos(sb, it.restoreArmor, "Armor");
        if (it.cooldown > 0f) sb.AppendLine(Pair("Cooldown:", FormatF(it.cooldown) + "s"));

        // description last (so it moves down as stats grow)
        if (!string.IsNullOrEmpty(it.description))
        {
            sb.AppendLine("-----------------------------");
            sb.AppendLine(it.description.Trim());
        }

        if (extraText) extraText.text = sb.ToString().TrimEnd();
    }

    void BuildEquipment(EquipmentSO eq)
    {
        if (eq == null) { Hide(); return; }
        if (nameText) nameText.text = string.IsNullOrEmpty(eq.itemName) ? "No Name" : eq.itemName;
        if (iconImage) iconImage.sprite = eq.icon;

        // let SO build its own lists (safe to call repeatedly)
        eq.CreateEquipmentStatDescription();

        var sb = new StringBuilder();
        sb.AppendLine(Pair("Sell Price:", FormatF(eq.shopSellPrice)));

        var d = eq.equipmentStatDescription;
        if (d != null)
        {
            if (d.typesLabels != null && d.typesValues != null)
            {
                int n = Mathf.Min(d.typesLabels.Count, d.typesValues.Count);
                if (n > 0)
                {
                    sb.AppendLine("-----------------------------");
                    for (int i = 0; i < n; i++) sb.AppendLine(Pair(Trim(d.typesLabels[i]) + ":", d.typesValues[i]));
                }
            }

            if (d.coreStatsLabels != null && d.coreStatsValues != null)
            {
                int n = Mathf.Min(d.coreStatsLabels.Count, d.coreStatsValues.Count);
                if (n > 0)
                {
                    sb.AppendLine("-----------------------------");
                    for (int i = 0; i < n; i++) sb.AppendLine(Pair(Trim(d.coreStatsLabels[i]) + ":", d.coreStatsValues[i]));
                }
            }

            if (d.coreAffixes != null && d.coreAffixes.Count > 0)
            {
                sb.AppendLine("-----------------------------");
                foreach (var s in d.coreAffixes)
                    if (!string.IsNullOrEmpty(s))
                        foreach (var line in s.Replace("\r", "").Split('\n')) sb.AppendLine(ProcessAffixLine(line));
            }

            if (d.rarityAffixes != null && d.rarityAffixes.Count > 0)
            {
                sb.AppendLine("-----------------------------");
                foreach (var s in d.rarityAffixes)
                    if (!string.IsNullOrEmpty(s))
                        foreach (var line in s.Replace("\r", "").Split('\n')) sb.AppendLine("  <color=#" + valueHex + ">" + line.Trim() + "</color>");
            }
        }
        else
        {
            // fallback to old behaviour
            sb.AppendLine("-----------------------------");
            sb.AppendLine(Pair("Slot:", eq.equipSlot.ToString()));
            sb.AppendLine(Pair("Tier:", eq.tier.ToString()));
            sb.AppendLine(Pair("Rarity:", eq.rarity.ToString()));
            sb.AppendLine("-----------------------------");
            AppendIfPos(sb, eq.maxHealth, "Health");
            AppendIfPos(sb, eq.maxMana, "Mana");
            AppendIfPos(sb, eq.maxArmor, "Armor");
            AppendIfNonZero(sb, eq.strength, "Strength");
            AppendIfNonZero(sb, eq.dexterity, "Dexterity");
            AppendIfNonZero(sb, eq.intelligence, "Intelligence");
            AppendIfNonZero(sb, eq.vitality, "Vitality");

            if (eq is WeaponSO w)
            {
                sb.AppendLine("-----------------------------");
                sb.AppendLine(Pair("Weapon Type:", w.weaponType.ToString()));
                sb.AppendLine(Pair("Damage Type:", w.damageType.ToString()));
                AppendIfPos(sb, w.weaponDamage, "Damage");
                AppendIfPos(sb, w.weaponActionSpeed, "Action Speed");
                if (w.coreAffixStats != null)
                {
                    sb.AppendLine("-----------------------------");
                    foreach (var a in w.coreAffixStats) sb.AppendLine(FormatAffix(a));
                }
            }
            else if (eq is ArmorSO a)
            {
                sb.AppendLine("-----------------------------");
                sb.AppendLine(Pair("Armor Type:", a.armorType.ToString()));
                AppendIfPos(sb, a.coreArmor, "Core Armor");
                AppendIfPos(sb, a.coreArmorRegenRate, "Core Armor Regen");
                AppendIfPos(sb, a.coreArmorThreshold, "Core Armor Threshold");
                if (a.coreAffixStats != null)
                {
                    sb.AppendLine("-----------------------------");
                    foreach (var af in a.coreAffixStats) sb.AppendLine(FormatAffix(af));
                }
            }
            else if (eq is AccessorySO ac)
            {
                sb.AppendLine("-----------------------------");
                sb.AppendLine(Pair("Accessory Type:", ac.accessoryType.ToString()));
                if (ac.accessoryEffectID != 0) sb.AppendLine(Pair("Effect ID:", ac.accessoryEffectID.ToString()));
                if (ac.coreAffixStats != null)
                {
                    sb.AppendLine("-----------------------------");
                    foreach (var af in ac.coreAffixStats) sb.AppendLine(FormatAffix(af));
                }
            }

            if (eq.affixes != null && eq.affixes.Count > 0)
            {
                sb.AppendLine("-----------------------------");
                foreach (var ea in eq.affixes) if (ea != null)
                    foreach (var s in GetAffixArrayStrings(ea)) sb.AppendLine(s);
            }
        }

        // description last
        if (!string.IsNullOrEmpty(eq.description))
        {
            sb.AppendLine("-----------------------------");
            sb.AppendLine(eq.description.Trim());
        }

        if (extraText) extraText.text = sb.ToString().TrimEnd();
    }

    // helpers (short)
    string Pair(string lab, string val) => $"<b><color=#{labelHex}>{lab}</color></b> <color=#{valueHex}>{val}</color>";
    void AppendIfPos(StringBuilder sb, int v, string name) { if (v > 0) sb.AppendLine(Pair(name + ":", v.ToString())); }
    void AppendIfPos(StringBuilder sb, float v, string name) { if (v > 0f) sb.AppendLine(Pair(name + ":", FormatF(v))); }
    void AppendIfNonZero(StringBuilder sb, int v, string name) { if (v != 0) sb.AppendLine(Pair(name + ":", v > 0 ? "+" + v.ToString() : v.ToString())); }

    string FormatAffix(AffixValuePair a) { if (a == null) return ""; var s = string.IsNullOrEmpty(a.statString) ? (FormatF((float)a.value) + " " + a.statType) : a.statString.Trim(); return "  <color=#" + valueHex + ">" + s + "</color>"; }

    System.Collections.Generic.List<string> GetAffixArrayStrings(object o)
    {
        var list = new System.Collections.Generic.List<string>();
        if (o is EquipmentAffix ea && ea.affixes != null)
            foreach (var a in ea.affixes) if (a != null) list.Add(FormatAffix(a));
        else list.Add(o?.ToString() ?? "");
        return list;
    }

    string ProcessAffixLine(string s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        int c = s.IndexOf(':');
        if (c >= 0) return Pair(s.Substring(0, c).Trim() + ":", s.Substring(c + 1).Trim());
        var t = s.Trim().Split(' ');
        if (t.Length >= 2)
        {
            if ((t[0] == "+" || t[0] == "-") && t.Length >= 2) return Pair(string.Join(" ", t, 2, t.Length - 2).Trim() + ":", t[0] + " " + t[1]);
            if (t[0].StartsWith("+") || t[0].StartsWith("-") || LooksNumeric(t[0])) return Pair(string.Join(" ", t, 1, t.Length - 1).Trim() + ":", t[0]);
            var last = t[t.Length - 1];
            if (last.StartsWith("+") || last.StartsWith("-") || last.EndsWith("%") || LooksNumeric(last)) return Pair(string.Join(" ", t, 0, t.Length - 1).Trim() + ":", last);
        }
        return "  <color=#" + valueHex + ">" + s.Trim() + "</color>";
    }

    bool LooksNumeric(string tok)
    {
        if (string.IsNullOrEmpty(tok)) return false;
        var c = tok.Trim().TrimEnd('%');
        return float.TryParse(c, System.Globalization.NumberStyles.Float | System.Globalization.NumberStyles.AllowThousands, CultureInfo.CurrentCulture, out _) ||
               float.TryParse(c, System.Globalization.NumberStyles.Float | System.Globalization.NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out _);
    }

    string Trim(string s) => (s ?? "").Trim().TrimEnd(':');
    string FormatF(float v) => Mathf.Approximately(v, Mathf.Round(v)) ? Mathf.RoundToInt(v).ToString() : v.ToString("0.0");
}
