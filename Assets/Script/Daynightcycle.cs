using UnityEngine;
using UnityEngine.Rendering;
using System.Collections;

/// <summary>
/// DayNightCycle.cs → Sahnedeki boş bir GameObject'e ekle (ör: "DayNightManager")
///
/// TOPLAM DÖNGÜ: 4 dakika (240 saniye)
///   - Gündüz  : 0 dk → 2 dk  (120 sn) — açık, parlak
///   - Akşam   : 2 dk → 3 dk  (60 sn)  — turuncu-kırmızı geçiş
///   - Gece    : 3 dk → 4 dk  (120 sn) — karanlık, mavi
///   - Şafak   : 4 dk → 4 dk  (geçiş)  — tekrar gündüze dön
///
/// KURULUM:
/// 1. Sahneye boş GameObject ekle → "DayNightManager"
/// 2. Bu scripti ekle
/// 3. directionalLight alanına sahne güneş ışığını sürükle
/// 4. mainCamera alanına Main Camera'yı sürükle
/// 5. dayNightHUD alanına DayNightHUD scriptini sürükle (otomatik bulur)
///
/// HİÇBİR MEVCUT SCRIPTE DOKUNMAZ.
/// </summary>
public class DayNightCycle : MonoBehaviour
{
    // ────────────────────────────────────────────
    // INSPECTOR
    // ────────────────────────────────────────────

    [Header("Referanslar")]
    public Light directionalLight;
    public Camera mainCamera;

    [Header("Zaman Ayarları")]
    [Tooltip("Toplam döngü süresi (saniye). Varsayılan: 240 = 4 dakika")]
    public float cycleDurationSeconds = 240f;

    [Tooltip("Başlangıç zamanı (0-1 arası, 0=gece yarısı, 0.25=sabah, 0.5=öğle, 0.75=akşam)")]
    [Range(0f, 1f)]
    public float startTime = 0.25f; // Sabah başlar

    // ────────────────────────────────────────────
    // DURUM
    // ────────────────────────────────────────────

    [HideInInspector] public float normalizedTime; // 0-1
    [HideInInspector] public TimeOfDay currentPhase;

    public enum TimeOfDay { Dawn, Day, Dusk, Night }

    // ────────────────────────────────────────────
    // RENK PALETLERİ
    // ────────────────────────────────────────────

    // Güneş ışığı renkleri (zaman→renk)
    static readonly Color colNight = new Color(0.04f, 0.06f, 0.18f, 1f); // koyu gece mavisi
    static readonly Color colDawn = new Color(0.98f, 0.55f, 0.25f, 1f); // şafak turuncu
    static readonly Color colDay = new Color(1.00f, 0.97f, 0.88f, 1f); // gün beyazı
    static readonly Color colDusk = new Color(0.95f, 0.35f, 0.12f, 1f); // akşam kırmızısı
    static readonly Color colDeepNight = new Color(0.03f, 0.04f, 0.14f, 1f);

    // Ortam ışığı renkleri
    static readonly Color ambNight = new Color(0.02f, 0.03f, 0.10f, 1f);
    static readonly Color ambDawn = new Color(0.30f, 0.18f, 0.12f, 1f);
    static readonly Color ambDay = new Color(0.22f, 0.26f, 0.32f, 1f);
    static readonly Color ambDusk = new Color(0.28f, 0.12f, 0.08f, 1f);

    // Sis renkleri
    static readonly Color fogNight = new Color(0.01f, 0.02f, 0.08f, 1f);
    static readonly Color fogDawn = new Color(0.55f, 0.35f, 0.28f, 1f);
    static readonly Color fogDay = new Color(0.62f, 0.68f, 0.75f, 1f);
    static readonly Color fogDusk = new Color(0.50f, 0.22f, 0.10f, 1f);

    // ────────────────────────────────────────────
    // PRIVATE
    // ────────────────────────────────────────────

    private DayNightHUD hud;
    private float bgOriginalNearClip;

