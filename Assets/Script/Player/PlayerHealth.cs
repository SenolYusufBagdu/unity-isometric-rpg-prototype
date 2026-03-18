using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// PlayerHealth.cs → Player GameObject'ine ekle
/// SetInvulnerable(true/false) → PlayerDash tarafından çağrılır
/// </summary>
public class PlayerHealth : MonoBehaviour
{
    [Header("Can")]
    public float maxHealth = 100f;
    public float currentHealth;

    [Header("UI")]
    public Slider healthSlider;

    private bool isDead;
    private bool isInvulnerable;

    void Start()
    {
        currentHealth = maxHealth;
        UpdateUI();
    }

    // PlayerDash i-frame için çağırır
    public void SetInvulnerable(bool value)
    {
        isInvulnerable = value;
        Debug.Log($"🛡️ PLAYER: I-Frame {(value ? "AÇIK" : "KAPALI")}");
    }

    public void TakeDamage(float damage)
    {
        if (isDead || isInvulnerable) return;

        currentHealth -= damage;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        UpdateUI();
        Debug.Log($"💢 PLAYER HASAR: -{damage} | Kalan: {currentHealth}/{maxHealth}");

        if (currentHealth <= 0) Die();
    }

    void UpdateUI()
    {
        if (healthSlider != null)
            healthSlider.value = currentHealth / maxHealth;
    }

    void Die()
    {
        isDead = true;
        Debug.Log("💀 PLAYER ÖLDÜ!");
    }
}