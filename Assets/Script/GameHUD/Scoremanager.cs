using UnityEngine;

/// <summary>
/// ScoreManager.cs → Herhangi bir sahnedeki boş GameObject'e ekle (GameManager gibi)
///
/// KULLANIM:
/// ScoreManager.Instance.AddScore(100);
/// EnemyHealth Die() içinde çağır
/// </summary>
public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    [Header("Skor Ayarları")]
    public int scorePerKill = 100;
    public int waveCompletionBonus = 500;
    public int scorePer10Seconds = 10;  // Hayatta kalma bonusu

    private int currentScore;
    private int totalKills;
    private float survivalTimer;

    public int CurrentScore => currentScore;
    public int TotalKills => totalKills;

    // UI güncelleme olayı
    public System.Action<int> OnScoreChanged;
    public System.Action<int> OnKillChanged;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Update()
    {
        // Hayatta kalma bonusu — her 10 saniyede puan ver
        if (WaveManager.Instance != null && WaveManager.Instance.IsWaveActive)
        {
            survivalTimer += Time.deltaTime;
            if (survivalTimer >= 10f)
            {
                survivalTimer = 0f;
                AddScore(scorePer10Seconds);
            }
        }
    }

    public void AddScore(int amount)
    {
        currentScore += amount;
        OnScoreChanged?.Invoke(currentScore);
        Debug.Log($"⭐ Skor: {currentScore} (+{amount})");
    }

    public void RegisterKill()
    {
        totalKills++;
        AddScore(scorePerKill);
        OnKillChanged?.Invoke(totalKills);
    }

    public void AddWaveBonus()
    {
        AddScore(waveCompletionBonus);
        Debug.Log($"🏆 Dalga tamamlama bonusu: +{waveCompletionBonus}");
    }

    public void ResetScore()
    {
        currentScore = 0;
        totalKills = 0;
        survivalTimer = 0f;
        OnScoreChanged?.Invoke(currentScore);
        OnKillChanged?.Invoke(totalKills);
    }
}