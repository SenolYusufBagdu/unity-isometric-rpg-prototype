using UnityEngine;

/// <summary>
/// ArrowCounter.cs → Player GameObject'ine ekle
///
/// - Oyun başlarken 10 ok verir
/// - Ok atılınca UseArrow() çağrılır → azalır
/// - Yerden ok toplandığında Inventory üzerinden otomatik güncellenir
/// - ArrowHUD bu scripti dinler
///
/// KURULUM:
/// 1. Player'a ekle
/// 2. PlayerAttack Inspector'ında arrowItemData alanına Arrow asset'ini sürükle
///    VEYA bu scriptteki arrowItemData alanına sürükle
/// </summary>
public class ArrowCounter : MonoBehaviour
{
    public static ArrowCounter Instance { get; private set; }

    [Header("Arrow Item")]
    public ItemData arrowItemData;   // Arrow ItemData asset'ini sürükle

    [Header("Başlangıç")]
    public int startingArrows = 10;

    // Ok sayısı değişince UI güncelle
    public System.Action<int> OnArrowCountChanged;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;
    }

    void Start()
    {
        // Başlangıçta 10 ok envantere ekle
        if (arrowItemData != null && Inventory.Instance != null)
        {
            Inventory.Instance.AddItem(arrowItemData, startingArrows);
            Debug.Log($"🏹 Başlangıç: {startingArrows} ok envantere eklendi");
        }

        // Envanter değişimini dinle
        if (Inventory.Instance != null)
            Inventory.Instance.OnInventoryChanged += NotifyHUD;
    }

    void OnDestroy()
    {
        if (Inventory.Instance != null)
            Inventory.Instance.OnInventoryChanged -= NotifyHUD;
    }

    void NotifyHUD()
    {
        OnArrowCountChanged?.Invoke(GetArrowCount());
    }

    /// Mevcut ok sayısını döndür
    public int GetArrowCount()
    {
        if (Inventory.Instance == null || arrowItemData == null) return 0;
        return Inventory.Instance.GetItemCount(arrowItemData);
    }

    /// Ok atılınca PlayerAttack bu metodu çağırır
    public bool UseArrow()
    {
        if (GetArrowCount() <= 0)
        {
            Debug.Log("❌ Ok bitti!");
            return false;
        }

        Inventory.Instance.RemoveItem(arrowItemData, 1);
        Debug.Log($"🏹 Ok kullanıldı. Kalan: {GetArrowCount()}");
        return true;
    }
}