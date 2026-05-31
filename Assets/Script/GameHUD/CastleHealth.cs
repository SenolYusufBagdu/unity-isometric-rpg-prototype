using UnityEngine;
using System.Collections;

/// <summary>
/// CastleHealth.cs → Kale GameObject'ine ekle
///
/// YENİ: Wave bittikten sonra kale canı kısmen iyileşir.
/// </summary>
public class CastleHealth : MonoBehaviour
{
    public static CastleHealth Instance { get; private set; }

    [Header("Kale Canı")]
    [SerializeField] public float maxHealth = 500f;
    [SerializeField] public float currentHealth;
    [SerializeField] public bool isDestroyed;

    [Header("Wave Sonu İyileşme")]
    [SerializeField] public bool healBetweenWaves = true;
    [SerializeField][Range(0f, 1f)] public float healPercentPerWave = 0.25f;
    [SerializeField] public float healDuration = 3f;

    [Header("Hasar Flaşı (Opsiyonel)")]
    [SerializeField] public Renderer[] castleRenderers;
    [SerializeField] public Color damageFlashColor = new Color(1f, 0.2f, 0.2f, 1f);
    [SerializeField] public float flashDuration = 0.15f;

    public System.Action<float, float> OnHealthChanged;
    public System.Action OnCastleDestroyed;

    private Color[] originalColors;
    private bool isFlashing;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        currentHealth = maxHealth;

        if (castleRenderers == null || castleRenderers.Length == 0)
            castleRenderers = GetComponentsInChildren<Renderer>();

        originalColors = new Color[castleRenderers.Length];
        for (int i = 0; i < castleRenderers.Length; i++)
            if (castleRenderers[i] != null)
                originalColors[i] = castleRenderers[i].material.color;

        if (WaveManager.Instance != null)
            WaveManager.Instance.OnWaveCompleted += OnWaveCompleted;

        OnHealthChanged?.Invoke(currentHealth, maxHealth);
        Debug.Log($"🏰 Kale başlatıldı. Can: {currentHealth}/{maxHealth}");
    }

    void OnDestroy()
    {
        if (WaveManager.Instance != null)
            WaveManager.Instance.OnWaveCompleted -= OnWaveCompleted;
    }

    public void TakeDamage(float damage)
    {
        if (isDestroyed) return;
        currentHealth = Mathf.Clamp(currentHealth - damage, 0f, maxHealth);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
        Debug.Log($"🏰 Kale hasar: -{damage} | Kalan: {currentHealth}/{maxHealth}");
        if (!isFlashing) StartCoroutine(DamageFlash());
        if (currentHealth <= 0f) TriggerDestroy();
    }

    void OnWaveCompleted(int waveNumber)
    {
        if (!healBetweenWaves || isDestroyed) return;
        float healAmount = maxHealth * healPercentPerWave;
        StartCoroutine(HealCoroutine(healAmount));
    }

    IEnumerator HealCoroutine(float totalHeal)
    {
        float startHP = currentHealth;
        float targetHP = Mathf.Min(currentHealth + totalHeal, maxHealth);
        float elapsed = 0f;
        Debug.Log($"💚 Kale iyileşiyor: +{totalHeal:F0} → {targetHP:F0}/{maxHealth}");
        while (elapsed < healDuration)
        {
            elapsed += Time.deltaTime;
            currentHealth = Mathf.Lerp(startHP, targetHP, elapsed / healDuration);
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
            yield return null;
        }
        currentHealth = targetHP;
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    void TriggerDestroy()
    {
        if (isDestroyed) return;
        isDestroyed = true;
        Debug.Log("💥 KALE YIKILDI!");
        OnCastleDestroyed?.Invoke();
    }

    IEnumerator DamageFlash()
    {
        isFlashing = true;
        foreach (var r in castleRenderers)
            if (r != null) r.material.color = damageFlashColor;
        yield return new WaitForSeconds(flashDuration);
        for (int i = 0; i < castleRenderers.Length; i++)
            if (castleRenderers[i] != null && i < originalColors.Length)
                castleRenderers[i].material.color = originalColors[i];
        isFlashing = false;
    }

    public float GetHealthRatio() => maxHealth > 0 ? currentHealth / maxHealth : 0f;
}