using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// CardSelectionUI.cs → Herhangi boş bir GameObject'e ekle. Canvas'a gerek YOK.
///
/// SORUNLAR VE ÇÖZÜMLER:
/// 1) Tıklanamıyor → Artık KENDİ Canvas'ını oluşturuyor (sortingOrder=99, en üstte)
///    Başka hiçbir panel tıklamayı bloklayamaz.
/// 2) Font uyarıları → Emoji kaldırıldı, düz metin kullanılıyor (⚔ → [K], ✨ → [B] gibi değil,
///    sadece Latin harfleri ve noktalama). TextMeshPro hata vermez.
/// 3) GraphicRaycaster / EventSystem eksikliği → Kendi Canvas'ına otomatik ekliyor.
/// </summary>
public class CardSelectionUI : MonoBehaviour
{
    public static CardSelectionUI Instance { get; private set; }

    [Header("Ayarlar")]
    public float cardAnimDuration = 0.3f;

    // İç referanslar
    private Canvas myCanvas;
    private GameObject overlayRoot;
    private GameObject cardContainer;
    private TextMeshProUGUI levelText;

    // Kart verisi
    private struct CardData
    {
        public string title;
        public string description;
        public string tag;      // Kısa etiket — emoji YOK, font hatası olmasın
        public Color color;
        public System.Action onSelect;
    }

    private readonly List<CardData> allCards = new List<CardData>();
    private bool isShowing;

