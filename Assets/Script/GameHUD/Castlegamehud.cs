using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// CastleGameHUD.cs → Canvas GameObject'ine ekle
///
/// KURULUM:
/// 1. Hierarchy'de UI → Canvas oluştur (Screen Space Overlay, 1920x1080)
/// 2. Bu scripti Canvas'a ekle
/// 3. Otomatik çalışır — WaveManager, ScoreManager ve CastleHealth'i bulur
///
/// GÖSTERGELER:
/// ┌─────────────────────────────────────────────┐
/// │  🏰 KALE   [████████░░░░]  350 / 500        │  ← Sol üst
/// │  ⭐ SKOR   12,500          💀 Kills: 25      │  ← Sağ üst
/// │  ⚔️ DALGA  2 / 5          👹 Kalan: 7 / 10  │  ← Orta üst
/// │                                              │
/// │  [       DALGA 2 BAŞLADI!      ]            │  ← Orta bildirim (geçici)
/// │                                              │
/// │  [    OYUN BİTTİ / KAZANDIN   ]            │  ← Oyun sonu ekranı
/// └─────────────────────────────────────────────┘
/// </summary>
public class CastleGameHUD : MonoBehaviour
{
    // ─── Renkler ───
    private static readonly Color CASTLE_BAR_COLOR = new Color(0.20f, 0.65f, 1.00f, 1f);
    private static readonly Color CASTLE_LOW_COLOR = new Color(0.95f, 0.25f, 0.15f, 1f);
    private static readonly Color CASTLE_DRAIN_COLOR = new Color(1f, 1f, 1f, 0.22f);
    private static readonly Color SCORE_COLOR = new Color(1.00f, 0.85f, 0.20f, 1f);
    private static readonly Color WAVE_COLOR = new Color(0.90f, 0.50f, 1.00f, 1f);
    private static readonly Color PANEL_COLOR = new Color(0.04f, 0.04f, 0.07f, 0.88f);
    private static readonly Color ENEMY_COUNT_COLOR = new Color(0.95f, 0.40f, 0.25f, 1f);
    private static readonly Color WIN_COLOR = new Color(0.20f, 0.95f, 0.45f, 1f);
    private static readonly Color LOSE_COLOR = new Color(0.95f, 0.20f, 0.20f, 1f);

    // ─── Kale canı ───
    private RectTransform castleFill;
    private RectTransform castleDrain;
    private Text castleHPText;
    private Image castleFillImg;

    // ─── Skor ───
    private Text scoreText;
    private Text killsText;

    // ─── Dalga ───
    private Text waveText;
    private Text enemyCountText;
    private Text waveTimerText;

    // ─── Bildirim ───
    private GameObject notifPanel;
    private Text notifText;

    // ─── Oyun sonu ───
    private GameObject gameOverPanel;
    private Text gameOverTitle;
    private Text gameOverScore;
    private Text gameOverKills;

    // ─── İç durum ───
    private float castleDrainTarget = 1f;
    private const float DRAIN_SPEED = 0.8f;
    private const float LOW_HP = 0.30f;

    void Start()
    {
        BuildHUD();
        SubscribeEvents();
        RefreshAll();
    }

    void Update()
    {
        // Kale drain animasyonu
        if (castleDrain != null)
        {
            float d = castleDrain.anchorMax.x;
            if (d > castleDrainTarget)
                castleDrain.anchorMax = new Vector2(
                    Mathf.MoveTowards(d, castleDrainTarget, DRAIN_SPEED * Time.deltaTime), 1f);
        }
    }

    void OnDestroy() => UnsubscribeEvents();

    // ══════════════════════════════════════════════
    // OLAY ABONELİKLERİ
    // ══════════════════════════════════════════════

    void SubscribeEvents()
    {
        if (CastleHealth.Instance != null)
            CastleHealth.Instance.OnHealthChanged += UpdateCastleBar;

        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.OnScoreChanged += UpdateScore;
            ScoreManager.Instance.OnKillChanged += UpdateKills;
        }

