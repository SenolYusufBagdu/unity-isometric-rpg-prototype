using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// EnemyHealthBar.cs → EnemyHealth scripti olan her düşmana otomatik eklenir.
/// Prefab veya manuel kurulum gerekmez — Start()'ta kendi Canvas'ını oluşturur.
///
/// KURULUM:
/// 1. Bu scripti EnemyHealth ile aynı GameObject'e ekle
/// 2. Hiçbir şey atamana gerek yok — otomatik çalışır
///
/// NOT: Camera.main kullanır. Main Camera tag'inin atanmış olduğundan emin ol.
/// </summary>
[RequireComponent(typeof(EnemyHealth))]
public class EnemyHealthBar : MonoBehaviour
{
    [Header("Konum Ayarları")]
    public float heightOffset = 2.4f;   // Düşmanın ne kadar üstünde durur
    public float barWidth = 1.8f;   // Barın genişliği (dünya birimi)
    public float barHeight = 0.18f;  // Barın yüksekliği
    public float canvasScale = 0.01f;  // World Space canvas scale

    [Header("Görünüm")]
    public Color fillColor = new Color(0.88f, 0.22f, 0.22f, 1f);   // kırmızı
    public Color drainColor = new Color(1f, 0.85f, 0.85f, 0.55f); // soluk beyaz drain
    public Color bgColor = new Color(0.05f, 0.05f, 0.07f, 0.85f); // koyu arka plan
    public Color borderColor = new Color(1f, 1f, 1f, 0.08f);  // kenarlık
    public Color lowHpColor = new Color(0.95f, 0.40f, 0.10f, 1f);    // turuncu (düşük can)
    public float lowHpThreshold = 0.30f;

    [Header("Animasyon")]
    public float drainDelay = 0.4f;   // Drain gecikmesi (saniye)
    public float drainSpeed = 1.5f;   // Drain hızı
    public bool alwaysVisible = false;  // false = yalnızca hasar alınca göster
    public float hideDelay = 3f;     // Hasar sonrası kaç saniye görünsün

    // UI referansları
    private GameObject canvasGO;
    private RectTransform fillRT;
    private RectTransform drainRT;
    private Image fillImg;
    private CanvasGroup group;

    private EnemyHealth eh;
    private Camera mainCam;

    private float targetFill = 1f;
    private float drainTarget = 1f;
    private float drainTimer = 0f;
    private bool isDraining = false;
    private float hideTimer = 0f;
    private bool shouldHide = false;

    void Start()
    {
        eh = GetComponent<EnemyHealth>();
        mainCam = Camera.main;

        BuildCanvas();

        // Başlangıçta dolu göster
        SetFill(1f, true);

        if (!alwaysVisible) SetAlpha(0f);
    }

    void Update()
    {
        if (eh == null || canvasGO == null) return;

        // Kameraya bak (billboard)
        if (mainCam != null)
            canvasGO.transform.rotation = mainCam.transform.rotation;

        // Can değişimini izle
        float currentRatio = Mathf.Clamp01(eh.currentHealth / eh.maxHealth);

        if (!Mathf.Approximately(currentRatio, targetFill))
        {
            float prev = targetFill;
            targetFill = currentRatio;

            // Bar anında güncelle
            SetFill(targetFill, false);

            // Drain başlat
            drainTimer = drainDelay;
            isDraining = false;

            // Görünür yap
            if (!alwaysVisible)
            {
                shouldHide = false;
                SetAlpha(1f);
                hideTimer = hideDelay;
            }
        }

        // Drain animasyonu
        if (drainTimer > 0f)
        {
            drainTimer -= Time.deltaTime;
            if (drainTimer <= 0f) isDraining = true;
        }

        if (isDraining && drainRT != null)
        {
            float cur = drainRT.anchorMax.x;
            if (cur > targetFill)
            {
                float next = Mathf.MoveTowards(cur, targetFill, drainSpeed * Time.deltaTime);
                drainRT.anchorMax = new Vector2(next, 1f);
            }
            else isDraining = false;
        }

        // Gizleme zamanlayıcısı
        if (!alwaysVisible && hideTimer > 0f)
        {
            hideTimer -= Time.deltaTime;
            if (hideTimer <= 0f)
                StartCoroutine(FadeOut());
        }
    }

