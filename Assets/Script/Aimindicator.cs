using UnityEngine;

/// <summary>
/// AimIndicator.cs → Player'a ekle
/// 
/// KURULUM:
/// 1. Player'a bu scripti ekle
/// 2. Inspector'da Player Transform alanına Player'ı sürükle
/// 3. Renk/boyut ayarlarını istediğin gibi değiştir
/// 
/// GÖRÜNÜM:
/// - Karakterden nişan noktasına uzanan ışın çizgisi (dashli)
/// - Nişan noktasında iç + dış çember
/// - 4 yön çizgisi (crosshair)
/// - Hepsi pulse animasyonuyla titreşir
/// </summary>
// Git e-posta ayarı test edildi.
public class AimIndicator : MonoBehaviour
{
    [Header("Referans")]
    public Transform playerTransform;   // Player'ı sürükle — ışın buradan başlar

    [Header("Renk Ayarları")]
    public Color beamColor = new Color(0.2f, 0.85f, 1f, 0.6f);   // Işın rengi
    public Color innerColor = new Color(0.2f, 0.85f, 1f, 0.95f);  // İç çember
    public Color outerColor = new Color(0.2f, 0.85f, 1f, 0.35f);  // Dış çember
    public Color crossColor = new Color(1f, 1f, 1f, 0.85f);   // Crosshair çizgileri

    [Header("Çember Boyutu")]
    public float innerRadius = 0.35f;
    public float outerRadius = 0.75f;
    public float crossLength = 0.4f;
    public float crossGap = 0.15f;

    [Header("Işın Ayarları")]
    public float beamStartWidth = 0.06f;   // Karaktere yakın taraf kalınlığı
    public float beamEndWidth = 0.02f;   // Hedefe yakın taraf (incelen)
    public float beamHeightOffset = 0.08f; // Yerden kaç birim yukarıda uçsun

    [Header("Animasyon")]
    public float pulseSpeed = 3f;
    public float pulseAmount = 0.08f;
    public float rotationSpeed = 45f;

    [Header("Çizgi Kalınlığı")]
    public float innerLineWidth = 0.045f;
    public float outerLineWidth = 0.022f;
    public float crossLineWidth = 0.030f;

    // LineRenderer'lar
    private LineRenderer lrBeam;
    private LineRenderer lrInner;
    private LineRenderer lrOuter;
    private LineRenderer[] lrCross = new LineRenderer[4];

    private float currentRotation = 0f;
    private bool isVisible = false;
    private Vector3 worldTarget;

    private const int CircleSegments = 64;
    private const int BeamSegments = 24; // Dashli görünüm için segment sayısı

    void Start()
    {
        if (playerTransform == null)
            playerTransform = transform;

        CreateRenderers();
        SetVisible(false);
    }

    void CreateRenderers()
    {
        // Işın — karakterden hedefe
        lrBeam = CreateLineRenderer("AimBeam", beamColor, beamStartWidth, BeamSegments, true);

        // Çemberler
        lrInner = CreateLineRenderer("AimInner", innerColor, innerLineWidth, CircleSegments + 1, false);
        lrOuter = CreateLineRenderer("AimOuter", outerColor, outerLineWidth, CircleSegments + 1, false);

        // Crosshair
        string[] names = { "AimCross_N", "AimCross_S", "AimCross_E", "AimCross_W" };
        for (int i = 0; i < 4; i++)
            lrCross[i] = CreateLineRenderer(names[i], crossColor, crossLineWidth, 2, false);

        // Işın için konik görünüm — başı kalın, ucu ince
        lrBeam.startWidth = beamStartWidth;
        lrBeam.endWidth = beamEndWidth;
    }

