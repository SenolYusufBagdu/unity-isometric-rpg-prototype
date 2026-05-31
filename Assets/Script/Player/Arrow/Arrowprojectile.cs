using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// ArrowProjectile.cs → Ok prefabına ekle
/// </summary>
public class ArrowProjectile : MonoBehaviour
{
    [Header("Hareket")]
    public float speed = 25f;
    public float maxDistance = 40f;
    public float maxLifetime = 5f;

    [Header("Hasar")]
    public float damage = 20f;

    [Header("Knockback")]
    public float knockbackDistance = 2f;
    public float knockbackDuration = 0.15f;

    [Header("Çarpma Sonrası")]
    public float stickDuration = 1f;  // İSTEDİĞİN GİBİ TAM 1 SANİYEYE İNDİRİLDİ

    // ─────────────────────────────────────────────────────────

    private float traveledDistance = 0f;
    private float spawnTime;
    private bool hasHit = false;

    void Start()
    {
        spawnTime = Time.time;
    }

    void Update()
    {
        if (hasHit) return;

        // Lifetime kontrolü
        if (Time.time - spawnTime > maxLifetime)
        {
            Destroy(gameObject);
            return;
        }

        // İleri hareket
        float step = speed * Time.deltaTime;
        transform.Translate(Vector3.forward * step);
        traveledDistance += step;

        // Mesafe aşıldıysa yok et
        if (traveledDistance >= maxDistance)
            Destroy(gameObject);
    }

    void OnTriggerEnter(Collider other)
    {
        if (hasHit) return;

        // Player'a ve kendi tag'lerine çarpma
        if (other.CompareTag("Player") || other.CompareTag("Untagged")) return;

        // Enemy'e çarptı
        if (other.CompareTag("Enemy"))
        {
            hasHit = true;

            // Hasar ver
            EnemyHealth eh = other.GetComponent<EnemyHealth>();
            if (eh != null)
            {
                eh.TakeDamage(damage);
                Debug.Log($"🏹 OK HİT: {other.name} → {damage} hasar!");
            }

            // Knockback — NavMeshAgent varsa
            NavMeshAgent agent = other.GetComponent<NavMeshAgent>();
            if (agent != null)
            {
                Vector3 dir = (other.transform.position - transform.position).normalized;
                dir.y = 0f;
                StartCoroutine(KnockbackAgent(agent, dir, eh));
            }

            // Oka saplan — oku düşmanın child'ı yap, birlikte hareket etsin
            transform.SetParent(other.transform);

            // GÜNCELLEME: Çarpışmayı ve Fiziği kapat ki hareket ederken sapıtmasın
            Collider col = GetComponent<Collider>();
            if (col != null) col.enabled = false;

            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = true;
                rb.linearVelocity = Vector3.zero;
            }

            // Tam 1 saniye sonra yok ol
            Destroy(gameObject, stickDuration);
            return;
        }

        // Zemin veya başka bir şeye çarptı — orada kal
        hasHit = true;
        Collider myCol = GetComponent<Collider>();
        if (myCol != null) myCol.enabled = false;

        Destroy(gameObject, stickDuration);
    }

    System.Collections.IEnumerator KnockbackAgent(NavMeshAgent agent, Vector3 direction, EnemyHealth eh)
    {
        if (agent == null) yield break;

        agent.isStopped = true;
        agent.velocity = Vector3.zero;

        float elapsed = 0f;
        float spd = knockbackDistance / knockbackDuration;

        while (elapsed < knockbackDuration)
        {
            if (agent == null) yield break;
            float t = 1f - (elapsed / knockbackDuration); // yavaşlayan knockback
            agent.Move(direction * spd * t * Time.deltaTime);
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (agent != null && eh != null && !eh.isDead)
            agent.isStopped = false;
    }
}