    // ══════════════════════════════════════════════
    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;
    }

    void Start()
    {
        BuildAllCards();
        BuildOwnCanvas();

        if (WaveManager.Instance != null)
            WaveManager.Instance.OnWaveCompleted += OnWaveCompleted;
        else
            StartCoroutine(SubscribeLate());

        overlayRoot.SetActive(false);
    }

    void OnDestroy()
    {
        if (WaveManager.Instance != null)
            WaveManager.Instance.OnWaveCompleted -= OnWaveCompleted;
    }

    IEnumerator SubscribeLate()
    {
        yield return new WaitForSeconds(0.5f);
        if (WaveManager.Instance != null)
            WaveManager.Instance.OnWaveCompleted += OnWaveCompleted;
    }

    // ══════════════════════════════════════════════
    // KENDİ CANVAS'INI KUR
    // ══════════════════════════════════════════════

    void BuildOwnCanvas()
    {
        // --- Canvas objesi ---
        GameObject canvasGO = new GameObject("CardSelectionCanvas");
        DontDestroyOnLoad(canvasGO);   // sahne geçişinde kaybolmasın

        myCanvas = canvasGO.AddComponent<Canvas>();
        myCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        myCanvas.sortingOrder = 99;    // Her şeyin üstünde

        CanvasScaler cs = canvasGO.AddComponent<CanvasScaler>();
        cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        cs.referenceResolution = new Vector2(1920, 1080);
        cs.matchWidthOrHeight = 0.5f;

        // GraphicRaycaster — olmadan tıklama çalışmaz
        canvasGO.AddComponent<GraphicRaycaster>();

        // EventSystem — sahnede yoksa ekle
        if (FindObjectOfType<EventSystem>() == null)
        {
            GameObject esGO = new GameObject("EventSystem");
            esGO.AddComponent<EventSystem>();
            esGO.AddComponent<StandaloneInputModule>();
            DontDestroyOnLoad(esGO);
        }

        // --- Karartma katmanı ---
        overlayRoot = new GameObject("Overlay");
        overlayRoot.transform.SetParent(canvasGO.transform, false);

        RectTransform ort = overlayRoot.AddComponent<RectTransform>();
        ort.anchorMin = Vector2.zero;
        ort.anchorMax = Vector2.one;
        ort.sizeDelta = Vector2.zero;

        Image overlayImg = overlayRoot.AddComponent<Image>();
        overlayImg.color = new Color(0f, 0f, 0f, 0.80f);
        overlayImg.raycastTarget = false;   // overlay tıklamayı BLOKLAMAZ

        overlayRoot.AddComponent<CanvasGroup>();

        // --- Orta kutu ---
        GameObject box = MakeRT("Box", overlayRoot.transform);
        RectTransform boxRT = box.GetComponent<RectTransform>();
        boxRT.anchorMin = new Vector2(0.5f, 0.5f);
        boxRT.anchorMax = new Vector2(0.5f, 0.5f);
        boxRT.pivot = new Vector2(0.5f, 0.5f);
        boxRT.sizeDelta = new Vector2(760f, 420f);
        boxRT.anchoredPosition = Vector2.zero;

        Image boxImg = box.AddComponent<Image>();
        boxImg.color = new Color(0.06f, 0.04f, 0.10f, 0.96f);
        boxImg.raycastTarget = false;

        // --- Baslik ---
        GameObject titleGO = MakeRT("Title", box.transform);
        Pos(titleGO, new Vector2(0f, 170f), new Vector2(700f, 48f));
        TextMeshProUGUI titleTMP = titleGO.AddComponent<TextMeshProUGUI>();
        titleTMP.text = "GUCLEN";
        titleTMP.fontSize = 38;
        titleTMP.fontStyle = FontStyles.Bold;
        titleTMP.alignment = TextAlignmentOptions.Center;
        titleTMP.color = new Color(0.95f, 0.82f, 0.35f);
        titleTMP.raycastTarget = false;

        // --- Seviye ---
        GameObject lvlGO = MakeRT("Level", box.transform);
        Pos(lvlGO, new Vector2(0f, 128f), new Vector2(500f, 32f));
        levelText = lvlGO.AddComponent<TextMeshProUGUI>();
        levelText.text = "SEVIYE 1";
        levelText.fontSize = 20;
        levelText.alignment = TextAlignmentOptions.Center;
        levelText.color = new Color(0.65f, 0.55f, 0.85f);
        levelText.raycastTarget = false;

        // --- Kart container ---
        cardContainer = MakeRT("CardContainer", box.transform);
        Pos(cardContainer, new Vector2(0f, -20f), new Vector2(730f, 300f));

        HorizontalLayoutGroup hlg = cardContainer.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 20f;
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = false;
        hlg.padding = new RectOffset(8, 8, 4, 4);

        Image ccImg = cardContainer.AddComponent<Image>();
        ccImg.color = Color.clear;
        ccImg.raycastTarget = false;
    }

    // ══════════════════════════════════════════════
    // KART HAVUZU  (emoji yok → font hatası yok)
    // ══════════════════════════════════════════════

    void BuildAllCards()
    {
        allCards.Clear();

        Add("Buyu Gucu", "Buyu hasarin\n%15 artar", "BG", new Color(0.55f, 0.20f, 0.90f),
            () => PlayerLevelSystem.Instance?.UpgradeSpellDamage(0.15f));

        Add("Kilic Ustasi", "Kilic hasarin\n%20 artar", "KU", new Color(0.85f, 0.30f, 0.10f),
            () => PlayerLevelSystem.Instance?.UpgradeSwordDamage(0.20f));

        Add("Okcu Ruhu", "Ok hasarin\n%20 artar", "OK", new Color(0.20f, 0.65f, 0.20f),
            () => PlayerLevelSystem.Instance?.UpgradeArrowDamage(0.20f));

        Add("Demir Yurek", "Maksimum can\n%30 artar", "CAN", new Color(0.85f, 0.10f, 0.20f),
            () => PlayerLevelSystem.Instance?.UpgradeMaxHealth(0.30f));

        Add("Ruzgar Adimi", "Hareket hizi\n%20 artar", "HIZ", new Color(0.10f, 0.60f, 0.85f),
            () => PlayerLevelSystem.Instance?.UpgradeMoveSpeed(0.20f));

        Add("Buyuk Buyu", "Buyu hasarin\n%25 artar", "BB", new Color(0.70f, 0.10f, 0.85f),
            () => PlayerLevelSystem.Instance?.UpgradeSpellDamage(0.25f));

        Add("Kanli Bicak", "Kilic hasarin\n%30 artar", "KB", new Color(0.90f, 0.10f, 0.10f),
            () => PlayerLevelSystem.Instance?.UpgradeSwordDamage(0.30f));

        Add("Demir Zirh", "Maksimum can\n%20 artar", "ZRH", new Color(0.50f, 0.50f, 0.65f),
            () => PlayerLevelSystem.Instance?.UpgradeMaxHealth(0.20f));
    }

    void Add(string title, string desc, string tag, Color color, System.Action action)
    {
        allCards.Add(new CardData
        {
            title = title,
            description = desc,
            tag = tag,
            color = color,
            onSelect = action
        });
    }

    // ══════════════════════════════════════════════
    // GÖSTER / GİZLE
    // ══════════════════════════════════════════════

    void OnWaveCompleted(int waveNum) => ShowCards(waveNum);

    public void ShowCards(int waveNum = 0)
    {
        if (isShowing) return;
        isShowing = true;
        Time.timeScale = 0f;

        if (levelText != null && PlayerLevelSystem.Instance != null)
            levelText.text = "SEVIYE " + PlayerLevelSystem.Instance.currentLevel;

        // Eski kartları temizle
        foreach (Transform t in cardContainer.transform)
            Destroy(t.gameObject);

        // 3 rastgele kart
        List<CardData> pool = new List<CardData>(allCards);
        for (int i = 0; i < 3 && pool.Count > 0; i++)
        {
            int idx = Random.Range(0, pool.Count);
            CreateCard(pool[idx]);
            pool.RemoveAt(idx);
        }

        overlayRoot.SetActive(true);
        StartCoroutine(FadeIn());
    }

    public void Hide()
    {
        isShowing = false;
        Time.timeScale = 1f;
        if (overlayRoot != null) overlayRoot.SetActive(false);
    }

    IEnumerator FadeIn()
    {
        CanvasGroup cg = overlayRoot.GetComponent<CanvasGroup>();
        cg.alpha = 0f;
        float t = 0f;
        while (t < cardAnimDuration)
        {
            t += Time.unscaledDeltaTime;
            cg.alpha = Mathf.Clamp01(t / cardAnimDuration);
            yield return null;
        }
        cg.alpha = 1f;
    }

    // ══════════════════════════════════════════════
    // KART OLUŞTUR
    // ══════════════════════════════════════════════

    void CreateCard(CardData data)
    {
        // Ana kart objesi
        GameObject card = MakeRT("Card_" + data.title, cardContainer.transform);

        LayoutElement le = card.AddComponent<LayoutElement>();
        le.preferredWidth = 215f;
        le.preferredHeight = 285f;
        le.minWidth = 215f;
        le.minHeight = 285f;

        Image cardBg = card.AddComponent<Image>();
        cardBg.color = new Color(0.09f, 0.06f, 0.13f, 1f);
        // raycastTarget=TRUE → Button'ın çalışması için gerekli
        cardBg.raycastTarget = true;

        Button btn = card.AddComponent<Button>();
        ColorBlock cb = btn.colors;
        cb.normalColor = new Color(0.09f, 0.06f, 0.13f, 1f);
        cb.highlightedColor = new Color(data.color.r * 0.30f, data.color.g * 0.30f, data.color.b * 0.30f, 1f);
        cb.pressedColor = data.color * 0.50f;
        cb.fadeDuration = 0.08f;
        btn.colors = cb;

        // Closure için yerel kopya
        System.Action act = data.onSelect;
        btn.onClick.AddListener(() => { act?.Invoke(); Hide(); });

        // --- Üst renkli şerit ---
        GameObject stripe = MakeRT("Stripe", card.transform);
        RectTransform srt = stripe.GetComponent<RectTransform>();
        srt.anchorMin = new Vector2(0f, 1f);
        srt.anchorMax = new Vector2(1f, 1f);
        srt.pivot = new Vector2(0.5f, 1f);
        srt.sizeDelta = new Vector2(0f, 6f);
        srt.anchoredPosition = Vector2.zero;
        Image stripeImg = stripe.AddComponent<Image>();
        stripeImg.color = data.color;
        stripeImg.raycastTarget = false;

        // --- Kısa etiket kutusu (emoji yerine) ---
        GameObject tagBox = MakeRT("TagBox", card.transform);
        Pos(tagBox, new Vector2(0f, 88f), new Vector2(70f, 70f));
        Image tagBg = tagBox.AddComponent<Image>();
        tagBg.color = new Color(data.color.r, data.color.g, data.color.b, 0.25f);
        tagBg.raycastTarget = false;

        GameObject tagTxt = MakeRT("TagTxt", tagBox.transform);
        FillRT(tagTxt);
        TextMeshProUGUI tagTMP = tagTxt.AddComponent<TextMeshProUGUI>();
        tagTMP.text = data.tag;
        tagTMP.fontSize = 22;
        tagTMP.fontStyle = FontStyles.Bold;
        tagTMP.alignment = TextAlignmentOptions.Center;
        tagTMP.color = data.color;
        tagTMP.raycastTarget = false;

        // --- Başlık ---
        GameObject titleGO = MakeRT("Title", card.transform);
        Pos(titleGO, new Vector2(0f, 30f), new Vector2(195f, 44f));
        TextMeshProUGUI titleTMP = titleGO.AddComponent<TextMeshProUGUI>();
        titleTMP.text = data.title;
        titleTMP.fontSize = 17f;
        titleTMP.fontStyle = FontStyles.Bold;
        titleTMP.alignment = TextAlignmentOptions.Center;
        titleTMP.color = data.color;
        titleTMP.raycastTarget = false;

        // --- Açıklama ---
        GameObject descGO = MakeRT("Desc", card.transform);
        Pos(descGO, new Vector2(0f, -38f), new Vector2(190f, 72f));
        TextMeshProUGUI descTMP = descGO.AddComponent<TextMeshProUGUI>();
        descTMP.text = data.description;
        descTMP.fontSize = 14f;
        descTMP.alignment = TextAlignmentOptions.Center;
        descTMP.color = new Color(0.78f, 0.72f, 0.85f);
        descTMP.raycastTarget = false;

        // --- Sec butonu görsel (tıklama card'dan geliyor) ---
        GameObject secBox = MakeRT("SecBox", card.transform);
        Pos(secBox, new Vector2(0f, -118f), new Vector2(170f, 34f));
        Image secImg = secBox.AddComponent<Image>();
        secImg.color = new Color(data.color.r * 0.65f, data.color.g * 0.65f, data.color.b * 0.65f, 1f);
        secImg.raycastTarget = false;

        GameObject secTxtGO = MakeRT("SecTxt", secBox.transform);
        FillRT(secTxtGO);
        TextMeshProUGUI secTMP = secTxtGO.AddComponent<TextMeshProUGUI>();
        secTMP.text = "SEC";
        secTMP.fontSize = 15f;
        secTMP.fontStyle = FontStyles.Bold;
        secTMP.alignment = TextAlignmentOptions.Center;
        secTMP.color = Color.white;
        secTMP.raycastTarget = false;
    }

    // ══════════════════════════════════════════════
    // YARDIMCI
    // ══════════════════════════════════════════════

    GameObject MakeRT(string n, Transform parent)
    {
        var go = new GameObject(n);
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();
        return go;
    }

    void Pos(GameObject go, Vector2 anchPos, Vector2 size)
    {
        var rt = go.GetComponent<RectTransform>();
        rt.anchoredPosition = anchPos;
        rt.sizeDelta = size;
    }

    void FillRT(GameObject go)
    {
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.sizeDelta = Vector2.zero;
        rt.anchoredPosition = Vector2.zero;
    }
}