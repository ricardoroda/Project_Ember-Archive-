using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using Unity.VisualScripting;

[RequireComponent(typeof(CanvasGroup))]
public class SkillItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
{
    public enum SkillType { Normal, Dash }

    [HideInInspector] public Transform parentAfterDrag;

    [Header("Ability")]
    public GameObject abilityPrefab;
    public SkillType skillType = SkillType.Normal;

    [Header("UI")]
    public Image icon;
    public TextMeshProUGUI manaTextUI;

    [HideInInspector] public GameObject draggingInstance;
    private CanvasGroup cg;

    void Awake()
    {
        cg = GetComponent<CanvasGroup>();
        if (cg == null) cg = gameObject.AddComponent<CanvasGroup>();
        parentAfterDrag = transform.parent;
        if (manaTextUI != null) manaTextUI.text = "100";
    }

    public void Start()
    {
        Initialise(abilityPrefab, skillType);
    }

    Transform GetDragRoot()
    {
        var cv = GetComponentInParent<Canvas>();
        return cv != null ? cv.transform : transform.root;
    }

    // ---------------- Drag & Drop ----------------
    public void OnBeginDrag(PointerEventData eventData)
    {
        var treeSlot = GetComponentInParent<SkillTreeSlot>();
        Transform dragRoot = GetDragRoot();

        if (treeSlot != null)
        {
            // drag from skill tree: create a visual clone to drag while original stays in tree
            var clone = Instantiate(gameObject, dragRoot);
            var cloneCg = clone.GetComponent<CanvasGroup>();
            if (cloneCg != null) cloneCg.blocksRaycasts = false; // clone is only visual
            var cloneSkill = clone.GetComponent<SkillItem>();
            if (cloneSkill != null) cloneSkill.parentAfterDrag = treeSlot.transform;
            clone.transform.SetAsLastSibling();

            draggingInstance = clone;
            // dim original
            cg.alpha = 0.6f;
        }
        else
        {
            // drag from action bar (move the actual object)
            draggingInstance = gameObject;
            parentAfterDrag = transform.parent;
            cg.blocksRaycasts = false; // allow raycasts to hit drop targets
            cg.alpha = 0.6f;
            transform.SetParent(dragRoot, true);
            transform.SetAsLastSibling();
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (draggingInstance != null)
            draggingInstance.transform.position = Input.mousePosition;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        // find possible ActionBarSlot under pointer
        GameObject rayGo = eventData.pointerCurrentRaycast.gameObject;
        if (rayGo == null) rayGo = eventData.pointerEnter;
        ActionBarSlot dropSlot = rayGo != null ? rayGo.GetComponentInParent<ActionBarSlot>() : null;

        // If we were dragging a clone (from tree)
        if (draggingInstance != null && draggingInstance != gameObject)
        {
            // If dropped onto a slot, call the ActionBar handler to accept it
            if (dropSlot != null && dropSlot.owner != null)
            {
                dropSlot.owner.HandleDrop(dropSlot, draggingInstance);
            }

            // If clone still parented to drag root => not accepted -> destroy clone
            if (draggingInstance != null && draggingInstance.transform.parent == GetDragRoot())
            {
                Destroy(draggingInstance);
            }
            else if (draggingInstance != null)
            {
                // accepted -> make sure it snaps correctly
                draggingInstance.transform.localPosition = Vector3.zero;
            }

            // restore original visuals
            cg.alpha = 1f;
            draggingInstance = null;
            return;
        }

        // If we were dragging the real object (from action bar)
        if (draggingInstance == gameObject)
        {
            // If dropped onto a slot, call the ActionBar handler to accept it
            if (dropSlot != null && dropSlot.owner != null)
            {
                dropSlot.owner.HandleDrop(dropSlot, this.gameObject);
            }

            // restore original behaviour and parent if needed
            cg.blocksRaycasts = true;
            cg.alpha = 1f;

            if (transform.parent == GetDragRoot() && parentAfterDrag != null)
            {
                transform.SetParent(parentAfterDrag, false);
                transform.localPosition = Vector3.zero;
            }
            else
            {
                transform.localPosition = Vector3.zero;
            }

            draggingInstance = null;
        }
    }

    // ---------------- Quick-Equip / Unequip ----------------
    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Right) return;

        var treeSlot = GetComponentInParent<SkillTreeSlot>();
        if (treeSlot != null)
        {
            var ab = FindFirstObjectByType<ActionBar>();
            if (ab != null)
                ab.EquipFromTreeShortcut(treeSlot, this);
            else
                Debug.LogWarning("[SkillItem] ActionBar not found for quick-equip.");
            return;
        }

        var actionSlot = GetComponentInParent<ActionBarSlot>();
        if (actionSlot != null)
        {
            // prevent unequip if cooldown overlay present
            foreach (Transform c in actionSlot.transform)
                if (c != null && c.name != null && c.name.StartsWith("CooldownOverlay"))
                    return;

            var ab = FindAnyObjectByType<ActionBar>();
            if (ab != null) ab.UnbindSlot(actionSlot.slotIndex);
            Destroy(gameObject); // remove runtime copy
        }
    }

    public void Initialise(GameObject abilityPrefab, SkillType type, Sprite iconSprite = null)
    {
        this.abilityPrefab = abilityPrefab;
        this.skillType = type;
        parentAfterDrag = transform.parent;

        if (icon == null) return;

        if (iconSprite != null)
        {
            icon.sprite = iconSprite;
            icon.enabled = true;
            return;
        }

        if (abilityPrefab == null) return;

        var info = abilityPrefab.GetComponentInChildren<AbilityInfo>(true);
        if (info == null || info.abilitySO == null || info.abilitySO.abilityIcon == null) return;

        icon.sprite = info.abilitySO.abilityIcon;
        icon.enabled = true;

        // Skill type (mainly Dash)
        this.skillType = (SkillType)(int)info.abilitySO.skillType;

        if (manaTextUI != null)
        {
            manaTextUI.text = info.abilitySO.manaCost.ToString();
        }
    }
}
