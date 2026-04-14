using UnityEngine;

public class SkillTreeSlot : MonoBehaviour
{
    public bool unlocked = false;
    public bool IsEmpty() => transform.childCount == 0;


    // place an skill
    public void SetItemInstance(MonoBehaviour item)
    {
        if (item == null) return;
        item.transform.SetParent(transform, false);
        item.transform.localPosition = Vector3.zero;
        var skill = item as SkillItem;
        if (skill != null) skill.parentAfterDrag = transform;
    }

    void Update()
    {
        if (!unlocked && transform.Find("UnlockButton") == null)
        {
            unlocked = true;
        }
    }
}