    // Kamera arka plan rengi (gece koyu mavi)
    private Color camBgDay = new Color(0.45f, 0.60f, 0.85f, 1f);
    private Color camBgDusk = new Color(0.20f, 0.10f, 0.18f, 1f);
    private Color camBgNight = new Color(0.01f, 0.01f, 0.05f, 1f);
    private Color camBgDawn = new Color(0.25f, 0.15f, 0.10f, 1f);

    // ────────────────────────────────────────────
    // GÜN ZAMANI EŞLEŞTİRMESİ
    // t=0   → gece yarısı
    // t=0.1 → şafak başlangıcı
    // t=0.2 → tam gündüz
    // t=0.7 → akşam başlangıcı
    // t=0.8 → tam gece
    // ────────────────────────────────────────────

    // Akşam: t=0.75-0.875 (normalizedTime içinde ≈ 60sn / 240sn = 0.25 oran olsun diye)
    // Gece : t=0.875-1.0 + 0.0-0.125 (2 dakika)
    // Gündüz: t=0.125-0.75 (2 dakika)
    // Bu şekilde: Gündüz=120sn, Akşam=60sn, Gece=60sn toplamı 240

    const float tDayStart = 0.125f; // 30sn
    const float tDuskStart = 0.625f; // 150sn — gündüz 120sn
    const float tNightStart = 0.875f; // 210sn — akşam 60sn
    // Gece: 210sn → 270sn (= 30sn fazlası bir sonraki döngüde), ama 240sn döngü olduğu için:
    // Gece: 0.875 → 1.0 (30sn) + 0.0 → 0.125 (30sn) = 60sn ✓
    // Gündüz: 0.125 → 0.625 = 120sn ✓
    // Akşam: 0.625 → 0.875 = 60sn ✓

    // ────────────────────────────────────────────

    void Start()
    {
        normalizedTime = startTime;
        hud = FindObjectOfType<DayNightHUD>();

        if (mainCamera == null)
            mainCamera = Camera.main;

        // Sis aktif et
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogDensity = 0.008f;

        if (directionalLight == null)
        {
            Light[] lights = FindObjectsOfType<Light>();
            foreach (var l in lights)
                if (l.type == LightType.Directional) { directionalLight = l; break; }

            if (directionalLight == null)
                Debug.LogError("❌ DayNightCycle: Sahneye Directional Light ekle ve atama yap!");
        }

        Apply(normalizedTime);
    }

    void Update()
    {
        normalizedTime += Time.deltaTime / cycleDurationSeconds;
        if (normalizedTime >= 1f) normalizedTime -= 1f;

        Apply(normalizedTime);

        // Faz güncelle
        if (normalizedTime >= tNightStart || normalizedTime < tDayStart)
            currentPhase = TimeOfDay.Night;
        else if (normalizedTime >= tDuskStart)
            currentPhase = TimeOfDay.Dusk;
        else if (normalizedTime >= tDayStart)
            currentPhase = TimeOfDay.Day;
        else
            currentPhase = TimeOfDay.Dawn;
    }

    void Apply(float t)
    {
        if (directionalLight == null) return;

        // Işık açısı: gündüz yukarıda, gece aşağıda
        float sunAngle = t * 360f - 90f;
        directionalLight.transform.rotation = Quaternion.Euler(sunAngle, -35f, 0f);

        // Işık şiddeti: gece sıfıra yakın
        float intensity = Mathf.Clamp01(Mathf.Sin(t * Mathf.PI * 2f) * 1.3f + 0.1f);
        directionalLight.intensity = intensity;

        // Renk & ambient interpolasyonu
        Color sunCol, ambCol, fogCol, camBg;
        GetColors(t, out sunCol, out ambCol, out fogCol, out camBg);

        directionalLight.color = sunCol;
        RenderSettings.ambientLight = ambCol;
        RenderSettings.fogColor = fogCol;

        // Kamera arka plan rengi (skybox yoksa solid color)
        if (mainCamera != null && mainCamera.clearFlags == CameraClearFlags.SolidColor)
            mainCamera.backgroundColor = camBg;

        // Gece gözü efekti: kamera tint (post-process olmadan basit yöntem)
        ApplyCameraEffect(t);
    }

