using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// GameHUD.cs → Canvas'a ekle (Screen Space Overlay, 1920x1080)
///
/// Player can barı + altında Q ve W büyü cooldown göstergesi
/// Büyü kullanılınca bar dolar, zamanla boşalır (hazır olunca tam açık)
/// </summary>
public class GameHUD : MonoBehaviour
{
    [Header("Referans")]
    public PlayerHealth playerHealth;
    public PlayerAttack playerAttack;

    [Header("Renkler")]
    public Color barColor = new Color(0.18f, 0.85f, 0.44f, 1f);
    public Color barLowColor = new Color(0.92f, 0.22f, 0.22f, 1f);
    public Color barBgColor = new Color(0.08f, 0.08f, 0.10f, 0.85f);
    public Color panelColor = new Color(0.05f, 0.05f, 0.08f, 0.90f);
    public Color flashColor = new Color(0.92f, 0.22f, 0.22f, 0.35f);
    public Color spellQColor = new Color(0.20f, 0.75f, 1.00f, 1f);  // Q — mavi
    public Color spellWColor = new Color(1.00f, 0.45f, 0.10f, 1f);  // W — turuncu
    public Color spellBgColor = new Color(0.08f, 0.08f, 0.12f, 0.90f);
    public Color spellReadyColor = new Color(1f, 1f, 1f, 0.15f);        // Hazır parlaması

    public float flashDuration = 0.25f;

    // Player can barı
    private RectTransform barFill;
    private RectTransform barDrain;
    private Text hpText;
    private Image damageFlash;

    // Büyü cooldown
    private RectTransform qBarFill;
    private RectTransform wBarFill;
    private Image qIcon;
    private Image wIcon;
    private Text qLabel;
    private Text wLabel;

    private float prevHP;
    private float drainTarget = 1f;

    private const float DRAIN_SPEED = 1.2f;
    private const float LOW_HP = 0.30f;
    private const float BAR_W = 280f;
    private const float BAR_H = 14f;
    private const float SPELL_W = 130f;
    private const float SPELL_H = 8f;

    void Start()
    {
        if (playerHealth == null) playerHealth = FindObjectOfType<PlayerHealth>();
        if (playerAttack == null) playerAttack = FindObjectOfType<PlayerAttack>();

        BuildUI();

        prevHP = playerHealth != null ? playerHealth.currentHealth : 100f;
        Refresh(1f, true);
    }

    void Update()
    {
        UpdateHPBar();
        UpdateSpellCooldowns();
    }

    void UpdateHPBar()
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

