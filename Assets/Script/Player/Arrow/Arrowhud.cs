using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// ArrowHUD.cs → Herhangi bir GameObject'e ekle (ör: Player veya GameManager)
///
/// Ekranın sağ altında daire + ok ikonu + sayı gösterir.
/// ArrowCounter.cs ile otomatik senkronize olur.
///
/// KURULUM:
/// 1. Player'a (veya boş bir objeye) ekle
/// 2. arrowSprite alanına ok PNG'sini sürükle (opsiyonel — yoksa metin gösterir)
/// </summary>
public class ArrowHUD : MonoBehaviour
{
    [Header("İkon")]
    public Sprite arrowSprite;   // Ok PNG'si — Inspector'dan ata

    // UI referansları
    private Text countText;
    private Image arrowIcon;
    private Image circleOutline;
    private Image circleFill;     // Ok azaldıkça boşalır
    private GameObject hudPanel;

    private int maxArrows = 10;
    private int currentArrows = 0;
    private float displayedFill = 1f;

    void Start()
    {
        BuildHUD();
        Refresh(10); // Başlangıç

        // ArrowCounter hazır olunca bağlan
        StartCoroutine(ConnectToCounter());
    }

    System.Collections.IEnumerator ConnectToCounter()
    {
        // ArrowCounter'ın Start() çalışmasını bekle
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();

        if (ArrowCounter.Instance != null)
        {
            ArrowCounter.Instance.OnArrowCountChanged += Refresh;
            maxArrows = ArrowCounter.Instance.startingArrows;
            Refresh(ArrowCounter.Instance.GetArrowCount());
            Debug.Log("✅ ArrowHUD: ArrowCounter'a bağlandı");
        }
        else
        {
            Debug.LogWarning("⚠️ ArrowHUD: ArrowCounter bulunamadı!");
        }
    }

    void OnDestroy()
    {
        if (ArrowCounter.Instance != null)
            ArrowCounter.Instance.OnArrowCountChanged -= Refresh;
    }

    void Update()
    {
        // Doluluk animasyonu
        float targetFill = maxArrows > 0 ? (float)currentArrows / maxArrows : 0f;
        displayedFill = Mathf.Lerp(displayedFill, targetFill, Time.deltaTime * 8f);
        if (circleFill != null) circleFill.fillAmount = displayedFill;

        // Renk: az ok → kırmızı
        Color barColor = currentArrows > 3
            ? new Color(0.85f, 0.78f, 0.35f, 1f)   // sarı
            : new Color(0.90f, 0.25f, 0.20f, 1f);   // kırmızı
        if (circleFill != null) circleFill.color = barColor;
        if (countText != null) countText.color = barColor;
    }

    public void Refresh(int count)
    {
        currentArrows = count;
        if (countText != null) countText.text = count.ToString();
    }

    // ─────────────────────────────────────────────────────────
    // UI BUILDER
    // ─────────────────────────────────────────────────────────