    void GetColors(float t, out Color sun, out Color amb, out Color fog, out Color camBg)
    {
        // Gün evrelerine göre 4 bölge arası lerp
        if (t < tDayStart) // Şafak
        {
            float f = t / tDayStart;
            sun = Color.Lerp(colDeepNight, colDawn, f);
            amb = Color.Lerp(ambNight, ambDawn, f);
            fog = Color.Lerp(fogNight, fogDawn, f);
            camBg = Color.Lerp(camBgNight, camBgDawn, f);
        }
        else if (t < tDuskStart) // Gündüz
        {
            float f = (t - tDayStart) / (tDuskStart - tDayStart);
            float mid = 0.15f;
            if (f < mid)
            {
                float ff = f / mid;
                sun = Color.Lerp(colDawn, colDay, ff);
                amb = Color.Lerp(ambDawn, ambDay, ff);
                fog = Color.Lerp(fogDawn, fogDay, ff);
                camBg = Color.Lerp(camBgDawn, camBgDay, ff);
            }
            else
            {
                sun = colDay; amb = ambDay; fog = fogDay; camBg = camBgDay;
            }
        }
        else if (t < tNightStart) // Akşam
        {
            float f = (t - tDuskStart) / (tNightStart - tDuskStart);
            sun = Color.Lerp(colDay, colDusk, f);
            amb = Color.Lerp(ambDay, ambDusk, f);
            fog = Color.Lerp(fogDay, fogDusk, f);
            camBg = Color.Lerp(camBgDay, camBgDusk, f);
        }
        else // Gece
        {
            float f = (t - tNightStart) / (1f - tNightStart);
            sun = Color.Lerp(colDusk, colDeepNight, f);
            amb = Color.Lerp(ambDusk, ambNight, f);
            fog = Color.Lerp(fogDusk, fogNight, f);
            camBg = Color.Lerp(camBgDusk, camBgNight, f);
        }
    }

    void ApplyCameraEffect(float t)
    {
        // Post-process yokken: kameranın render texture'una dokunmadan
        // ekranı karartan hafif bir global sis yoğunluğu kullan
        bool isNight = currentPhase == TimeOfDay.Night;
        bool isDusk = currentPhase == TimeOfDay.Dusk;

        float targetFog = 0.008f;
        if (isNight) targetFog = 0.025f;
        else if (isDusk) targetFog = 0.014f;

        RenderSettings.fogDensity = Mathf.Lerp(RenderSettings.fogDensity, targetFog, Time.deltaTime * 0.5f);
    }

    // ────────────────────────────────────────────
    // PUBLIC YARDIMCILAR
    // ────────────────────────────────────────────

    /// Güncel saati 0-24 formatında döndürür
    public float GetHour()
    {
        // Gündüz: 06:00-18:00 / Akşam: 18:00-21:00 / Gece: 21:00-06:00
        if (normalizedTime < tDayStart)         // Şafak: 0.0 → 0.125
        {
            float f = normalizedTime / tDayStart;
            return Mathf.Lerp(4f, 6f, f);       // 04:00 → 06:00
        }
        else if (normalizedTime < tDuskStart)   // Gündüz: 0.125 → 0.625
        {
            float f = (normalizedTime - tDayStart) / (tDuskStart - tDayStart);
            return Mathf.Lerp(6f, 18f, f);      // 06:00 → 18:00
        }
        else if (normalizedTime < tNightStart)  // Akşam: 0.625 → 0.875
        {
            float f = (normalizedTime - tDuskStart) / (tNightStart - tDuskStart);
            return Mathf.Lerp(18f, 21f, f);     // 18:00 → 21:00
        }
        else                                    // Gece: 0.875 → 1.0
        {
            float f = (normalizedTime - tNightStart) / (1f - tNightStart);
            return Mathf.Lerp(21f, 28f, f);     // 21:00 → 04:00 (28=24+4)
        }
    }

    /// Gece mi?
    public bool IsNight()
    {
        return currentPhase == TimeOfDay.Night;
    }

    // Gizmo — Inspector'da zaman göster
    void OnDrawGizmos()
    {
#if UNITY_EDITOR
        UnityEditor.Handles.Label(transform.position,
            $"🕐 {GetHour():F1}h | {currentPhase}");
#endif
    }
}