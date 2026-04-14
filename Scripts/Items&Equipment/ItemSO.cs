using UnityEngine;

[CreateAssetMenu(menuName = "Items/ItemSO", fileName = "NewItemSO")]
public class ItemSO : ScriptableObject
{
    [Header("Information")]
    public string itemName;
    public Sprite icon;
    [TextArea] public string description;

    [Header("Shop price")]
    public int shopBuyPrice = 0;
    public int shopSellPrice = 0;

    [Header("Stacking")]
    public bool stackable = true;
    public int maxStack = 99;

    [Header("Consumable")]
    public bool consumable = true;

    [Header("Health and Mana Restoration")]
    public float restoreHealth = 0;
    public float restoreMana = 0;
    public float restoreArmor = 0;

    [Header("Cooldown)")]
    public bool cooldownFlag = false;
    public float cooldown = 0f;
}
