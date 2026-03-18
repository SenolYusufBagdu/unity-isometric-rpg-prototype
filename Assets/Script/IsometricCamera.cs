using UnityEngine;

/// <summary>
/// VRisingCamera.cs → Main Camera'ya ekle
/// 
/// INSPECTOR AYARLARI:
/// - Target: carackte rv12 sürükle
/// - Distance: 15 (Kameranın karaktere uzaklığı)
/// - Min/Max Distance: Scroll ile ne kadar yaklaşıp uzaklaşabileceği
/// - Pitch: 45 (Yukarıdan bakış açısı - V Rising için 45-55 arası idealdir)
/// </summary>
public class VRisingCamera : MonoBehaviour
{
    [Header("Hedef")]
    public Transform target;

    [Header("Mesafe ve Zoom (Mouse Scroll)")]
    public float distance = 15f;
    public float minDistance = 5f;
    public float maxDistance = 25f;
    public float zoomSpeed = 10f;

    [Header("Kamera Dönüşü (Sağ Tık)")]
    public float rotationSpeed = 5f;
    public float pitch = 50f; // Yukarıdan bakış açısı (Sabit)
    private float currentYaw = 45f; // Sağa sola dönüş açısı

    [Header("Takip Hızı")]
    public float smoothSpeed = 10f;

    void LateUpdate()
    {
        if (target == null) return;

        // 1. Zoom Kontrolü (Fare Tekerleği)
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        distance -= scroll * zoomSpeed;
        distance = Mathf.Clamp(distance, minDistance, maxDistance);

        // 2. Kamerayı Döndürme (Sağ tık basılıyken fareyi sağa sola kaydırma)
        // Eğer orta tuş ile dönmesini istersen GetMouseButton(2) yapabilirsin.
        if (Input.GetMouseButton(1))
        {
            currentYaw += Input.GetAxis("Mouse X") * rotationSpeed;
        }

        // 3. Pozisyon ve Rotasyon Hesaplama
        // Kameranın karakterin etrafındaki dönüş açısını belirliyoruz
        Quaternion rotation = Quaternion.Euler(pitch, currentYaw, 0f);

        // Kamerayı karakterin o anki açısından geriye (distance kadar) çekiyoruz
        Vector3 offset = rotation * new Vector3(0, 0, -distance);
        Vector3 desiredPos = target.position + offset;

        // 4. Uygulama (Yumuşak Takip)
        transform.position = Vector3.Lerp(transform.position, desiredPos, smoothSpeed * Time.deltaTime);
        transform.rotation = rotation;
    }
}