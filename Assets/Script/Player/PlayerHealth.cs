using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class PlayerHealth : MonoBehaviour
{
    [Header("Can")]
    public float maxHealth = 100f;
    public float currentHealth;

    [Header("UI")]
    public Slider healthSlider;

    [Header("Dalga Sonu İyileşme")]
    [Range(0f, 1f)] public float healPercentPerWave = 0.30f;
    public float healDuration = 2f;

    private bool isDead;
    private bool isInvulnerable;

    void Start()
    {
        currentHealth = maxHealth;
        UpdateUI();

        if (WaveManager.Instance != null)
            WaveManager.Instance.OnWaveCompleted += OnWaveCompleted;
        else
            StartCoroutine(SubscribeLate());
    }

    IEnumerator SubscribeLate()
    {
        yield return new WaitForSeconds(0.5f);
        if (WaveManager.Instance != null)
            WaveManager.Instance.OnWaveCompleted += OnWaveCompleted;
    }

    void OnDestroy()
    {
        if (WaveManager.Instance != null)
            WaveManager.Instance.OnWaveCompleted -= OnWaveCompleted;
    }

    void OnWaveCompleted(int waveNumber)
    {
        if (isDead) return;
        float healAmount = maxHealth * healPercentPerWave;
        StartCoroutine(HealCoroutine(healAmount));
    }

    IEnumerator HealCoroutine(float totalHeal)
    {
        float startHP = currentHealth;
        float targetHP = Mathf.Min(currentHealth + totalHeal, maxHealth);
        float elapsed = 0f;

        while (elapsed < healDuration)
        {
            elapsed += Time.deltaTime;
            currentHealth = Mathf.Lerp(startHP, targetHP, elapsed / healDuration);
            UpdateUI();
            yield return null;
        }

        currentHealth = targetHP;
        UpdateUI();
    }

    public void SetInvulnerable(bool value) => isInvulnerable = value;

    public void TakeDamage(float damage)
    {
        if (isDead || isInvulnerable) return;
        currentHealth = Mathf.Clamp(currentHealth - damage, 0, maxHealth);
        UpdateUI();
        if (currentHealth <= 0) Die();
    }

    public void Heal(float amount)
    {
        if (isDead) return;
        currentHealth = Mathf.Clamp(currentHealth + amount, 0, maxHealth);
        UpdateUI();
    }

    // PlayerLevelSystem max can artınca slider'ı güncelle
    public void ForceUpdateUI() => UpdateUI();

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