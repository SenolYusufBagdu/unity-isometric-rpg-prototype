using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// DashHitbox.cs → Player'ın önünde ayrı bir child GameObject'e ekle
/// Adı: DashHitbox
/// 
/// INSPECTOR / COLLIDER AYARLARI:
/// - Box Collider veya Sphere Collider ekle
/// - Is Trigger: AÇIK
/// - Başlangıçta bu objeyi devre dışı bırak (SetActive false)
/// 
/// KURULUM:
/// 1. Player altında boş GameObject oluştur → "DashHitbox" adı ver
/// 2. Position: X:0, Y:0.5, Z:0.8 (karakterin önünde)
/// 3. Box Collider ekle: Size X:1, Y:1, Z:1.5 → Is Trigger: AÇIK
/// 4. Bu scripti ekle
/// 5. PlayerDash → Dash Hitbox alanına sürükle
/// </summary>
public class DashHitbox : MonoBehaviour
{
    private float damage;
    private HashSet<GameObject> hitEnemies = new HashSet<GameObject>(); // Aynı dash'te tekrar hasar önle

    // PlayerDash tarafından çağrılır
    public void SetDamage(float dmg)
    {
        damage = dmg;
    }

    public void SetActive(bool active)
    {
        if (active)
        {
            hitEnemies.Clear(); // Yeni dash başladı, listeyi temizle
        }
        gameObject.SetActive(active);
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Enemy")) return;

        // Bu dash'te zaten vuruldu mu?
        if (hitEnemies.Contains(other.gameObject)) return;
        hitEnemies.Add(other.gameObject);

        EnemyHealth eh = other.GetComponent<EnemyHealth>();
        if (eh != null)
        {
            eh.TakeDamage(damage);
            Debug.Log($"💥 DASH HIT: {other.name} → {damage} hasar!");
        }
    }
}