using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// WaveManager.cs → Boş bir "GameManager" objesine ekle.
///
/// YENİ: İki farklı enemy prefab destekli.
/// Her wave'de hangi oranda karışık spawn olacağı ayarlanabilir.
/// + Dalga tamamlanınca ses çalar (ElevenLabs veya herhangi AudioClip).
/// </summary>
public class WaveManager : MonoBehaviour
{
    public static WaveManager Instance { get; private set; }

    [Header("─ Enemy Prefablar")]
    [Tooltip("Birinci enemy türü (örn: kılıçlı düşman)")]
    [SerializeField] public GameObject enemyPrefabA;

    [Tooltip("İkinci enemy türü (örn: okçu düşman)")]
    [SerializeField] public GameObject enemyPrefabB;

    [Tooltip("0 = hep A  |  1 = hep B  |  0.5 = yarı yarıya")]
    [SerializeField][Range(0f, 1f)] public float enemyBSpawnChance = 0.4f;

    [Header("─ Spawn Noktaları")]
    [Tooltip("Kale etrafına koyduğun boş GameObject'leri buraya sürükle")]
    [SerializeField] public Transform[] spawnPoints;

    [Header("─ Kale")]
    [SerializeField] public CastleHealth castle;

    [Header("─ Dalga Ayarları")]
    [SerializeField] public float timeBetweenWaves = 5f;
    [SerializeField] public bool autoStart = true;

    [Header("─ Ses Ayarları")]
    [Tooltip("Dalga tamamlanınca çalacak ses (ElevenLabs'tan ürettiğin fanfare klibini buraya sürükle)")]
    [SerializeField] private AudioClip waveClearedSound;

    [Tooltip("Ses seviyesi (0-1)")]
    [SerializeField][Range(0f, 1f)] private float waveSoundVolume = 1f;

    [Tooltip("Bırakırsan otomatik AudioSource oluşturulur; istersen kendin ata")]
    [SerializeField] private AudioSource audioSource;

    [Header("─ Dalga Listesi")]
    public List<WaveData> waves = new List<WaveData>();

    // ── İç durum ──
    private int currentWaveIndex = -1;
    private int enemiesAlive;
    private int enemiesSpawnedThisWave;
    private bool waveActive;
    private bool gameOver;

    public bool IsWaveActive => waveActive;
    public int CurrentWaveNumber => currentWaveIndex + 1;
    public int TotalWaves => waves.Count;

    // ── Olaylar ──
    public System.Action<int, int> OnWaveStarted;       // waveNumber, totalWaves
    public System.Action<int> OnWaveCompleted;          // waveNumber
    public System.Action OnAllWavesCompleted;
    public System.Action OnGameOver;
    public System.Action<int, int> OnEnemyCountChanged; // alive, totalThisWave

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        if (castle == null) castle = FindObjectOfType<CastleHealth>();
        if (castle != null) castle.OnCastleDestroyed += HandleCastleDestroyed;

