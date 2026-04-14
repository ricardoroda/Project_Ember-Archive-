using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(EquipmentItem))]
public class EquipmentItemClickHandlerSimple : MonoBehaviour, IPointerClickHandler
{
    private EquipmentItem eqItem;
    private ItemDetailPanel panel;

    private void Awake()
    {
        eqItem = GetComponent<EquipmentItem>();
        panel = FindFirstObjectByType<ItemDetailPanel>();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        // only show details on left click
        if (eventData.button != PointerEventData.InputButton.Left) return;

        if (eqItem == null || eqItem.equipment == null || panel == null) return;

        panel.Show(eqItem.equipment);
    }
}