    LineRenderer CreateLineRenderer(string objName, Color color, float width, int posCount, bool useTextureMode)
    {
        GameObject go = new GameObject(objName);
        go.transform.SetParent(transform);

        LineRenderer lr = go.AddComponent<LineRenderer>();
        lr.useWorldSpace = true;
        lr.loop = false;
        lr.positionCount = posCount;
        lr.startWidth = width;
        lr.endWidth = width;
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.startColor = color;
        lr.endColor = color;
        lr.sortingOrder = 10;

        if (useTextureMode)
        {
            // Dashli/noktalı görünüm için texture scale
            lr.material.mainTextureScale = new Vector2(8f, 1f);
        }

        return lr;
    }

    public void Show(Vector3 position)
    {
        worldTarget = position;
        worldTarget.y += 0.05f;
        if (!isVisible) SetVisible(true);
    }

    public void Hide()
    {
        SetVisible(false);
    }

    void SetVisible(bool value)
    {
        isVisible = value;
        lrBeam.enabled = value;
        lrInner.enabled = value;
        lrOuter.enabled = value;
        foreach (var lr in lrCross) lr.enabled = value;
    }

    void Update()
    {
        if (!isVisible) return;

        float pulse = 1f + Mathf.Sin(Time.time * pulseSpeed) * pulseAmount;
        float innerR = innerRadius * pulse;
        float outerR = outerRadius * pulse;

        currentRotation += rotationSpeed * Time.deltaTime;

        DrawBeam();
        DrawCircle(lrInner, worldTarget, innerR, 0f);
        DrawCircle(lrOuter, worldTarget, outerR, currentRotation);
        DrawCross(worldTarget);
    }

    void DrawBeam()
    {
        // Başlangıç noktası: karakterin pozisyonu + hafif yukarı
        Vector3 start = playerTransform.position;
        start.y = worldTarget.y + beamHeightOffset;

        Vector3 end = worldTarget;
        end.y = worldTarget.y + beamHeightOffset;

        // Segmentler boyunca düzgün dağıt — hafif dalgalı görünüm için
        for (int i = 0; i < BeamSegments; i++)
        {
            float t = (float)i / (BeamSegments - 1);

            // Pulse ile hafif dikey oynama (orta kısım hafif yükselir)
            float wave = Mathf.Sin(t * Mathf.PI) * 0.04f * Mathf.Sin(Time.time * 4f);
            Vector3 pos = Vector3.Lerp(start, end, t);
            pos.y += wave;

            lrBeam.SetPosition(i, pos);
        }

        // Baştan sona incelen çizgi
        float pulseFactor = 1f + Mathf.Sin(Time.time * pulseSpeed) * 0.15f;
        lrBeam.startWidth = beamStartWidth * pulseFactor;
        lrBeam.endWidth = beamEndWidth;

        // Başlangıç şeffaf, bitiş daha opak — gradient efekti
        Color startCol = beamColor;
        startCol.a = 0.15f;
        Color endCol = beamColor;
        endCol.a = 0.75f;
        lrBeam.startColor = startCol;
        lrBeam.endColor = endCol;
    }

    void DrawCircle(LineRenderer lr, Vector3 center, float radius, float angleOffset)
    {
        float angleStep = 360f / CircleSegments;
        for (int i = 0; i <= CircleSegments; i++)
        {
            float angle = Mathf.Deg2Rad * (i * angleStep + angleOffset);
            Vector3 point = center + new Vector3(
                Mathf.Cos(angle) * radius,
                0f,
                Mathf.Sin(angle) * radius
            );
            lr.SetPosition(i, point);
        }
    }

    void DrawCross(Vector3 center)
    {
        Vector3[] dirs = {
             Vector3.forward,
            -Vector3.forward,
             Vector3.right,
            -Vector3.right
        };

        for (int i = 0; i < 4; i++)
        {
            Vector3 start = center + dirs[i] * crossGap;
            Vector3 end = center + dirs[i] * (crossGap + crossLength);
            lrCross[i].SetPosition(0, start);
            lrCross[i].SetPosition(1, end);
        }
    }
}