        if (WaveManager.Instance != null)
        {
            WaveManager.Instance.OnWaveStarted += OnWaveStarted;
            WaveManager.Instance.OnWaveCompleted += OnWaveCompleted;
            WaveManager.Instance.OnEnemyCountChanged += UpdateEnemyCount;
            WaveManager.Instance.OnAllWavesCompleted += OnAllWavesCompleted;
            WaveManager.Instance.OnGameOver += OnGameOver;
        }
    }

    void UnsubscribeEvents()
    {
        if (CastleHealth.Instance != null)
            CastleHealth.Instance.OnHealthChanged -= UpdateCastleBar;

        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.OnScoreChanged -= UpdateScore;
            ScoreManager.Instance.OnKillChanged -= UpdateKills;
        }

        if (WaveManager.Instance != null)
        {
            WaveManager.Instance.OnWaveStarted -= OnWaveStarted;
            WaveManager.Instance.OnWaveCompleted -= OnWaveCompleted;
            WaveManager.Instance.OnEnemyCountChanged -= UpdateEnemyCount;
            WaveManager.Instance.OnAllWavesCompleted -= OnAllWavesCompleted;
            WaveManager.Instance.OnGameOver -= OnGameOver;
        }
    }

    // ══════════════════════════════════════════════
    // GÜNCELLEME METODLARİ
    // ══════════════════════════════════════════════

    void RefreshAll()
    {
        if (CastleHealth.Instance != null)
            UpdateCastleBar(CastleHealth.Instance.currentHealth, CastleHealth.Instance.maxHealth);

        if (ScoreManager.Instance != null)
        {
            UpdateScore(ScoreManager.Instance.CurrentScore);
            UpdateKills(ScoreManager.Instance.TotalKills);
        }

        if (WaveManager.Instance != null)
        {
            if (waveText != null)
                waveText.text = $"DALGA  {WaveManager.Instance.CurrentWaveNumber} / {WaveManager.Instance.TotalWaves}";
        }
    }

    void UpdateCastleBar(float current, float max)
    {
        float ratio = max > 0 ? Mathf.Clamp01(current / max) : 0f;
        castleDrainTarget = ratio;

        if (castleFill != null)
            castleFill.anchorMax = new Vector2(ratio, 1f);

        if (castleFillImg != null)
            castleFillImg.color = ratio <= LOW_HP ? CASTLE_LOW_COLOR : CASTLE_BAR_COLOR;

        if (castleHPText != null)
            castleHPText.text = $"{Mathf.CeilToInt(current)} / {Mathf.CeilToInt(max)}";
    }

    void UpdateScore(int score)
    {
        if (scoreText != null)
            scoreText.text = score.ToString("N0");
    }

    void UpdateKills(int kills)
    {
        if (killsText != null)
            killsText.text = $"💀  {kills}";
    }

    void UpdateEnemyCount(int alive, int total)
    {
        if (enemyCountText != null)
            enemyCountText.text = $"👹  {alive} / {total}";
    }

    void OnWaveStarted(int waveNum, int totalWaves)
    {
        if (waveText != null)
            waveText.text = $"DALGA  {waveNum} / {totalWaves}";

        ShowNotification($"⚔️  DALGA {waveNum} BAŞLADI!", WAVE_COLOR, 3f);
    }

    void OnWaveCompleted(int waveNum)
    {
        ShowNotification($"✅  DALGA {waveNum} TAMAMLANDI!  +500", WIN_COLOR, 3f);
    }

    void OnAllWavesCompleted()
    {
        ShowGameOver(true);
    }

    void OnGameOver()
    {
        ShowGameOver(false);
    }

    // ══════════════════════════════════════════════
    // BİLDİRİM SİSTEMİ
    // ══════════════════════════════════════════════

    void ShowNotification(string message, Color color, float duration)
    {
        StopAllCoroutines();
        StartCoroutine(NotifCoroutine(message, color, duration));
    }

    System.Collections.IEnumerator NotifCoroutine(string message, Color color, float duration)
    {
        if (notifPanel == null || notifText == null) yield break;

        notifText.text = message;
        notifText.color = color;
        notifPanel.SetActive(true);

        var cg = notifPanel.GetComponent<CanvasGroup>();
        if (cg != null) cg.alpha = 1f;

        yield return new WaitForSeconds(duration - 0.5f);

        // Fade out
        if (cg != null)
        {
            float t = 0f;
            while (t < 0.5f)
            {
                t += Time.deltaTime;
                cg.alpha = Mathf.Lerp(1f, 0f, t / 0.5f);
                yield return null;
            }
        }

        notifPanel.SetActive(false);
    }

    // ══════════════════════════════════════════════
    // OYUN SONU EKRANI
    // ══════════════════════════════════════════════

    void ShowGameOver(bool victory)
    {
        if (gameOverPanel == null) return;

        gameOverPanel.SetActive(true);

        if (gameOverTitle != null)
        {
            gameOverTitle.text = victory ? "🏆  KAZANDIN!" : "💀  KALE YIKILDI";
            gameOverTitle.color = victory ? WIN_COLOR : LOSE_COLOR;
        }

        int score = ScoreManager.Instance?.CurrentScore ?? 0;
        int kills = ScoreManager.Instance?.TotalKills ?? 0;

        if (gameOverScore != null)
            gameOverScore.text = $"Skor:  {score:N0}";

        if (gameOverKills != null)
            gameOverKills.text = $"Öldürülen:  {kills}";
    }

    // ══════════════════════════════════════════════════════════════════
    // UI BUILDER — Tüm Canvas çocukları burada oluşturulur
    // ══════════════════════════════════════════════════════════════════

    void BuildHUD()
    {
        Canvas canvas = GetComponent<Canvas>();
        if (canvas == null) { Debug.LogError("❌ CastleGameHUD: Canvas yok!"); return; }

        BuildCastleBar();
        BuildScorePanel();
        BuildWavePanel();
        BuildNotifBanner();
        BuildGameOverScreen();
    }

    // ── KALE CAN BARI (Sol üst) ──
    void BuildCastleBar()
    {
        var panel = MakePanel("CastlePanel", transform,
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(16f, -16f), new Vector2(360f, 68f));

        // Başlık
        var title = MakeText("🏰  KALE", panel.transform, 11,
            new Color(0.7f, 0.85f, 1f, 1f), TextAnchor.UpperLeft);
        PositionRT(title.GetComponent<RectTransform>(),
            new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f),
            new Vector2(10f, -8f), new Vector2(-10f, 16f));
        title.fontStyle = FontStyle.Bold;

        // HP değeri (sağ üst)
        castleHPText = MakeText("500 / 500", panel.transform, 11, Color.white, TextAnchor.UpperRight);
        PositionRT(castleHPText.GetComponent<RectTransform>(),
            new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f),
            new Vector2(-10f, -8f), new Vector2(0f, 16f));

        // Bar arka plan
        var bg = MakeGO("BarBg", panel.transform);
        PositionRT(bg.GetComponent<RectTransform>(),
            new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f),
            new Vector2(10f, -34f), new Vector2(-10f, 18f));
        bg.AddComponent<Image>().color = new Color(0.05f, 0.05f, 0.09f, 0.9f);

        castleDrain = MakeBarFill("Drain", bg.transform, CASTLE_DRAIN_COLOR);
        var fillGO = MakeGO("Fill", bg.transform);
        castleFill = fillGO.GetComponent<RectTransform>();
        castleFill.anchorMin = Vector2.zero; castleFill.anchorMax = Vector2.one;
        castleFill.offsetMin = castleFill.offsetMax = Vector2.zero;
        castleFillImg = fillGO.AddComponent<Image>();
        castleFillImg.color = CASTLE_BAR_COLOR;

        // % etiketi
        var pct = MakeText("Kale", bg.transform, 9,
            new Color(1f, 1f, 1f, 0.5f), TextAnchor.MiddleCenter);
        Fill(pct.GetComponent<RectTransform>());
    }

    // ── SKOR PANELİ (Sağ üst) ──
    void BuildScorePanel()
    {
        var panel = MakePanel("ScorePanel", transform,
            new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f),
            new Vector2(-16f, -16f), new Vector2(280f, 68f));

        // Skor başlık
        var slbl = MakeText("⭐  SKOR", panel.transform, 10,
            new Color(1f, 0.9f, 0.4f, 0.8f), TextAnchor.UpperLeft);
        PositionRT(slbl.GetComponent<RectTransform>(),
            new Vector2(0f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, 1f),
            new Vector2(10f, -8f), new Vector2(0f, 16f));

        // Skor değeri
        scoreText = MakeText("0", panel.transform, 20, SCORE_COLOR, TextAnchor.UpperLeft);
        scoreText.fontStyle = FontStyle.Bold;
        PositionRT(scoreText.GetComponent<RectTransform>(),
            new Vector2(0f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, 1f),
            new Vector2(10f, -26f), new Vector2(0f, 26f));

        // Kill başlık
        var klbl = MakeText("ÖLDÜRÜLEN", panel.transform, 10,
            new Color(0.9f, 0.5f, 0.4f, 0.8f), TextAnchor.UpperLeft);
        PositionRT(klbl.GetComponent<RectTransform>(),
            new Vector2(0.5f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f),
            new Vector2(8f, -8f), new Vector2(-8f, 16f));

        // Kill değeri
        killsText = MakeText("💀  0", panel.transform, 18, ENEMY_COUNT_COLOR, TextAnchor.UpperLeft);
        killsText.fontStyle = FontStyle.Bold;
        PositionRT(killsText.GetComponent<RectTransform>(),
            new Vector2(0.5f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f),
            new Vector2(8f, -26f), new Vector2(-8f, 26f));
    }

    // ── DALGA PANELİ (Orta üst) ──
    void BuildWavePanel()
    {
        var panel = MakePanel("WavePanel", transform,
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -16f), new Vector2(300f, 68f));

        // Dalga etiketi
        waveText = MakeText("DALGA  — / —", panel.transform, 13, WAVE_COLOR, TextAnchor.UpperCenter);
        waveText.fontStyle = FontStyle.Bold;
        PositionRT(waveText.GetComponent<RectTransform>(),
            new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
            new Vector2(8f, -10f), new Vector2(-8f, 22f));

        // Enemy sayısı
        enemyCountText = MakeText("👹  0 / 0", panel.transform, 13, ENEMY_COUNT_COLOR, TextAnchor.UpperCenter);
        PositionRT(enemyCountText.GetComponent<RectTransform>(),
            new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
            new Vector2(8f, -34f), new Vector2(-8f, 22f));
    }

    // ── BİLDİRİM BANDI (Ekran ortası) ──
    void BuildNotifBanner()
    {
        notifPanel = MakeGO("NotifBanner", transform);
        var rt = notifPanel.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.65f);
        rt.anchorMax = new Vector2(0.5f, 0.65f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(600f, 60f);
        rt.anchoredPosition = Vector2.zero;

        var bg = notifPanel.AddComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.75f);

        notifPanel.AddComponent<CanvasGroup>();

        notifText = MakeText("", notifPanel.transform, 20, WAVE_COLOR, TextAnchor.MiddleCenter);
        notifText.fontStyle = FontStyle.Bold;
        Fill(notifText.GetComponent<RectTransform>());

        notifPanel.SetActive(false);
    }

    // ── OYUN SONU EKRANI (Tam ekran overlay) ──
    void BuildGameOverScreen()
    {
        gameOverPanel = MakeGO("GameOverScreen", transform);
        Fill(gameOverPanel.GetComponent<RectTransform>());
        gameOverPanel.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.80f);

        // Merkez kutu
        var box = MakeGO("Box", gameOverPanel.transform);
        var bRT = box.GetComponent<RectTransform>();
        bRT.anchorMin = new Vector2(0.5f, 0.5f); bRT.anchorMax = new Vector2(0.5f, 0.5f);
        bRT.pivot = new Vector2(0.5f, 0.5f); bRT.sizeDelta = new Vector2(520f, 260f);
        bRT.anchoredPosition = Vector2.zero;
        box.AddComponent<Image>().color = new Color(0.04f, 0.04f, 0.08f, 0.97f);

        // Başlık
        gameOverTitle = MakeText("", box.transform, 32, WIN_COLOR, TextAnchor.MiddleCenter);
        gameOverTitle.fontStyle = FontStyle.Bold;
        PositionRT(gameOverTitle.GetComponent<RectTransform>(),
            new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
            new Vector2(20f, -30f), new Vector2(-20f, 50f));

        // Skor
        gameOverScore = MakeText("Skor:  0", box.transform, 20, SCORE_COLOR, TextAnchor.MiddleCenter);
        PositionRT(gameOverScore.GetComponent<RectTransform>(),
            new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(20f, 20f), new Vector2(-20f, 36f));

        // Kill
        gameOverKills = MakeText("Öldürülen:  0", box.transform, 18,
            ENEMY_COUNT_COLOR, TextAnchor.MiddleCenter);
        PositionRT(gameOverKills.GetComponent<RectTransform>(),
            new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(20f, -20f), new Vector2(-20f, 30f));

        // Alt not
        var hint = MakeText("Yeniden başlatmak için R tuşuna bas", box.transform, 12,
            new Color(0.6f, 0.6f, 0.65f, 1f), TextAnchor.MiddleCenter);
        PositionRT(hint.GetComponent<RectTransform>(),
            new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f),
            new Vector2(20f, 18f), new Vector2(-20f, 28f));

        gameOverPanel.SetActive(false);
    }

    // ══════════════════════════════════════════════
    // YARDIMCI METODlar
    // ══════════════════════════════════════════════

    GameObject MakePanel(string name, Transform parent,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot,
        Vector2 anchoredPos, Vector2 sizeDelta)
    {
        var go = MakeGO(name, parent);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin; rt.anchorMax = anchorMax; rt.pivot = pivot;
        rt.anchoredPosition = anchoredPos; rt.sizeDelta = sizeDelta;
        go.AddComponent<Image>().color = PANEL_COLOR;
        return go;
    }

    RectTransform MakeBarFill(string name, Transform parent, Color color)
    {
        var go = MakeGO(name, parent);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        go.AddComponent<Image>().color = color;
        return rt;
    }

    Text MakeText(string content, Transform parent, int size, Color color, TextAnchor anchor)
    {
        var go = MakeGO("Txt", parent);
        var t = go.AddComponent<Text>();
        t.text = content; t.fontSize = size; t.color = color;
        t.alignment = anchor;
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.raycastTarget = false;
        return t;
    }

    GameObject MakeGO(string name, Transform parent)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();
        return go;
    }

    void Fill(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    void PositionRT(RectTransform rt,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot,
        Vector2 offsetMin, Vector2 sizeDelta)
    {
        rt.anchorMin = anchorMin; rt.anchorMax = anchorMax; rt.pivot = pivot;
        rt.anchoredPosition = offsetMin; rt.sizeDelta = sizeDelta;
    }
}