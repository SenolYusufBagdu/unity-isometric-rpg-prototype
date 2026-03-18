using UnityEngine;

/// <summary>
/// CameraFollow.cs → Main Camera'ya ekle
/// Target: carackte rv12 (karakterin kendisi)
/// Kamera Player'ın child'ı OLMAMALI — sahnede bağımsız olmalı
/// </summary>
public class CameraFollow : MonoBehaviour
{
    [Header("Hedef")]
    public Transform target;                            // Karakteri sürükle

    [Header("Pozisyon Ayarları")]
    public Vector3 offset = new Vector3(0f, 3f, -6f);  // Kameradan mesafe
    public float smoothSpeed = 8f;                      // Takip yumuşaklığı

    [Header("Rotasyon Ayarları")]
    public float lookAtHeightOffset = 1.5f;            // Karakterin hangi yüksekliğine baksın

    void LateUpdate()
    {
        if (target == null) return;

        // Hedef pozisyona göre kamera pozisyonunu hesapla
        Vector3 desiredPosition = target.position + offset;

        // Yumuşak takip
        transform.position = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);

        // Karaktere bak
        transform.LookAt(target.position + Vector3.up * lookAtHeightOffset);
    }
}