        if (barDrain != null)
        {
            float d = barDrain.anchorMax.x;
            if (d > drainTarget)
                barDrain.anchorMax = new Vector2(
                    Mathf.MoveTowards(d, drainTarget, DRAIN_SPEED * Time.deltaTime), 1f);
        }
    }

    void UpdateSpellCooldowns()
    {
        if (playerAttack == null) return;

        // Q cooldown
        if (qBarFill != null)
        {
            float elapsed = Time.time - playerAttack.LastProjectileTime;
            float ratio = Mathf.Clamp01(elapsed / playerAttack.ProjectileCooldown);
            qBarFill.anchorMax = new Vector2(ratio, 1f);

            // Hazır olunca label temiz, doluyken kalan süreyi göster
            if (qLabel != null)
            {
                if (ratio >= 1f)
                    qLabel.text = "Q";
                else
                    qLabel.text = $"Q  {(playerAttack.ProjectileCooldown - elapsed):F1}s";
            }

            // Hazır rengi
            if (qIcon != null)
                qIcon.color = ratio >= 1f ? spellQColor : new Color(spellQColor.r, spellQColor.g, spellQColor.b, 0.35f);
        }

        // W cooldown
        if (wBarFill != null)
        {
            float elapsed = Time.time - playerAttack.LastFireballTime;
            float ratio = Mathf.Clamp01(elapsed / playerAttack.FireballCooldown);
            wBarFill.anchorMax = new Vector2(ratio, 1f);

            if (wLabel != null)
            {
                if (ratio >= 1f)
                    wLabel.text = "W";
                else
                    wLabel.text = $"W  {(playerAttack.FireballCooldown - elapsed):F1}s";
            }

            if (wIcon != null)
                wIcon.color = ratio >= 1f ? spellWColor : new Color(spellWColor.r, spellWColor.g, spellWColor.b, 0.35f);
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
        Canvas canvas = GetComponent<Canvas>();
        if (canvas == null) { Debug.LogError("❌ GameHUD: Canvas yok!"); return; }

        // Hasar flash
        var flashGO = MakeGO("DamageFlash", transform);
        Fill(flashGO.GetComponent<RectTransform>());
        damageFlash = flashGO.AddComponent<Image>();
        damageFlash.color = Color.clear;
        damageFlash.raycastTarget = false;

        // Ana panel — sol alt (can + büyüler)
        BuildPlayerPanel();
    }

    void BuildPlayerPanel()
    {
        // Panel
        var panel = MakeGO("PlayerPanel", transform);
        var pRT = panel.GetComponent<RectTransform>();
        pRT.anchorMin = Vector2.zero;
        pRT.anchorMax = Vector2.zero;
        pRT.pivot = Vector2.zero;
        pRT.anchoredPosition = new Vector2(16f, 16f);
        pRT.sizeDelta = new Vector2(BAR_W + 52f, 130f); // Büyü için yükseklik artırıldı
        panel.AddComponent<Image>().color = panelColor;

        // ── CAN BARI KISMI ──

        // İkon
        var icon = MakeGO("Icon", panel.transform);
        var iRT = icon.GetComponent<RectTransform>();
        iRT.anchorMin = new Vector2(0f, 1f);
        iRT.anchorMax = new Vector2(0f, 1f);
        iRT.pivot = new Vector2(0f, 1f);
        iRT.anchoredPosition = new Vector2(10f, -10f);
        iRT.sizeDelta = new Vector2(34f, 34f);
        icon.AddComponent<Image>().color = barColor;
        var iconTxt = MakeText("♥", icon.transform, 18, new Color(0f, 0.15f, 0.08f, 1f), TextAnchor.MiddleCenter);
        Fill(iconTxt.GetComponent<RectTransform>());

        // PLAYER etiketi
        var lbl = MakeText("PLAYER", panel.transform, 10, new Color(0.6f, 0.6f, 0.65f, 1f), TextAnchor.MiddleLeft);
        var lRT = lbl.GetComponent<RectTransform>();
        lRT.anchorMin = new Vector2(0f, 1f); lRT.anchorMax = new Vector2(1f, 1f);
        lRT.pivot = new Vector2(0f, 1f);
        lRT.anchoredPosition = new Vector2(52f, -10f);
        lRT.sizeDelta = new Vector2(-60f, 14f);
        lbl.fontStyle = FontStyle.Bold;

        // Can değeri
        hpText = MakeText("100 / 100", panel.transform, 11, Color.white, TextAnchor.MiddleRight);
        var vRT = hpText.GetComponent<RectTransform>();
        vRT.anchorMin = new Vector2(0f, 1f); vRT.anchorMax = new Vector2(1f, 1f);
        vRT.pivot = new Vector2(1f, 1f);
        vRT.anchoredPosition = new Vector2(-8f, -10f);
        vRT.sizeDelta = new Vector2(0f, 14f);

        // Can bar arka plan
        var bg = MakeGO("BarBg", panel.transform);
        var bgRT = bg.GetComponent<RectTransform>();
        bgRT.anchorMin = new Vector2(0f, 1f); bgRT.anchorMax = new Vector2(1f, 1f);
        bgRT.pivot = new Vector2(0f, 1f);
        bgRT.anchoredPosition = new Vector2(52f, -32f);
        bgRT.sizeDelta = new Vector2(-60f, BAR_H);
        bg.AddComponent<Image>().color = barBgColor;

        barDrain = MakeBarFill("Drain", bg.transform, new Color(1f, 1f, 1f, 0.22f));
        barFill = MakeBarFill("Fill", bg.transform, barColor);

        // ── BÜYÜ COOLDOWN KISMI ──

        // Ayırıcı çizgi
        var sep = MakeGO("Sep", panel.transform);
        var sepRT = sep.GetComponent<RectTransform>();
        sepRT.anchorMin = new Vector2(0f, 1f); sepRT.anchorMax = new Vector2(1f, 1f);
        sepRT.pivot = new Vector2(0f, 1f);
        sepRT.anchoredPosition = new Vector2(8f, -54f);
        sepRT.sizeDelta = new Vector2(-16f, 1f);
        sep.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.06f);

        // Q büyüsü
        BuildSpellSlot(panel.transform, "Q", spellQColor, -62f, out qIcon, out qBarFill, out qLabel);

        // W büyüsü
        BuildSpellSlot(panel.transform, "W", spellWColor, -96f, out wIcon, out wBarFill, out wLabel);
    }

    void BuildSpellSlot(Transform parent, string key, Color color, float yOffset,
        out Image iconImg, out RectTransform fillRT, out Text labelTxt)
    {
        // İkon kutusu
        var slot = MakeGO($"Spell_{key}", parent);
        var slotRT = slot.GetComponent<RectTransform>();
        slotRT.anchorMin = new Vector2(0f, 1f); slotRT.anchorMax = new Vector2(0f, 1f);
        slotRT.pivot = new Vector2(0f, 1f);
        slotRT.anchoredPosition = new Vector2(10f, yOffset);
        slotRT.sizeDelta = new Vector2(26f, 26f);
        iconImg = slot.AddComponent<Image>();
        iconImg.color = color;

        // Tuş harfi
        var keyTxt = MakeText(key, slot.transform, 12, Color.white, TextAnchor.MiddleCenter);
        Fill(keyTxt.GetComponent<RectTransform>());
        keyTxt.fontStyle = FontStyle.Bold;

        // Cooldown bar arka plan
        var barBg = MakeGO($"SpellBg_{key}", parent);
        var barBgRT = barBg.GetComponent<RectTransform>();
        barBgRT.anchorMin = new Vector2(0f, 1f); barBgRT.anchorMax = new Vector2(1f, 1f);
        barBgRT.pivot = new Vector2(0f, 1f);
        barBgRT.anchoredPosition = new Vector2(44f, yOffset + 7f);
        barBgRT.sizeDelta = new Vector2(-52f, SPELL_H);
        barBg.AddComponent<Image>().color = spellBgColor;

        // Dolan bar (0→1 = cooldown bitti → hazır)
        var fill = MakeGO($"SpellFill_{key}", barBg.transform);
        fillRT = fill.GetComponent<RectTransform>();
        fillRT.anchorMin = Vector2.zero;
        fillRT.anchorMax = Vector2.one;
        fillRT.offsetMin = Vector2.zero;
        fillRT.offsetMax = Vector2.zero;
        var fillImg = fill.AddComponent<Image>();
        fillImg.color = color;

        // Kalan süre / Hazır etiketi
        labelTxt = MakeText(key, parent, 10, new Color(0.8f, 0.8f, 0.85f, 1f), TextAnchor.MiddleLeft);
        var lRT = labelTxt.GetComponent<RectTransform>();
        lRT.anchorMin = new Vector2(0f, 1f); lRT.anchorMax = new Vector2(1f, 1f);
        lRT.pivot = new Vector2(0f, 1f);
        lRT.anchoredPosition = new Vector2(44f, yOffset - 4f);
        lRT.sizeDelta = new Vector2(-52f, 14f);
    }

    // ── YARDIMCI ──

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