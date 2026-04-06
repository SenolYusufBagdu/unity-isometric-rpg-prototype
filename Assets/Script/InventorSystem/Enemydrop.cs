using UnityEngine;

/// <summary>
/// EnemyDrop.cs → Enemy GameObject'ine ekle (EnemyHealth ile aynı obje)
///
/// KURULUM:
/// 1. Bu scripti EnemyHealth olan objeye ekle
/// 2. possibleDrops listesine ItemData asset'lerini ekle
/// 3. Her item için dropChance ItemData'da ayarlı (0-1 arası)
///
/// NOT: EnemyHealth.cs'e dokunmaz — Die() metodunu dinlemek için
/// EnemyHealth'in Update'ini izler.
/// </summary>
public class EnemyDrop : MonoBehaviour
{
    [Header("Düşecek İtemlar")]
    public ItemData[] possibleDrops;

    [Header("Ayarlar")]
    public float dropRadius = 0.8f;       // İtemlerin etrafa saçılma yarıçapı
    public float dropUpForce = 2f;        // Yukarı fırlatma kuvveti
    public bool dropToInventory = true;   // true = direkt envantere ekle, false = yere düş

    private EnemyHealth enemyHealth;
    private bool hasDropped = false;

    void Start()
    {
        enemyHealth = GetComponent<EnemyHealth>();
        if (enemyHealth == null)
            Debug.LogWarning("⚠️ EnemyDrop: EnemyHealth bulunamadı!");
    }

    void Update()
    {
        // EnemyHealth ölünce drop yap
        if (!hasDropped && enemyHealth != null && enemyHealth.isDead)
        {
            hasDropped = true;
            Drop();
        }
    }

    void Drop()
    {
        if (possibleDrops == null || possibleDrops.Length == 0) return;

        foreach (ItemData item in possibleDrops)
        {
            if (item == null) continue;

            // Şans kontrolü
            if (Random.value > item.dropChance) continue;

            if (dropToInventory && Inventory.Instance != null)
            {
                // Direkt envantere ekle
                int dropAmount = item.isStackable ? Random.Range(1, 4) : 1;
                Inventory.Instance.AddItem(item, dropAmount);
                Debug.Log($"💰 DÜŞMAN DROP (envanter): {item.itemName} x{dropAmount}");
            }
            else if (item.worldPrefab != null)
            {
                // Yere düşür
                SpawnWorldItem(item);
            }
        }
    }

    void SpawnWorldItem(ItemData item)
    {
        // Rastgele konum (düşmanın etrafına saç)
        Vector2 randomCircle = Random.insideUnitCircle * dropRadius;
        Vector3 spawnPos = transform.position +
                           new Vector3(randomCircle.x, 0.5f, randomCircle.y);

        GameObject dropped = Instantiate(item.worldPrefab, spawnPos, Random.rotation);

        // ItemPickup ekle
        ItemPickup pickup = dropped.GetComponent<ItemPickup>();
        if (pickup == null) pickup = dropped.AddComponent<ItemPickup>();
        pickup.itemData = item;
        pickup.amount = item.isStackable ? Random.Range(1, 4) : 1;

        // Collider yoksa ekle
        if (dropped.GetComponent<Collider>() == null)
        {
            SphereCollider sc = dropped.AddComponent<SphereCollider>();
            sc.isTrigger = true;
            sc.radius = 1.5f;
        }

        // Küçük zıplama efekti
        Rigidbody rb = dropped.GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = dropped.AddComponent<Rigidbody>();
            rb.useGravity = true;
        }
        rb.AddForce(Vector3.up * dropUpForce + Random.insideUnitSphere * 1.5f, ForceMode.Impulse);

        // 2 saniye sonra kinematic yap (yerde dursun)
        StartCoroutine(MakeKinematic(rb, 2f));

        Debug.Log($"💰 DÜŞMAN DROP (yere): {item.itemName}");
    }

    System.Collections.IEnumerator MakeKinematic(Rigidbody rb, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.linearVelocity = Vector3.zero;
        }
    }

    // Inspector'dan test için
    [ContextMenu("Test Drop")]
    void TestDrop()
    {
        hasDropped = false;
        Drop();
    }
}