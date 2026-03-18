using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// EnemyHealth.cs
/// EnemyHealthBar scripti aynı GameObject'te varsa otomatik çalışır.
/// Ekstra bağlantı gerekmez.
/// </summary>
public class EnemyHealth : MonoBehaviour
{
    [Header("Can")]
    public float maxHealth = 100f;
    public float currentHealth;
    public bool isDead;

    [Header("UI (Opsiyonel)")]
    public Slider healthSlider;

    private Animator animator;
    private static readonly int DeathHash = Animator.StringToHash("Death");

    void Start()
    {
        animator = GetComponent<Animator>();
        currentHealth = maxHealth;
        UpdateUI();
        Debug.Log($"✅ ENEMY: {gameObject.name} başlatıldı. Can: {currentHealth}");
    }

    public void TakeDamage(float damage)
    {
        if (isDead) return;

        currentHealth = Mathf.Clamp(currentHealth - damage, 0f, maxHealth);
        UpdateUI();
        Debug.Log($"💢 ENEMY: {gameObject.name} → -{damage} | Kalan: {currentHealth}/{maxHealth}");

        if (currentHealth <= 0f) Die();
    }

    void UpdateUI()
    {
        if (healthSlider != null)
            healthSlider.value = currentHealth / maxHealth;
        // EnemyHealthBar kendi Update()'inde currentHealth'i izliyor — burada çağırmaya gerek yok
    }

    void Die()
    {
        isDead = true;
        Debug.Log($"💀 ENEMY ÖLDÜ: {gameObject.name}");

        if (animator != null) animator.SetTrigger(DeathHash);

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null) rb.linearVelocity = Vector3.zero;

        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        Destroy(gameObject, 3f);
    }
}