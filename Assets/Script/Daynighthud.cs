using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// DayNightHUD.cs → Sahnedeki herhangi bir GameObject'e ekle (ör: DayNightManager ile aynı objeye)
///
/// Kendi Canvas'ını otomatik oluşturur — hiçbir şey ataman gerekmez.
/// DayNightCycle.cs ile aynı GameObject'te olması tavsiye edilir.
///
/// GÖSTERİR:
///   - Saat (HH:MM formatında, in-game zaman)
///   - Evre ikonu (☀ Gündüz / 🌆 Akşam / 🌙 Gece / 🌅 Şafak)
///   - Görsel zaman çubuğu (24 saatlik yay)
/// </summary>
public class DayNightHUD : MonoBehaviour
{
    // ────────────────────────────────────────────
    // REFERANS
    // ────────────────────────────────────────────

    private DayNightCycle cycle;

    // ────────────────────────────────────────────
    // UI OBJELERİ
    // ────────────────────────────────────────────

    private Canvas canvas;
    private Image panelBg;
    private Text phaseIcon;
    private Text timeText;
    private Text phaseLabel;
    private Image progressBg;
    private Image progressFill;
    private Image nightOverlay; // Ekranı karartan overlay

    // ────────────────────────────────────────────
    // RENK PALETİ
    // ────────────────────────────────────────────

    static readonly Color colPanelDay = new Color(0.05f, 0.08f, 0.16f, 0.72f);
    static readonly Color colPanelDusk = new Color(0.16f, 0.06f, 0.04f, 0.80f);
    static readonly Color colPanelNight = new Color(0.02f, 0.02f, 0.08f, 0.88f);

    static readonly Color colBarDay = new Color(1.00f, 0.88f, 0.30f, 1f);
    static readonly Color colBarDusk = new Color(1.00f, 0.38f, 0.10f, 1f);
    static readonly Color colBarNight = new Color(0.40f, 0.55f, 1.00f, 1f);
    static readonly Color colBarDawn = new Color(1.00f, 0.60f, 0.25f, 1f);

    static readonly Color colTextDay = new Color(1.00f, 0.95f, 0.75f, 1f);
    static readonly Color colTextDusk = new Color(1.00f, 0.75f, 0.50f, 1f);
    static readonly Color colTextNight = new Color(0.70f, 0.80f, 1.00f, 1f);

    // ────────────────────────────────────────────

    private DayNightCycle.TimeOfDay lastPhase;
    private float smoothFill;

    void Start()
    {
        cycle = FindObjectOfType<DayNightCycle>();
        if (cycle == null)
        {
            Debug.LogError("❌ DayNightHUD: Sahnede DayNightCycle scripti bulunamadı!");
            enabled = false;
            return;
        }

        BuildUI();
        lastPhase = cycle.currentPhase;
    }

    void Update()
    {
        if (cycle == null) return;
        RefreshUI();
    }

    // ────────────────────────────────────────────
    // UI GÜNCELLEME
    // ────────────────────────────────────────────

    void RefreshUI()
    {
        float hour = cycle.GetHour();
        int h = Mathf.FloorToInt(hour) % 24;
        int m = Mathf.FloorToInt((hour % 1f) * 60f);

        // Saat metni
        timeText.text = $"{h:D2}:{m:D2}";

        // Evre ikon + label
        string icon, label;
        Color barCol, textCol, panelCol;
        GetPhaseDisplay(cycle.currentPhase, out icon, out label, out barCol, out textCol, out panelCol);

        phaseIcon.text = icon;
        phaseLabel.text = label;
        phaseIcon.color = barCol;
        timeText.color = textCol;
        phaseLabel.color = textCol;

        // Panel renk geçişi
        if (panelBg != null)
            panelBg.color = Color.Lerp(panelBg.color, panelCol, Time.deltaTime * 1.5f);

        // Progress bar
        float targetFill = cycle.normalizedTime;
        smoothFill = Mathf.Lerp(smoothFill, targetFill, Time.deltaTime * 3f);
        if (progressFill != null)
        {
            progressFill.fillAmount = smoothFill;
            progressFill.color = Color.Lerp(progressFill.color, barCol, Time.deltaTime * 2f);
        }

        // Gece overlay (ekranı karartar)
        if (nightOverlay != null)
        {
            float targetAlpha = 0f;
            if (cycle.currentPhase == DayNightCycle.TimeOfDay.Night)
                targetAlpha = 0.38f;
            else if (cycle.currentPhase == DayNightCycle.TimeOfDay.Dusk)
                targetAlpha = Mathf.Lerp(0f, 0.38f, (cycle.normalizedTime - 0.625f) / (0.875f - 0.625f));

            Color oc = nightOverlay.color;
            oc.a = Mathf.Lerp(oc.a, targetAlpha, Time.deltaTime * 0.8f);
            nightOverlay.color = oc;
        }

        // Faz değişimi animasyonu
        if (cycle.currentPhase != lastPhase)
        {
            lastPhase = cycle.currentPhase;
            StartCoroutine(PhaseChangeAnim());
        }
    }

