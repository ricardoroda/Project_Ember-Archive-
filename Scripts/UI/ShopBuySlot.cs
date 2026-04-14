using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ShopBuySlot : MonoBehaviour
{
    public Button buyButton;
    public TextMeshProUGUI priceTextUI;
    public ItemSO shopItem;        
    const string OverlayName = "ShopDragBlockerOverlay";

    InventoryManager invManager;
    PlayerStats playerStats;
    InventoryItem childInventoryItem;
    bool overlayBound = false;

    static readonly Vector2 FixedAnchoredPos = new Vector2(-60f, 0f);

    Coroutine shakeCoroutine;

    void Start()
    {
        invManager = FindFirstObjectByType<InventoryManager>();
        playerStats = FindFirstObjectByType<PlayerStats>();
        if (buyButton != null) buyButton.onClick.AddListener(OnBuyButtonPressed);

        RefreshChild();
        InitChildAndForcePosition();
        BindOverlayIfPresent();
        UpdateUI();
    }

    void Update()
    {
        RefreshChild();
        InitChildAndForcePosition();
        if (!overlayBound) BindOverlayIfPresent();
        UpdateUI();
    }

    void RefreshChild()
    {
        if (childInventoryItem != null && childInventoryItem.transform.IsChildOf(transform)) return;
        childInventoryItem = GetComponentInChildren<InventoryItem>(true);
    }

    void InitChildAndForcePosition()
    {
        if (childInventoryItem == null) return;

        if (childInventoryItem.item == null && shopItem != null)
            childInventoryItem.InitialiseItem(shopItem, 1);

        childInventoryItem.transform.SetParent(this.transform, false);
        childInventoryItem.transform.localScale = Vector3.one;
        childInventoryItem.parentAfterDrag = this.transform;

        var rt = childInventoryItem.GetComponent<RectTransform>();
        if (rt != null)
        {
            rt.anchoredPosition = FixedAnchoredPos;
            rt.localRotation = Quaternion.identity;
        }
        else
        {
            childInventoryItem.transform.localPosition = new Vector3(FixedAnchoredPos.x, FixedAnchoredPos.y, 0f);
            childInventoryItem.transform.localRotation = Quaternion.identity;
        }

        if (childInventoryItem.icon != null)
        {
            if (childInventoryItem.item != null && childInventoryItem.item.icon != null)
            {
                childInventoryItem.icon.sprite = childInventoryItem.item.icon;
                childInventoryItem.icon.enabled = true;
            }
        }
        else
        {
            Debug.LogWarning($"[ShopBuySlot] InventoryItem '{childInventoryItem.name}' tem o campo 'icon' null no prefab.");
        }

        childInventoryItem.UpdateCountText();
    }

    void BindOverlayIfPresent()
    {
        if (overlayBound) return;
        if (childInventoryItem == null) RefreshChild();

        Transform overlayT = null;


        if (childInventoryItem != null)
            overlayT = childInventoryItem.transform.Find(OverlayName);

        if (overlayT == null)
            overlayT = transform.Find(OverlayName);

        if (overlayT == null)
        {
            Debug.LogWarning($"[ShopBuySlot] Overlay not found");
            return;
        }

        if (childInventoryItem != null)
            overlayT.SetParent(childInventoryItem.transform, false);
        else
            overlayT.SetParent(this.transform, false);

        var rt = overlayT as RectTransform;
        if (rt != null)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;
            rt.anchoredPosition = Vector2.zero;
        }

        var img = overlayT.GetComponent<Image>() ?? overlayT.gameObject.AddComponent<Image>();
        img.raycastTarget = true;
        img.color = new Color(img.color.r, img.color.g, img.color.b, 0f);

        var blocker = overlayT.GetComponent<ShopDragBlocker>() ?? overlayT.gameObject.AddComponent<ShopDragBlocker>();
        blocker.forwardTarget = childInventoryItem != null ? childInventoryItem.gameObject : null;
        blocker.targetInventoryItem = childInventoryItem;

        overlayT.SetAsLastSibling();

        overlayBound = true;

        Debug.Log($"[ShopBuySlot] Overlay bound for slot '{name}' (overlay parent: {overlayT.parent.name})");
    }

    int GetPrice()
    {
        if (childInventoryItem == null || childInventoryItem.item == null) return 0;
        return childInventoryItem.item.shopBuyPrice;
    }

    public void UpdateUI()
    {
        int price = GetPrice();
        if (priceTextUI != null) priceTextUI.text = price > 0 ? price.ToString() : "-";
    }

    void OnBuyButtonPressed()
    {
        if (childInventoryItem == null || childInventoryItem.item == null) { Debug.LogWarning("ShopBuySlot: no item"); return; }
        if (invManager == null || playerStats == null) { Debug.LogWarning("ShopBuySlot: missing refs"); return; }

        int price = GetPrice();
        if (playerStats.gold < price)
        {
            // Sem ouro -> feedback visual: shake o botão
            if (shakeCoroutine != null) StopCoroutine(shakeCoroutine);
            shakeCoroutine = StartCoroutine(ShakeButtonCoroutine(buyButton.transform as RectTransform, 0.45f, 10f));
            // opcional: tocar som de erro aqui
            return;
        }

        if (!invManager.AddItem(childInventoryItem.item, 1))
        {
            Debug.Log("ShopBuySlot: failed to add item to inventory (maybe full).");
            if (shakeCoroutine != null) StopCoroutine(shakeCoroutine);
            shakeCoroutine = StartCoroutine(ShakeButtonCoroutine(buyButton.transform as RectTransform, 0.45f, 6f));
            return;
        }

        // only spend gold if the item was actually added
        playerStats.SpendGold(price);
        UpdateUI();
        Debug.Log($"ShopBuySlot: Purchased 1x {childInventoryItem.item.itemName} for {price} gold.");
    }

    // Shake
    IEnumerator ShakeButtonCoroutine(RectTransform rt, float duration, float magnitude)
    {
        if (rt == null) yield break;
        Vector2 original = rt.anchoredPosition;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            float damper = 1f - (elapsed / duration);
            float x = (Random.value * 2f - 1f) * magnitude * damper * 0.5f;
            float y = (Random.value * 2f - 1f) * magnitude * damper * 0.5f;
            rt.anchoredPosition = original + new Vector2(x, y);
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
        rt.anchoredPosition = original;
        shakeCoroutine = null;
    }

    void OnDestroy() { if (buyButton != null) buyButton.onClick.RemoveListener(OnBuyButtonPressed); }
}
