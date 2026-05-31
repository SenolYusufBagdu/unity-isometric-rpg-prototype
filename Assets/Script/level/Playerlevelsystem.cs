using UnityEngine;

/// <summary>
/// PlayerLevelSystem.cs → Player GameObject'ine ekle
///
/// Kart seçilince bu script ilgili stat'ı artırır.
/// WaveManager dalga bitince CardSelectionUI'ı tetikler,
/// CardSelectionUI kart seçilince bu scripti çağırır.
/// </summary>
public class PlayerLevelSystem : MonoBehaviour
{
    public static PlayerLevelSystem Instance { get; private set; }

    [Header("Seviye")]
    public int currentLevel = 1;

    // ── Mevcut çarpanlar (başlangıç = 1.0) ──
    [Header("Mevcut Stat Çarpanları (Bilgi amaçlı)")]
    [SerializeField] private float spellDamageMultiplier = 1f;
    [SerializeField] private float swordDamageMultiplier = 1f;
    [SerializeField] private float arrowDamageMultiplier = 1f;
    [SerializeField] private float maxHealthMultiplier = 1f;
    [SerializeField] private float moveSpeedMultiplier = 1f;

    // Referanslar — otomatik bulunur
    private PlayerHealth playerHealth;
    private PlayerMouseRotation mouseRot;
    private SwordDamage swordDamage;
    private PlayerAttack playerAttack;

    // Seviye atlandığında UI veya efekt için
    public System.Action<int> OnLevelUp; // yeni seviye

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;
    }

    void Start()
    {
        playerHealth = GetComponent<PlayerHealth>();
        mouseRot = GetComponent<PlayerMouseRotation>();
        playerAttack = GetComponent<PlayerAttack>();

        // Kılıç child objesinde olabilir
        swordDamage = GetComponentInChildren<SwordDamage>();
    }

    // ═══════════════════════════════════════════════════════
    // KART ETKİLERİ — CardSelectionUI bu metodları çağırır
    // ═══════════════════════════════════════════════════════

    /// Büyü hasarını %amount artır (örn: 0.15 = %15)
    public void UpgradeSpellDamage(float amount)
    {
        spellDamageMultiplier += amount;

        // Projectile ve Fireball prefabları runtime'da oluşturulduğu için
        // çarpanı PlayerAttack üzerinden saklıyoruz; SpawnProjectile/SpawnFireball
        // bu değeri kullanacak şekilde güncellendi
        if (playerAttack != null)
            playerAttack.spellDamageMultiplier = spellDamageMultiplier;

        Debug.Log($"✨ Büyü hasarı x{spellDamageMultiplier:F2}");
        LevelUp();
    }

    /// Kılıç hasarını %amount artır
    public void UpgradeSwordDamage(float amount)
    {
        swordDamageMultiplier += amount;

        if (swordDamage != null)
            swordDamage.damage *= (1.5f + amount);

        Debug.Log($"⚔️ Kılıç hasarı x{swordDamageMultiplier:F2}");
        LevelUp();
    }

    /// Ok hasarını %amount artır
    public void UpgradeArrowDamage(float amount)
    {
        arrowDamageMultiplier += amount;

        if (playerAttack != null)
            playerAttack.arrowDamage *= (1.5f + amount);

        Debug.Log($"🏹 Ok hasarı x{arrowDamageMultiplier:F2}");
        LevelUp();
    }

    /// Maksimum canı %amount artır ve mevcut canı da yükselt
    public void UpgradeMaxHealth(float amount)
    {
        maxHealthMultiplier += amount;

        if (playerHealth != null)
        {
            float oldMax = playerHealth.maxHealth;
            playerHealth.maxHealth *= (1f + amount);
            float added = playerHealth.maxHealth - oldMax;
            playerHealth.currentHealth = Mathf.Min(
                playerHealth.currentHealth + added,
                playerHealth.maxHealth);
            // Slider güncelle
            playerHealth.ForceUpdateUI();
        }

        Debug.Log($"❤️ Max can x{maxHealthMultiplier:F2}");
        LevelUp();
    }

    /// Hareket hızını %amount artır
    public void UpgradeMoveSpeed(float amount)
    {
        moveSpeedMultiplier += amount;

        if (mouseRot != null)
            mouseRot.moveSpeed *= (1f + amount);

        Debug.Log($"💨 Hareket hızı x{moveSpeedMultiplier:F2}");
        LevelUp();
    }

    // ─────────────────────────────────────────────────────
    void LevelUp()
    {
        currentLevel++;
        OnLevelUp?.Invoke(currentLevel);
        Debug.Log($"🌟 SEVİYE {currentLevel}!");
    }
}