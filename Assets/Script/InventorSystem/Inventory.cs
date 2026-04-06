using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Inventory.cs → Player GameObject'ine ekle
///
/// Tüm envanter mantığını yönetir.
/// InventoryUI bu scripti dinler ve UI'ı günceller.
/// </summary>
public class Inventory : MonoBehaviour
{
    public static Inventory Instance { get; private set; }

    [Header("Ayarlar")]
    public int slotCount = 24;

    // Envanter slotları
    public List<InventorySlot> slots = new List<InventorySlot>();

    // Ekipman slotları
    public InventorySlot headSlot = new InventorySlot();
    public InventorySlot chestSlot = new InventorySlot();
    public InventorySlot mainHandSlot = new InventorySlot();

    // Olaylar — InventoryUI dinler
    public System.Action OnInventoryChanged;
    public System.Action OnEquipmentChanged;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        // Slotları başlat
        slots.Clear();
        for (int i = 0; i < slotCount; i++)
            slots.Add(new InventorySlot());
    }

    // ─────────────────────────────────────────────────────────
    // ITEM EKLEME
    // ─────────────────────────────────────────────────────────

    /// Item ekle — başarılıysa true döner
    public bool AddItem(ItemData item, int amount = 1)
    {
        if (item == null) return false;

        // Stackable ise mevcut slota ekle
        if (item.isStackable)
        {
            foreach (var slot in slots)
            {
                if (slot.item == item && slot.amount < item.maxStack)
                {
                    int canAdd = item.maxStack - slot.amount;
                    int adding = Mathf.Min(canAdd, amount);
                    slot.amount += adding;
                    amount -= adding;
                    if (amount <= 0)
                    {
                        OnInventoryChanged?.Invoke();
                        Debug.Log($"📦 {item.itemName} x{adding} eklendi (mevcut slota)");
                        return true;
                    }
                }
            }
        }

        // Boş slot bul
        foreach (var slot in slots)
        {
            if (slot.item == null)
            {
                slot.item = item;
                slot.amount = amount;
                OnInventoryChanged?.Invoke();
                Debug.Log($"📦 {item.itemName} x{amount} eklendi (yeni slot)");
                return true;
            }
        }

        Debug.Log("❌ Envanter dolu!");
        return false;
    }

    // ─────────────────────────────────────────────────────────
    // ITEM ÇIKARMA
    // ─────────────────────────────────────────────────────────

    public bool RemoveItem(ItemData item, int amount = 1)
    {
        foreach (var slot in slots)
        {
            if (slot.item == item)
            {
                slot.amount -= amount;
                if (slot.amount <= 0)
                {
                    slot.item = null;
                    slot.amount = 0;
                }
                OnInventoryChanged?.Invoke();
                return true;
            }
        }
        return false;
    }

    // ─────────────────────────────────────────────────────────
    // EKİPMAN
    // ─────────────────────────────────────────────────────────

    public void EquipItem(ItemData item)
    {
        if (item == null || item.equipSlot == EquipSlot.None) return;

        InventorySlot targetSlot = GetEquipSlot(item.equipSlot);
        if (targetSlot == null) return;

        // Eski ekipmanı envantere geri koy
        if (targetSlot.item != null)
            AddItem(targetSlot.item, 1);

        // Envanterdeki bu itemi çıkar
        RemoveItem(item, 1);

        // Ekipman slotuna koy
        targetSlot.item = item;
        targetSlot.amount = 1;

        OnEquipmentChanged?.Invoke();
        OnInventoryChanged?.Invoke();
        Debug.Log($"⚔️ {item.itemName} kuşanıldı ({item.equipSlot})");
    }

    public void UnequipSlot(EquipSlot slot)
    {
        InventorySlot equipSlot = GetEquipSlot(slot);
        if (equipSlot == null || equipSlot.item == null) return;

        if (AddItem(equipSlot.item, 1))
        {
            equipSlot.item = null;
            equipSlot.amount = 0;
            OnEquipmentChanged?.Invoke();
            OnInventoryChanged?.Invoke();
            Debug.Log($"📤 {slot} slotu boşaltıldı");
        }
    }

    InventorySlot GetEquipSlot(EquipSlot slot)
    {
        switch (slot)
        {
            case EquipSlot.Head: return headSlot;
            case EquipSlot.Chest: return chestSlot;
            case EquipSlot.MainHand: return mainHandSlot;
            default: return null;
        }
    }

    public bool HasItem(ItemData item)
    {
        foreach (var slot in slots)
            if (slot.item == item && slot.amount > 0) return true;
        return false;
    }

    public int GetItemCount(ItemData item)
    {
        int total = 0;
        foreach (var slot in slots)
            if (slot.item == item) total += slot.amount;
        return total;
    }
}


[System.Serializable]
public class InventorySlot
{
    public ItemData item;
    public int amount;

    public bool IsEmpty => item == null;
}