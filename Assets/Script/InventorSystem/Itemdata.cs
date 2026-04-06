using UnityEngine;

/// <summary>
/// ItemData.cs — ScriptableObject
///
/// KURULUM:
/// Project penceresinde sağ tıkla →
/// Create → Inventory → Item Data
/// Her item için ayrı bir asset oluştur.
/// Örnek: "Sword", "Wooden Bow", "Arrow" asset'leri yap
/// </summary>
[CreateAssetMenu(fileName = "NewItem", menuName = "Inventory/Item Data")]
public class ItemData : ScriptableObject
{
    [Header("Temel Bilgi")]
    public string itemName = "Yeni Item";
    public Sprite icon;
    [TextArea] public string description = "";

    [Header("Tip")]
    public ItemType itemType = ItemType.Misc;

    [Header("Yığınlama")]
    public bool isStackable = false;
    public int maxStack = 99;

    [Header("Ekipman")]
    public EquipSlot equipSlot = EquipSlot.None;

    [Header("Dünya Prefabı")]
    public GameObject worldPrefab;
    [Range(0f, 1f)] public float dropChance = 0.5f;
}

public enum ItemType
{
    Sword,
    Bow,
    Arrow,
    Helmet,
    Chest,
    Misc
}

public enum EquipSlot
{
    None,
    Head,
    Chest,
    MainHand
}