    void SetFill(float ratio, bool instant)
    {
        if (fillRT == null) return;
        fillRT.anchorMax = new Vector2(ratio, 1f);

        // Düşük can rengi
        if (fillImg != null)
            fillImg.color = ratio <= lowHpThreshold ? lowHpColor : fillColor;

        if (instant && drainRT != null)
            drainRT.anchorMax = new Vector2(ratio, 1f);
    }

    void SetAlpha(float a)
    {
        if (group != null) group.alpha = a;
    }

    IEnumerator FadeOut()
    {
        if (group == null) yield break;
        float start = group.alpha;
        float t = 0f;
        float dur = 0.5f;
        while (t < dur)
        {
            t += Time.deltaTime;
            group.alpha = Mathf.Lerp(start, 0f, t / dur);
            yield return null;
        }
        group.alpha = 0f;
    }

    // ══════════════════════════════════════════════════
    // CANVAS BUILDER
    // ══════════════════════════════════════════════════

    void BuildCanvas()
    {
        // World Space Canvas
        canvasGO = new GameObject("EnemyHPBar_Canvas");
        canvasGO.transform.SetParent(transform);
        canvasGO.transform.localPosition = new Vector3(0f, heightOffset, 0f);
        canvasGO.transform.localScale = Vector3.one * canvasScale;

        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 5;

        group = canvasGO.AddComponent<CanvasGroup>();
        group.interactable = false;
        group.blocksRaycasts = false;

        RectTransform canvasRT = canvasGO.GetComponent<RectTransform>();
        canvasRT.sizeDelta = new Vector2(barWidth / canvasScale, 30f);

        // Arka plan
        GameObject bg = new GameObject("BG");
        bg.transform.SetParent(canvasGO.transform, false);
        Image bgImg = bg.AddComponent<Image>();
        bgImg.color = bgColor;
        RectTransform bgRT = bg.GetComponent<RectTransform>();
        bgRT.anchorMin = new Vector2(0f, 0f);
        bgRT.anchorMax = new Vector2(1f, 1f);
        bgRT.offsetMin = Vector2.zero;
        bgRT.offsetMax = Vector2.zero;

        // Barın boyut alanı — padding ile
        float padH = 3f, padV = 4f;
        GameObject barArea = new GameObject("BarArea");
        barArea.transform.SetParent(canvasGO.transform, false);
        RectTransform areaRT = barArea.AddComponent<RectTransform>();
        areaRT.anchorMin = Vector2.zero;
        areaRT.anchorMax = Vector2.one;
        areaRT.offsetMin = new Vector2(padH, padV);
        areaRT.offsetMax = new Vector2(-padH, -padV);

        // Drain bar (gecikmeli soluk bar)
        GameObject drainGO = new GameObject("Drain");
        drainGO.transform.SetParent(barArea.transform, false);
        Image drainImg = drainGO.AddComponent<Image>();
        drainImg.color = drainColor;
        drainRT = drainGO.GetComponent<RectTransform>();
        drainRT.anchorMin = new Vector2(0f, 0f);
        drainRT.anchorMax = new Vector2(1f, 1f);
        drainRT.offsetMin = Vector2.zero;
        drainRT.offsetMax = Vector2.zero;

        // Dolu bar
        GameObject fillGO = new GameObject("Fill");
        fillGO.transform.SetParent(barArea.transform, false);
        fillImg = fillGO.AddComponent<Image>();
        fillImg.color = fillColor;
        fillRT = fillGO.GetComponent<RectTransform>();
        fillRT.anchorMin = new Vector2(0f, 0f);
        fillRT.anchorMax = new Vector2(1f, 1f);
        fillRT.offsetMin = Vector2.zero;
        fillRT.offsetMax = Vector2.zero;

        // Kenarlık çizgisi (ince beyaz border)
        GameObject border = new GameObject("Border");
        border.transform.SetParent(canvasGO.transform, false);
        Image borderImg = border.AddComponent<Image>();
        borderImg.color = Color.clear;
        RectTransform bRT = border.GetComponent<RectTransform>();
        bRT.anchorMin = Vector2.zero;
        bRT.anchorMax = Vector2.one;
        bRT.offsetMin = Vector2.zero;
        bRT.offsetMax = Vector2.zero;
    }
}