    void GetPhaseDisplay(DayNightCycle.TimeOfDay phase,
        out string icon, out string label,
        out Color barCol, out Color textCol, out Color panelCol)
    {
        switch (phase)
        {
            case DayNightCycle.TimeOfDay.Day:
                icon = "☀"; label = "GÜNDÜZ";
                barCol = colBarDay; textCol = colTextDay; panelCol = colPanelDay;
                break;
            case DayNightCycle.TimeOfDay.Dusk:
                icon = "🌆"; label = "AKŞAM";
                barCol = colBarDusk; textCol = colTextDusk; panelCol = colPanelDusk;
                break;
            case DayNightCycle.TimeOfDay.Night:
                icon = "🌙"; label = "GECE";
                barCol = colBarNight; textCol = colTextNight; panelCol = colPanelNight;
                break;
            default: // Dawn
                icon = "🌅"; label = "ŞAFAK";
                barCol = colBarDawn; textCol = colTextDay; panelCol = colPanelDay;
                break;
        }
    }

    // ────────────────────────────────────────────
    // ANİMASYON
    // ────────────────────────────────────────────

    IEnumerator PhaseChangeAnim()
    {
        // Panel hafifçe büyüyüp küçülür
        if (panelBg == null) yield break;
        RectTransform rt = panelBg.GetComponent<RectTransform>();
        if (rt == null) yield break;

        Vector3 origScale = rt.localScale;
        float t = 0f;
        while (t < 0.3f)
        {
            t += Time.deltaTime;
            float s = 1f + Mathf.Sin(t / 0.3f * Mathf.PI) * 0.05f;
            rt.localScale = origScale * s;
            yield return null;
        }
        rt.localScale = origScale;
    }

    // ────────────────────────────────────────────
    // UI BUILDER
    // ────────────────────────────────────────────

