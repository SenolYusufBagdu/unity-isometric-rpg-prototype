using UnityEngine;

/// <summary>
/// PlayerMouseDirection.cs → Player GameObject'ine ekle
/// 
/// INSPECTOR AYARLARI:
/// - Ground Layer: Ground layer'ı seç
/// - Rotation Speed: 15 (yumuşak dönüş hızı)
/// </summary>
public class PlayerMouseDirection : MonoBehaviour
{
    [Header("Ayarlar")]
    public LayerMask groundLayer;
    public float rotationSpeed = 15f;

    private Camera mainCamera;
    private Vector3 lookDirection;

    // Diğer scriptler bu yönü okuyabilir
    public Vector3 LookDirection => lookDirection;

    void Start()
    {
        mainCamera = Camera.main;
    }

    void Update()
    {
        CalculateMouseDirection();
        RotateCharacter();
    }

    void CalculateMouseDirection()
    {
        // Mouse pozisyonundan izometrik ground'a raycast at
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit, 100f, groundLayer))
        {
            Vector3 targetPoint = hit.point;
            targetPoint.y = transform.position.y; // Y eksenini kilitle

            lookDirection = (targetPoint - transform.position).normalized;
        }
    }

    void RotateCharacter()
    {
        if (lookDirection == Vector3.zero) return;

        Quaternion targetRotation = Quaternion.LookRotation(lookDirection);
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            rotationSpeed * Time.deltaTime
        );
    }
}