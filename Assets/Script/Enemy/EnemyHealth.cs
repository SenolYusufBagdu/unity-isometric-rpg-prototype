using UnityEngine;
using UnityEngine.UI;

public class EnemyHealth : MonoBehaviour
{
    [Header("Can")]
    public float maxHealth = 100f;
    public float currentHealth;
    public bool isDead;

    [Header("UI (Opsiyonel)")]
    public Slider healthSlider;

    private Animator animator;

    // WaveManager bu eventi dinler
    public System.Action OnEnemyDied;

    // 🔊 EnemyAI ses hookları için
    public System.Action OnHurt;
    public System.Action OnDied;

    void Start()
    {
        animator = GetComponentInChildren<Animator>();
        currentHealth = maxHealth;
        UpdateUI();
        Debug.Log($"✅ ENEMY: {gameObject.name} başlatıldı. Can: {currentHealth}");
    }

    public void TakeDamage(float damage)
    {
        if (isDead) return;

        currentHealth = Mathf.Clamp(currentHealth - damage, 0f, maxHealth);
        UpdateUI();
        OnHurt?.Invoke(); // 🔊 Hasar sesi
        Debug.Log($"💢 ENEMY: {gameObject.name} → -{damage} | Kalan: {currentHealth}/{maxHealth}");

        if (currentHealth <= 0f) Die();
    }

    void UpdateUI()
    {
        if (healthSlider != null)
            healthSlider.value = currentHealth / maxHealth;
    }

    void Die()
    {
        if (isDead) return;
        isDead = true;

        Debug.Log($"💀 {gameObject.name} öldü!");

        OnEnemyDied?.Invoke(); // WaveManager
        OnDied?.Invoke();      // 🔊 Ölüm sesi

        var agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (agent != null) agent.isStopped = true;

        var col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        var dissolve = GetComponent<DeathDissolve>();
        if (dissolve == null)
            dissolve = GetComponentInParent<DeathDissolve>();

        if (dissolve != null)
            dissolve.StartDissolve();
        else
            Destroy(gameObject, 2f);
    }
}