    void BuildHUD()
    {
        // Canvas
        GameObject canvasGO = new GameObject("ArrowHUDCanvas");
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 12;
        CanvasScaler cs = canvasGO.AddComponent<CanvasScaler>();
        cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        cs.referenceResolution = new Vector2(1920, 1080);
        canvasGO.AddComponent<GraphicRaycaster>();

        // Ana panel — sağ alt köşe
        hudPanel = new GameObject("ArrowHUD");
        hudPanel.transform.SetParent(canvasGO.transform, false);
        RectTransform panelRT = hudPanel.AddComponent<RectTransform>();
        panelRT.anchorMin = new Vector2(1f, 0f);
        panelRT.anchorMax = new Vector2(1f, 0f);
        panelRT.pivot = new Vector2(1f, 0f);
        panelRT.anchoredPosition = new Vector2(-20f, 20f);
        panelRT.sizeDelta = new Vector2(90f, 90f);

        // ── Dış daire (koyu arka plan) ──
        GameObject circleBgGO = new GameObject("CircleBg");
        circleBgGO.transform.SetParent(hudPanel.transform, false);
        Image circleBg = circleBgGO.AddComponent<Image>();
        circleBg.color = new Color(0.06f, 0.06f, 0.10f, 0.88f);
        circleBg.sprite = CreateCircleSprite();
        circleBg.type = Image.Type.Simple;
        RectTransform cbRT = circleBgGO.GetComponent<RectTransform>();
        cbRT.anchorMin = Vector2.zero; cbRT.anchorMax = Vector2.one;
        cbRT.offsetMin = cbRT.offsetMax = Vector2.zero;

        // ── Doluluk halkası (Filled daire) ──
        GameObject fillGO = new GameObject("CircleFill");
        fillGO.transform.SetParent(hudPanel.transform, false);
        circleFill = fillGO.AddComponent<Image>();
        circleFill.sprite = CreateRingSprite();
        circleFill.type = Image.Type.Filled;
        circleFill.fillMethod = Image.FillMethod.Radial360;
        circleFill.fillOrigin = (int)Image.Origin360.Top;
        circleFill.fillClockwise = true;
        circleFill.fillAmount = 1f;
        circleFill.color = new Color(0.85f, 0.78f, 0.35f, 1f);
        RectTransform fillRT = fillGO.GetComponent<RectTransform>();
        fillRT.anchorMin = Vector2.zero; fillRT.anchorMax = Vector2.one;
        fillRT.offsetMin = fillRT.offsetMax = Vector2.zero;

        // ── Ok ikonu (ortada) ──
        GameObject iconGO = new GameObject("ArrowIcon");
        iconGO.transform.SetParent(hudPanel.transform, false);
        arrowIcon = iconGO.AddComponent<Image>();
        arrowIcon.color = new Color(0.95f, 0.90f, 0.80f, 0.90f);
        if (arrowSprite != null) arrowIcon.sprite = arrowSprite;
        else arrowIcon.enabled = false; // sprite yoksa gizle
        RectTransform iconRT = iconGO.GetComponent<RectTransform>();
        iconRT.anchorMin = new Vector2(0.22f, 0.30f);
        iconRT.anchorMax = new Vector2(0.78f, 0.78f);
        iconRT.offsetMin = iconRT.offsetMax = Vector2.zero;

        // ── Ok sayısı (alta) ──
        GameObject countGO = new GameObject("CountText");
        countGO.transform.SetParent(hudPanel.transform, false);
        countText = countGO.AddComponent<Text>();
        countText.text = "10";
        countText.fontSize = 18;
        countText.fontStyle = FontStyle.Bold;
        countText.color = new Color(0.85f, 0.78f, 0.35f, 1f);
        countText.alignment = TextAnchor.MiddleCenter;
        countText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        RectTransform countRT = countGO.GetComponent<RectTransform>();
        countRT.anchorMin = new Vector2(0f, 0f);
        countRT.anchorMax = new Vector2(1f, 0.35f);
        countRT.offsetMin = countRT.offsetMax = Vector2.zero;

        // ── "OK" etiketi (ikonun üstünde, küçük) ──
        if (arrowSprite == null)
        {
            // Sprite yoksa büyük ok sembolü göster
            GameObject symGO = new GameObject("ArrowSymbol");
            symGO.transform.SetParent(hudPanel.transform, false);
            Text symTxt = symGO.AddComponent<Text>();
            symTxt.text = "↑";
            symTxt.fontSize = 28;
            symTxt.color = new Color(0.95f, 0.90f, 0.75f, 0.85f);
            symTxt.alignment = TextAnchor.MiddleCenter;
            symTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            RectTransform sRT = symGO.GetComponent<RectTransform>();
            sRT.anchorMin = new Vector2(0f, 0.30f);
            sRT.anchorMax = new Vector2(1f, 0.85f);
            sRT.offsetMin = sRT.offsetMax = Vector2.zero;
        }
    }

    // ─────────────────────────────────────────────────────────
    // SPRITE OLUŞTURMA (kod ile daire çizer — sprite gerekmez)
    // ─────────────────────────────────────────────────────────

    Sprite CreateCircleSprite()
    {
        int size = 128;
        Texture2D tex = new Texture2D(size, size);
        Color[] pixels = new Color[size * size];
        float center = size / 2f;
        float radius = size / 2f - 1f;

        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                float alpha = Mathf.Clamp01(radius - dist);
                pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
            }

        tex.SetPixels(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
    }

    Sprite CreateRingSprite()
    {
        int size = 128;
        Texture2D tex = new Texture2D(size, size);
        Color[] pixels = new Color[size * size];
        float center = size / 2f;
        float outerR = size / 2f - 2f;
        float innerR = size / 2f - 14f;

        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                bool inRing = dist >= innerR && dist <= outerR;
                float alpha = inRing ? Mathf.Clamp01(Mathf.Min(outerR - dist, dist - innerR)) : 0f;
                pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
            }

        tex.SetPixels(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
    }
}