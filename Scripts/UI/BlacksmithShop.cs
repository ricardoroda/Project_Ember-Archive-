using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class BlacksmithShop : MonoBehaviour, IDropHandler
{
    public RectTransform slot;
    public Button addButton;
    public Button removeButton;
    public EquipmentRoller equipmentRoller;
    public PlayerStats playerStats;

    public int addCost = 200;
    public int removeCost = 100;
    public Vector2 centerOffset = Vector2.zero;

    void Awake()
    {
        addButton?.onClick.AddListener(OnAddPressed);
        removeButton?.onClick.AddListener(OnRemovePressed);
        if (equipmentRoller == null) equipmentRoller = FindFirstObjectByType<EquipmentRoller>();
        if (playerStats == null) playerStats = FindFirstObjectByType<PlayerStats>();
    }

    void OnDestroy()
    {
        addButton?.onClick.RemoveListener(OnAddPressed);
        removeButton?.onClick.RemoveListener(OnRemovePressed);
    }

    public void OnDrop(PointerEventData eventData)
    {
        var dragged = eventData.pointerDrag;
        if (dragged == null) { Shake(addButton); return; }

        var eqUI = dragged.GetComponent<EquipmentItem>();
        if (eqUI == null || eqUI.equipment == null) { Shake(addButton); return; }

        var current = GetCurrentEquipmentItem();

        if (current != null)
        {
            var currInfo = current.GetComponent<OriginalParentInfo>();
            Transform returnParent = null;

            if (currInfo != null && currInfo.originalParent != null)
                returnParent = currInfo.originalParent;
            else if (current.parentAfterDrag != null)
                returnParent = current.parentAfterDrag;
            else
                returnParent = current.transform.parent;

            if (returnParent != null)
            {
                current.transform.SetParent(returnParent, false);

                if (currInfo != null && currInfo.originalSiblingIndex >= 0)
                    current.transform.SetSiblingIndex(Mathf.Clamp(currInfo.originalSiblingIndex, 0, returnParent.childCount));
                else
                    current.transform.SetAsLastSibling();

                var cgOld = current.GetComponent<CanvasGroup>();
                if (cgOld != null) { cgOld.blocksRaycasts = true; cgOld.interactable = true; }

                if (currInfo != null) Object.Destroy(currInfo);
            }
        }

        var info = dragged.GetComponent<OriginalParentInfo>();
        if (info == null) info = dragged.AddComponent<OriginalParentInfo>();

        if (eqUI.parentAfterDrag != null && eqUI.parentAfterDrag != slot)
            info.originalParent = eqUI.parentAfterDrag;
        else
            info.originalParent = dragged.transform.parent;

        var ei = dragged.GetComponent<EquipmentItem>();
        info.originalSiblingIndex = (ei != null && ei.previousSiblingIndex >= 0) ? ei.previousSiblingIndex : dragged.transform.GetSiblingIndex();

        dragged.transform.SetParent(slot, false);
        var rect = dragged.GetComponent<RectTransform>();
        if (rect != null)
        {
            rect.anchoredPosition = centerOffset;
            rect.localScale = Vector3.one;
            rect.localRotation = Quaternion.identity;
            rect.SetAsLastSibling();
        }
        else dragged.transform.SetAsLastSibling();

        var cg = dragged.GetComponent<CanvasGroup>();
        if (cg != null) { cg.blocksRaycasts = true; cg.interactable = true; }
    }

    EquipmentItem GetCurrentEquipmentItem()
    {
        if (slot != null) return slot.GetComponentInChildren<EquipmentItem>();
        return GetComponentInChildren<EquipmentItem>();
    }

    void OnAddPressed()
    {
        var current = GetCurrentEquipmentItem();
        if (current == null) { Shake(addButton); return; }
        if (playerStats == null || playerStats.souls < addCost) { Shake(addButton); return; }
        if (equipmentRoller == null) { Shake(addButton); return; }

        // Try add; BlackSmithAddAffix rolls & appends a new EquipmentAffix to equipment.affixes
        if (!equipmentRoller.BlackSmithAddAffix(current.equipment)) { Shake(addButton); return; }

        // Apply the newly added affix's values to the equipment stats so UI sees the change immediately.
        var eq = current.equipment;
        if (eq != null && eq.affixes != null && eq.affixes.Count > 0)
        {
            var added = eq.affixes[eq.affixes.Count - 1]; // last added affix
            if (added != null && added.affixes != null)
            {
                foreach (var aff in added.affixes)
                {
                    if (aff == null) continue;
                    // SetAffixValuePair updates the equipment numeric fields and sets statString on the affix
                    eq.SetAffixValuePair(aff);
                }
            }
        }

        playerStats.SpendSouls(addCost);
        current.gameObject.SendMessage("RefreshUI", SendMessageOptions.DontRequireReceiver);

        // Update detail panel (works with original Show or newer RefreshShown)
        var detail = FindFirstObjectByType<ItemDetailPanel>();
        if (detail != null && detail.root != null && detail.root.activeSelf)
        {
            // prefer RefreshShown if present, otherwise call Show
            detail.gameObject.SendMessage("RefreshShown", SendMessageOptions.DontRequireReceiver);
            detail.Show(current.equipment);
        }
    }

    void OnRemovePressed()
    {
        var current = GetCurrentEquipmentItem();
        if (current == null) { Shake(removeButton); return; }
        if (playerStats == null || playerStats.souls < removeCost) { Shake(removeButton); return; }
        if (equipmentRoller == null) { Shake(removeButton); return; }

        if (!equipmentRoller.BlackSmithRemoveAffix(current.equipment)) { Shake(removeButton); return; }

        playerStats.SpendSouls(removeCost);
        current.gameObject.SendMessage("RefreshUI", SendMessageOptions.DontRequireReceiver);

        var detail = FindFirstObjectByType<ItemDetailPanel>();
        if (detail != null && detail.root != null && detail.root.activeSelf)
        {
            detail.gameObject.SendMessage("RefreshShown", SendMessageOptions.DontRequireReceiver);
            detail.Show(current.equipment);
        }
    }

    void Shake(Button b)
    {
        if (b == null) return;
        StartCoroutine(ShakeRect(b.GetComponent<RectTransform>(), 0.12f, 6f));
    }

    System.Collections.IEnumerator ShakeRect(RectTransform rt, float dur, float mag)
    {
        if (rt == null) yield break;
        Vector2 orig = rt.anchoredPosition;
        float t = 0f;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            rt.anchoredPosition = orig + Random.insideUnitCircle * mag * (1f - t / dur);
            yield return null;
        }
        rt.anchoredPosition = orig;
    }
}

public class OriginalParentInfo : MonoBehaviour
{
    public Transform originalParent;
    public int originalSiblingIndex = -1;
}