    void BuildUI()
    {
        // ── CANVAS ──
        GameObject canvasGO = new GameObject("DayNightCanvas");
        canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;
        canvasGO.AddComponent<UnityEngine.UI.CanvasScaler>().uiScaleMode =
            UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasGO.GetComponent<UnityEngine.UI.CanvasScaler>().referenceResolution = new Vector2(1920, 1080);
        canvasGO.AddComponent<UnityEngine.UI.GraphicRaycaster>();

        // ── GECE OVERLAY (tüm ekranı kaplar) ──
        GameObject overlayGO = new GameObject("NightOverlay");
        overlayGO.transform.SetParent(canvasGO.transform, false);
        nightOverlay = overlayGO.AddComponent<Image>();
        nightOverlay.color = new Color(0f, 0.01f, 0.08f, 0f);
        nightOverlay.raycastTarget = false;
        RectTransform overlayRT = overlayGO.GetComponent<RectTransform>();
        overlayRT.anchorMin = Vector2.zero;
        overlayRT.anchorMax = Vector2.one;
        overlayRT.offsetMin = overlayRT.offsetMax = Vector2.zero;

        // ── ANA PANEL (sağ üst köşe) ──
        GameObject panelGO = new GameObject("DayNightPanel");
        panelGO.transform.SetParent(canvasGO.transform, false);

        panelBg = panelGO.AddComponent<Image>();
        panelBg.color = colPanelDay;

        RectTransform pRT = panelGO.GetComponent<RectTransform>();
        pRT.anchorMin = new Vector2(1f, 1f);
        pRT.anchorMax = new Vector2(1f, 1f);
        pRT.pivot = new Vector2(1f, 1f);
        pRT.anchoredPosition = new Vector2(-20f, -20f);
        pRT.sizeDelta = new Vector2(200f, 80f);

        // Yuvarlak köşe efekti için sprite — yoksa düz renk olur
        // (Sprite atanmadıysa dikdörtgen görünür, tamam)

        // ── İKON ──
        phaseIcon = MakeText("☀", panelGO.transform, 28, colBarDay, TextAnchor.MiddleLeft);
        RectTransform iRT = phaseIcon.GetComponent<RectTransform>();
        iRT.anchorMin = new Vector2(0f, 0.5f);
        iRT.anchorMax = new Vector2(0f, 0.5f);
        iRT.pivot = new Vector2(0f, 0.5f);
        iRT.anchoredPosition = new Vector2(12f, 4f);
        iRT.sizeDelta = new Vector2(40f, 40f);

        // ── SAAT ──
        timeText = MakeText("08:00", panelGO.transform, 22, colTextDay, TextAnchor.MiddleLeft);
        timeText.fontStyle = FontStyle.Bold;
        RectTransform tRT = timeText.GetComponent<RectTransform>();
        tRT.anchorMin = new Vector2(0f, 0.5f);
        tRT.anchorMax = new Vector2(1f, 0.5f);
        tRT.pivot = new Vector2(0f, 0.5f);
        tRT.anchoredPosition = new Vector2(56f, 8f);
        tRT.sizeDelta = new Vector2(-64f, 30f);

        // ── EVRE LABEL ──
        phaseLabel = MakeText("GÜNDÜZ", panelGO.transform, 9, colTextDay, TextAnchor.MiddleLeft);
        RectTransform lRT = phaseLabel.GetComponent<RectTransform>();
        lRT.anchorMin = new Vector2(0f, 0f);
        lRT.anchorMax = new Vector2(1f, 0f);
        lRT.pivot = new Vector2(0f, 0f);
        lRT.anchoredPosition = new Vector2(56f, 8f);
        lRT.sizeDelta = new Vector2(-64f, 18f);

        // ── PROGRESS BAR ARKAPLAN ──
        GameObject bgGO = new GameObject("ProgressBg");
        bgGO.transform.SetParent(panelGO.transform, false);
        progressBg = bgGO.AddComponent<Image>();
        progressBg.color = new Color(1f, 1f, 1f, 0.08f);
        RectTransform bgRT = bgGO.GetComponent<RectTransform>();
        bgRT.anchorMin = new Vector2(0f, 0f);
        bgRT.anchorMax = new Vector2(1f, 0f);
        bgRT.pivot = new Vector2(0f, 0f);
        bgRT.anchoredPosition = new Vector2(8f, 6f);
        bgRT.sizeDelta = new Vector2(-16f, 5f);

        // ── PROGRESS BAR DOLUSU ──
        GameObject fillGO = new GameObject("ProgressFill");
        fillGO.transform.SetParent(bgGO.transform, false);
        progressFill = fillGO.AddComponent<Image>();
        progressFill.color = colBarDay;
        progressFill.type = Image.Type.Filled;
        progressFill.fillMethod = Image.FillMethod.Horizontal;
        progressFill.fillAmount = 0f;
        RectTransform fRT = fillGO.GetComponent<RectTransform>();
        fRT.anchorMin = Vector2.zero;
        fRT.anchorMax = Vector2.one;
        fRT.offsetMin = fRT.offsetMax = Vector2.zero;
    }

    // ────────────────────────────────────────────
    // YARDIMCI
    // ────────────────────────────────────────────

    Text MakeText(string content, Transform parent, int size, Color color, TextAnchor anchor)
    {
        var go = new GameObject("T");
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();
        var t = go.AddComponent<Text>();
        t.text = content;
        t.fontSize = size;
        t.color = color;
        t.alignment = anchor;
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.raycastTarget = false;
        return t;
    }
}