using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// GameHUD.cs → Canvas'a ekle (Screen Space Overlay, 1920x1080)
/// Sadece Player can barını gösterir.
/// Enemy barları artık EnemyHealthBar.cs ile her düşmanın üstünde çalışır.
/// </summary>
public class GameHUD : MonoBehaviour
{
    [Header("Referans")]
    public PlayerHealth playerHealth;

    [Header("Renkler")]
    public Color barColor = new Color(0.18f, 0.85f, 0.44f, 1f);  // yeşil
    public Color barLowColor = new Color(0.92f, 0.22f, 0.22f, 1f);  // kırmızı
    public Color barBgColor = new Color(0.08f, 0.08f, 0.10f, 0.85f);
    public Color panelColor = new Color(0.05f, 0.05f, 0.08f, 0.90f);
    public Color flashColor = new Color(0.92f, 0.22f, 0.22f, 0.35f);

    public float flashDuration = 0.25f;

    private RectTransform barFill;
    private RectTransform barDrain;
    private Text hpText;
    private Image damageFlash;

    private float prevHP;
    private float drainTarget = 1f;
    private const float DRAIN_SPEED = 1.2f;
    private const float LOW_HP = 0.30f;
    private const float BAR_W = 280f;
    private const float BAR_H = 14f;

    void Start()
    {
        if (playerHealth == null)
            playerHealth = FindObjectOfType<PlayerHealth>();

        BuildUI();

        prevHP = playerHealth != null ? playerHealth.currentHealth : 100f;
        Refresh(1f, true);
    }

    void Update()
    {
        if (playerHealth == null) return;

        float ratio = Mathf.Clamp01(playerHealth.currentHealth / playerHealth.maxHealth);

        if (!Mathf.Approximately(playerHealth.currentHealth, prevHP))
        {
            bool tookDamage = playerHealth.currentHealth < prevHP;
            prevHP = playerHealth.currentHealth;
            Refresh(ratio, false);
            if (tookDamage) StartCoroutine(Flash());
        }

        // Drain animasyonu
        if (barDrain != null)
        {
            float d = barDrain.anchorMax.x;
            if (d > drainTarget)
                barDrain.anchorMax = new Vector2(
                    Mathf.MoveTowards(d, drainTarget, DRAIN_SPEED * Time.deltaTime), 1f);
        }
    }

    void Refresh(float ratio, bool instant)
    {
        drainTarget = ratio;

        if (barFill != null)
        {
            barFill.anchorMax = new Vector2(ratio, 1f);
            var img = barFill.GetComponent<Image>();
            if (img) img.color = ratio <= LOW_HP ? barLowColor : barColor;
        }

        if (hpText != null && playerHealth != null)
            hpText.text = $"{Mathf.CeilToInt(playerHealth.currentHealth)} / {Mathf.CeilToInt(playerHealth.maxHealth)}";

        if (instant && barDrain != null)
            barDrain.anchorMax = new Vector2(ratio, 1f);
    }

    IEnumerator Flash()
    {
        if (damageFlash == null) yield break;
        damageFlash.color = flashColor;
        float t = 0f;
        while (t < flashDuration)
        {
            t += Time.deltaTime;
            var c = damageFlash.color;
            c.a = Mathf.Lerp(flashColor.a, 0f, t / flashDuration);
            damageFlash.color = c;
            yield return null;
        }
        damageFlash.color = Color.clear;
    }

    // ══════════════════════════════════════
    // UI BUILDER
    // ══════════════════════════════════════

    void BuildUI()
    {
        // Hasar flash — tam ekran
        var flashGO = MakeGO("DamageFlash", transform);
        Fill(flashGO.GetComponent<RectTransform>());
        damageFlash = flashGO.AddComponent<Image>();
        damageFlash.color = Color.clear;
        damageFlash.raycastTarget = false;

        // Panel — sol alt köşe
        var panel = MakeGO("PlayerPanel", transform);
        var pRT = panel.GetComponent<RectTransform>();
        pRT.anchorMin = Vector2.zero;
        pRT.anchorMax = Vector2.zero;
        pRT.pivot = Vector2.zero;
        pRT.anchoredPosition = new Vector2(16f, 16f);
        pRT.sizeDelta = new Vector2(BAR_W + 52f, 74f);
        panel.AddComponent<Image>().color = panelColor;

        // Can ikonu
        var icon = MakeGO("Icon", panel.transform);
        var iRT = icon.GetComponent<RectTransform>();
        iRT.anchorMin = new Vector2(0f, 0.5f);
        iRT.anchorMax = new Vector2(0f, 0.5f);
        iRT.pivot = new Vector2(0f, 0.5f);
        iRT.anchoredPosition = new Vector2(10f, 0f);
        iRT.sizeDelta = new Vector2(34f, 34f);
        icon.AddComponent<Image>().color = barColor;
        var iconTxt = MakeText("♥", icon.transform, 18, new Color(0f, 0.15f, 0.08f, 1f), TextAnchor.MiddleCenter);
        Fill(iconTxt.GetComponent<RectTransform>());

        // "PLAYER" etiketi — sol üst
        var lbl = MakeText("PLAYER", panel.transform, 10, new Color(0.6f, 0.6f, 0.65f, 1f), TextAnchor.MiddleLeft);
        var lRT = lbl.GetComponent<RectTransform>();
        lRT.anchorMin = new Vector2(0f, 1f); lRT.anchorMax = new Vector2(1f, 1f);
        lRT.pivot = new Vector2(0f, 1f);
        lRT.anchoredPosition = new Vector2(52f, -7f);
        lRT.sizeDelta = new Vector2(-60f, 14f);
        lbl.fontStyle = FontStyle.Bold;

        // Can değeri — sağ üst
        hpText = MakeText("100 / 100", panel.transform, 11, Color.white, TextAnchor.MiddleRight);
        var vRT = hpText.GetComponent<RectTransform>();
        vRT.anchorMin = new Vector2(0f, 1f); vRT.anchorMax = new Vector2(1f, 1f);
        vRT.pivot = new Vector2(1f, 1f);
        vRT.anchoredPosition = new Vector2(-8f, -7f);
        vRT.sizeDelta = new Vector2(0f, 14f);

        // Bar arka plan
        var bg = MakeGO("BarBg", panel.transform);
        var bgRT = bg.GetComponent<RectTransform>();
        bgRT.anchorMin = Vector2.zero; bgRT.anchorMax = new Vector2(1f, 0f);
        bgRT.pivot = Vector2.zero;
        bgRT.offsetMin = new Vector2(52f, 12f);
        bgRT.offsetMax = new Vector2(-8f, 12f + BAR_H);
        bg.AddComponent<Image>().color = barBgColor;

        // Drain bar
        barDrain = MakeBarFill("Drain", bg.transform, new Color(1f, 1f, 1f, 0.22f));

        // Dolu bar
        barFill = MakeBarFill("Fill", bg.transform, barColor);
    }

    RectTransform MakeBarFill(string name, Transform parent, Color color)
    {
        var go = MakeGO(name, parent);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        go.AddComponent<Image>().color = color;
        return rt;
    }

    Text MakeText(string content, Transform parent, int size, Color color, TextAnchor anchor)
    {
        var go = MakeGO("T", parent);
        var t = go.AddComponent<Text>();
        t.text = content;
        t.fontSize = size;
        t.color = color;
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
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
}