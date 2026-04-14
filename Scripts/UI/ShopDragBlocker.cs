using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class ShopDragBlocker :
    MonoBehaviour,
    IBeginDragHandler, IDragHandler, IEndDragHandler,
    IPointerClickHandler, IPointerDownHandler, IPointerUpHandler,
    IPointerEnterHandler, IPointerExitHandler
{
    // Alvo para receber os eventos (normalmente o InventoryItem ou o seu GameObject)
    public GameObject forwardTarget;
    public InventoryItem targetInventoryItem; // opcional (retrocompat)

    void Awake()
    {
        var img = GetComponent<Image>() ?? gameObject.AddComponent<Image>();
        img.color = new Color(0f, 0f, 0f, 0f);
        img.raycastTarget = true;
        var cg = GetComponent<CanvasGroup>();
        if (cg) cg.blocksRaycasts = true;
    }

    GameObject TargetGO =>
        forwardTarget != null ? forwardTarget :
        targetInventoryItem != null ? targetInventoryItem.gameObject : null;

    // ----- bloquear drag -----
    public void OnBeginDrag(PointerEventData e) { e.Use(); }
    public void OnDrag(PointerEventData e)      { e.Use(); }
    public void OnEndDrag(PointerEventData e)   { e.Use(); }

    // ----- encaminhar todos os eventos de pointer -----
    public void OnPointerClick(PointerEventData e)  => Forward(e, ExecuteEvents.pointerClickHandler);
    public void OnPointerDown(PointerEventData e)   => Forward(e, ExecuteEvents.pointerDownHandler);
    public void OnPointerUp(PointerEventData e)     => Forward(e, ExecuteEvents.pointerUpHandler);
    public void OnPointerEnter(PointerEventData e)  => Forward(e, ExecuteEvents.pointerEnterHandler);
    public void OnPointerExit(PointerEventData e)   => Forward(e, ExecuteEvents.pointerExitHandler);

    void Forward<T>(PointerEventData e, ExecuteEvents.EventFunction<T> fn) where T : IEventSystemHandler
    {
        var go = TargetGO;
        if (go == null) return;

        // envia para o alvo e, se preciso, para os pais (equivalente a “hierarchy bubble”)
        ExecuteEvents.ExecuteHierarchy(go, e, fn);

        // consumimos para não “clicar” mais nada por baixo
        e.Use();
    }
}
