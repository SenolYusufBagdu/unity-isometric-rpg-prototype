using Unity.VisualScripting;
using UnityEngine;

/// <summary>
/// AtmosphereController.cs
/// Oyuncuyu takip eden "Umut Işığı" (God Ray) efekti.
/// </summary>
public class AtmosphereController : MonoBehaviour
{
    [Header("God Ray — Umut Işığı")]
    public Transform player;

    [Tooltip("Oyuncunun arkasından vuracak ışık rengi")]
    public Color lightColor = new Color(1f, 0.92f, 0.72f, 1f);

    [Tooltip("Işığın gücü/parlaklığı")]
    public float lightIntensity = 1.4f;

    [Header("Işık Çapı ve Menzili")]
    [Tooltip("Işığın yerdeki çapı (Spot Açısı). Değer büyüdükçe aydınlanan alan genişler.")]
    [Range(10f, 170f)]
    public float lightAngle = 45f;

    [Tooltip("Işığın ne kadar uzağa (aşağıya) ulaşabileceği.")]
    public float lightRange = 20f;

    [Header("Konumlandırma")]
    [Tooltip("Oyuncunun ne kadar üstünde konumlanır")]
    public float lightHeight = 4.5f;
    [Tooltip("Oyuncunun ne kadar arkasında konumlanır")]
    public float lightBehind = 2f;

    [Header("Nefes Alma (Titreşim) Efekti")]
    [Tooltip("Işık yumuşak salınım hızı")]
    public float breatheSpeed = 0.6f;
    [Tooltip("Işığın ne kadar parlayıp söneceği")]
    public float breatheAmount = 0.12f;

    // ─────────────────────────────────────────────────────
    private Light godRayLight;
    private float breatheTime;

    // ─────────────────────────────────────────────────────

    void Start()
    {
        // Player atanmamışsa Tag ile bul
        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null)
                player = p.transform;
            else
                Debug.LogError("[AtmosphereController] Player bulunamadı! 'Player' tag'ini kontrol et.");
        }

        SetupGodRay();
    }

    void LateUpdate()
    {
        if (player == null || godRayLight == null) return;

        // ── Işığın pozisyonunu güncelle ──
        // player.forward yerine sabit dünya ekseni kullanıyoruz:
        // İzometrik oyunlarda player.forward kamera yönüne göre değişebilir,
        // bu yüzden Vector3.back (dünya -Z) baz alınır. İstersen player.forward'a döndürebilirsin.
        Vector3 backDir = -Vector3.forward; // Dünya ekseninde "geri" yön
        Vector3 lightPos = player.position
                         + backDir * lightBehind
                         + Vector3.up * lightHeight;
        godRayLight.transform.position = lightPos;

        // ── Işığı oyuncuya doğru döndür ──
        Vector3 toPlayer = (player.position + Vector3.up * 1.2f) - lightPos;
        if (toPlayer.sqrMagnitude > 0.01f)
            godRayLight.transform.rotation = Quaternion.LookRotation(toPlayer.normalized);

        // ── Inspector değişikliklerini anlık uygula ──
        godRayLight.spotAngle = lightAngle;
        godRayLight.range = lightRange;
        godRayLight.color = lightColor;        // renk değişince anında yansısın

        // ── Nefes efekti ──
        breatheTime += Time.deltaTime * breatheSpeed;
        float breathe = Mathf.Sin(breatheTime) * breatheAmount;
        godRayLight.intensity = lightIntensity + breathe;  // baseIntensity yerine direkt lightIntensity
    }

    void SetupGodRay()
    {
        // Daha önce oluşturulmuş bir GodRay varsa temizle (Play-Stop-Play döngüsünde çift oluşmayı önler)
        GameObject existing = GameObject.Find("GodRay_HopeLight");
        if (existing != null)
            Destroy(existing);

        GameObject lightGO = new GameObject("GodRay_HopeLight");

        godRayLight = lightGO.AddComponent<Light>();
        godRayLight.type = LightType.Spot;
        godRayLight.color = lightColor;
        godRayLight.intensity = lightIntensity;
        godRayLight.range = lightRange;
        godRayLight.spotAngle = lightAngle;
        godRayLight.shadows = LightShadows.Soft;
        godRayLight.shadowStrength = 0.5f;

        Debug.Log("✨ Umut Işığı kuruldu.");
    }

    // Editörde ışığın konumunu gösteren Gizmo
    void OnDrawGizmos()
    {
        if (player == null) return;

        Vector3 backDir = -Vector3.forward;
        Vector3 pos = player.position + backDir * lightBehind + Vector3.up * lightHeight;

        Gizmos.color = new Color(1f, 0.92f, 0.72f, 0.5f);
        Gizmos.DrawLine(pos, player.position + Vector3.up * 1.2f);
        Gizmos.DrawWireSphere(pos, 0.3f);
    }

    // Uygulama kapanırken oluşturulan ışık nesnesini temizle
    void OnDestroy()
    {
        GameObject go = GameObject.Find("GodRay_HopeLight");
        if (go != null)
            Destroy(go);
    }
 

}