        // AudioSource yoksa otomatik ekle
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        if (waves.Count == 0) CreateDefaultWaves();
        if (autoStart) StartCoroutine(StartNextWaveDelayed(2f));
    }

    // ════════════════════════════════════════
    // VARSAYILAN DALGALAR
    // ════════════════════════════════════════

    void CreateDefaultWaves()
    {
        //                          waveNo  count  interval  hpMult  dmgMult  playerChance
        waves.Add(NewWave(1, 5, 3.0f, 1.00f, 1.00f, 0.40f));
        waves.Add(NewWave(2, 10, 2.5f, 1.25f, 1.10f, 0.35f));
        waves.Add(NewWave(3, 15, 2.0f, 1.50f, 1.20f, 0.30f));
        waves.Add(NewWave(4, 20, 1.5f, 2.00f, 1.40f, 0.25f));
        waves.Add(NewWave(5, 8, 1.0f, 3.00f, 2.00f, 0.50f)); // boss dalgası
        Debug.Log($"✅ WaveManager: {waves.Count} varsayılan dalga oluşturuldu.");
    }

    WaveData NewWave(int num, int count, float interval, float hp, float dmg, float pChance)
    {
        return new WaveData
        {
            waveNumber = num,
            enemyCount = count,
            spawnInterval = interval,
            enemyHealthMultiplier = hp,
            enemyDamageMultiplier = dmg,
            playerTargetChance = pChance
        };
    }

    // ════════════════════════════════════════
    // DALGA BAŞLATMA
    // ════════════════════════════════════════

    IEnumerator StartNextWaveDelayed(float delay)
    {
        yield return new WaitForSeconds(delay);
        StartNextWave();
    }

    public void StartNextWave()
    {
        if (gameOver) return;
        currentWaveIndex++;

        if (currentWaveIndex >= waves.Count)
        {
            Debug.Log("🏆 TÜM DALGALAR TAMAMLANDI!");
            OnAllWavesCompleted?.Invoke();
            return;
        }

        WaveData wave = waves[currentWaveIndex];
        enemiesAlive = 0;
        enemiesSpawnedThisWave = 0;
        waveActive = true;

        Debug.Log($"⚔️ DALGA {wave.waveNumber} BAŞLADI! Düşman: {wave.enemyCount}");
        OnWaveStarted?.Invoke(wave.waveNumber, waves.Count);
        StartCoroutine(SpawnWave(wave));
    }

    // ════════════════════════════════════════
    // SPAWN
    // ════════════════════════════════════════

    IEnumerator SpawnWave(WaveData wave)
    {
        for (int i = 0; i < wave.enemyCount; i++)
        {
            if (gameOver) yield break;
            SpawnEnemy(wave);
            enemiesSpawnedThisWave++;
            OnEnemyCountChanged?.Invoke(enemiesAlive, wave.enemyCount);
            yield return new WaitForSeconds(wave.spawnInterval);
        }
    }

    void SpawnEnemy(WaveData wave)
    {
        // Hangi prefab?
        GameObject prefab = PickPrefab();
        if (prefab == null)
        {
            Debug.LogError("❌ WaveManager: Hiçbir enemy prefabı atanmamış! Inspector'dan EnemyPrefabA veya B'yi ata.");
            return;
        }

        // Hangi spawn noktası?
        Transform sp = GetRandomSpawnPoint();
        if (sp == null)
        {
            Debug.LogError("❌ WaveManager: SpawnPoints dizisi boş!");
            return;
        }

        GameObject go = Instantiate(prefab, sp.position, sp.rotation);
        enemiesAlive++;

        // ── Can çarpanı ──
        EnemyHealth health = go.GetComponent<EnemyHealth>();
        if (health != null)
        {
            health.maxHealth *= wave.enemyHealthMultiplier;
            health.currentHealth = health.maxHealth;
            health.OnEnemyDied += HandleEnemyDied;
        }

        // ── AI ayarları ──
        EnemyAI ai = go.GetComponent<EnemyAI>();
        if (ai != null)
        {
            ai.attackDamage *= wave.enemyDamageMultiplier;
            ai.castleAttackDamage *= wave.enemyDamageMultiplier;
            ai.castle = castle;
            ai.playerTargetChance = wave.playerTargetChance;
            ai.DecideTarget();
        }

        string prefabLabel = (prefab == enemyPrefabA) ? "A" : "B";
        Debug.Log($"👹 Spawn [{prefabLabel}] {go.name} | HP:{health?.currentHealth:F0} | Hedef:{(ai != null ? (ai.playerTargetChance > Random.value ? "PLAYER" : "KALE") : "?")}");
    }

    /// <summary>Rastgele prefab seç — B şansına göre</summary>
    GameObject PickPrefab()
    {
        if (enemyPrefabB == null) return enemyPrefabA;
        if (enemyPrefabA == null) return enemyPrefabB;
        return Random.value < enemyBSpawnChance ? enemyPrefabB : enemyPrefabA;
    }

    Transform GetRandomSpawnPoint()
    {
        if (spawnPoints == null || spawnPoints.Length == 0) return null;
        return spawnPoints[Random.Range(0, spawnPoints.Length)];
    }

    // ════════════════════════════════════════
    // ÖLÜM / WAVE BİTİŞİ
    // ════════════════════════════════════════

    void HandleEnemyDied()
    {
        enemiesAlive = Mathf.Max(0, enemiesAlive - 1);
        OnEnemyCountChanged?.Invoke(enemiesAlive, waves[currentWaveIndex].enemyCount);
        ScoreManager.Instance?.RegisterKill();

        if (enemiesAlive <= 0 && enemiesSpawnedThisWave >= waves[currentWaveIndex].enemyCount)
            StartCoroutine(WaveCompleted());
    }

    IEnumerator WaveCompleted()
    {
        waveActive = false;
        int num = waves[currentWaveIndex].waveNumber;
        Debug.Log($"✅ DALGA {num} TAMAMLANDI!");

        // ── 🎺 DALGA TAMAMLANDI SESİ ──
        PlayWaveClearedSound();

        ScoreManager.Instance?.AddWaveBonus();
        OnWaveCompleted?.Invoke(num);

        if (currentWaveIndex + 1 >= waves.Count)
        {
            yield return new WaitForSeconds(2f);
            OnAllWavesCompleted?.Invoke();
            yield break;
        }

        yield return new WaitForSeconds(timeBetweenWaves);
        StartNextWave();
    }

    /// <summary>
    /// Dalga tamamlanma fanfare sesini çalar.
    /// waveClearedSound atanmamışsa sessizce geçer.
    /// </summary>
    void PlayWaveClearedSound()
    {
        if (waveClearedSound == null)
        {
            Debug.LogWarning("⚠️ WaveManager: waveClearedSound atanmamış. Inspector'dan AudioClip'i ata.");
            return;
        }

        audioSource.PlayOneShot(waveClearedSound, waveSoundVolume);
        Debug.Log($"🎺 Dalga tamamlama sesi çalındı: {waveClearedSound.name}");
    }

    void HandleCastleDestroyed()
    {
        gameOver = true;
        waveActive = false;
        StopAllCoroutines();
        Debug.Log("💀 OYUN BİTTİ — Kale yıkıldı!");
        OnGameOver?.Invoke();
    }

    public void ForceStartWave() => StartNextWave();
}

// ════════════════════════════════════════
// WAVE VERİSİ
// ════════════════════════════════════════

[System.Serializable]
public class WaveData
{
    [Header("Temel")]
    public int waveNumber = 1;
    public int enemyCount = 5;
    public float spawnInterval = 3f;

    [Header("Zorluk")]
    [Range(0.5f, 5f)] public float enemyHealthMultiplier = 1f;
    [Range(0.5f, 5f)] public float enemyDamageMultiplier = 1f;

    [Header("Hedef Seçimi")]
    [Range(0f, 1f)]
    [Tooltip("Bu dalganın enemy'lerinin player'ı hedefleme şansı (0=hep kale, 1=hep player)")]
    public float playerTargetChance = 0.4f;
}