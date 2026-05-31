using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class SwordDamage : MonoBehaviour
{
    [Header("Hasar Ayarları")]
    public float damage = 50f;
    public float hitCooldown = 0.3f;

    [Header("Knockback Ayarları")]
    public float knockbackDistance = 2.5f; // Kaç birim geri gitsin (2-3 arası idealdir)
    public float knockbackDuration = 0.2f; // Kaç saniyede gitsin

    private Collider swordCollider;
    private float lastHitTime;

    void Awake()
    {
        swordCollider = GetComponent<Collider>();

        if (swordCollider == null)
        {
            Debug.LogError("❌ KILIÇ: Box Collider yok!");
            return;
        }

        swordCollider.enabled = false;
    }

    public void EnableDamage()
    {
        if (swordCollider == null) return;
        swordCollider.enabled = true;
        lastHitTime = 0f;
        Debug.Log("⚔️ KILIÇ: Hasar aktif!");
    }

    public void DisableDamage()
    {
        if (swordCollider == null) return;
        swordCollider.enabled = false;
        Debug.Log("🛡️ KILIÇ: Hasar pasif.");
    }

    void OnTriggerStay(Collider other)
    {
        if (!other.CompareTag("Enemy")) return;
        if (Time.time < lastHitTime + hitCooldown) return;

        lastHitTime = Time.time;

        // Hasar ver
        EnemyHealth eh = other.GetComponent<EnemyHealth>();
        if (eh != null)
        {
            eh.TakeDamage(damage);
            Debug.Log($"💥 KILIÇ: {other.name} → {damage} hasar!");
        }

        // Knockback — NavMeshAgent varsa coroutine ile geri it
        NavMeshAgent agent = other.GetComponent<NavMeshAgent>();
        if (agent != null)
        {
            Vector3 knockDir = (other.transform.position - transform.position).normalized;
            knockDir.y = 0f;
            MonoBehaviour enemy = other.GetComponent<MonoBehaviour>();
            if (enemy != null)
                enemy.StartCoroutine(KnockbackAgent(agent, knockDir));
        }
    }

    IEnumerator KnockbackAgent(NavMeshAgent agent, Vector3 direction)
    {
        // Agent'ı durdur, kısa süre manuel taşı
        agent.isStopped = true;
        agent.velocity = Vector3.zero;

        float elapsed = 0f;
        while (elapsed < knockbackDuration)
        {
            float step = (knockbackDistance / knockbackDuration) * Time.deltaTime;
            agent.Move(direction * step);
            elapsed += Time.deltaTime;
            yield return null;
        }

        // Düşman ölmediyse agent'ı tekrar aç
        EnemyHealth eh = agent.GetComponent<EnemyHealth>();
        if (eh != null && !eh.isDead)
            agent.isStopped = false;
    }
}