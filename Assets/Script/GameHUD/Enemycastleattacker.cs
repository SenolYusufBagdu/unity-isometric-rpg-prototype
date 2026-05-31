using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// EnemyCastleAttacker.cs → WaveManager tarafından otomatik eklenir.
/// 
/// Enemy'nin hem player'ı hem de kaleyi hedef almasını sağlar.
/// Kale, player'dan daha yakındaysa kaleye saldırır.
/// Player'ı görmüyorsa direkt kaleye yürür.
///
/// ÇALIŞMA MANTIĞI:
/// - Player yakındaysa (playerPriority mesafesi) → Player'a saldır
/// - Player uzaktaysa → Kale'ye yürü ve saldır
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class EnemyCastleAttacker : MonoBehaviour
{
    [Header("Referanslar — WaveManager tarafından doldurulur")]
    public CastleHealth castle;

    [Header("Ayarlar")]
    public float castleAttackRange = 3f;
    public float castleAttackDamage = 20f;
    public float castleAttackCooldown = 2f;
    public float playerPriorityDistance = 8f;  // Player bu kadar yakınsa player'a odaklan

    private NavMeshAgent agent;
    private EnemyHealth enemyHealth;
    private EnemyAI enemyAI;
    private Transform player;

    private float lastCastleAttackTime;
    private bool isAttackingCastle;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        enemyHealth = GetComponent<EnemyHealth>();
        enemyAI = GetComponent<EnemyAI>();

        // Player bul
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) player = p.transform;

        // Kale referansını bul
        if (castle == null)
            castle = CastleHealth.Instance;
    }

    void Update()
    {
        if (enemyHealth != null && enemyHealth.isDead) return;
        if (castle == null || castle.isDestroyed) return;

        // Player yakınsa EnemyAI halleder — biz müdahale etme
        if (player != null)
        {
            float distToPlayer = Vector3.Distance(transform.position, player.position);
            if (distToPlayer <= playerPriorityDistance) return;
        }

        // Player uzak → Kaleye odaklan
        float distToCastle = Vector3.Distance(transform.position, castle.transform.position);

        if (distToCastle <= castleAttackRange)
        {
            // EnemyAI'yı durdur
            if (enemyAI != null) enabled = true; // Bu component aktif kalsın

            AttackCastle();
        }
        else
        {
            // Kaleye yürü
            MoveToCastle();
        }
    }

    void MoveToCastle()
    {
        if (!agent.isOnNavMesh) return;

        // EnemyAI'nın destination'ını kaleye yönlendir
        agent.isStopped = false;
        agent.SetDestination(castle.transform.position);
    }

    void AttackCastle()
    {
        if (agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
        }

        // Kaleye bak
        Vector3 dir = (castle.transform.position - transform.position).normalized;
        dir.y = 0f;
        if (dir != Vector3.zero)
            transform.rotation = Quaternion.LookRotation(dir);

        // Saldırı cooldown
        if (Time.time >= lastCastleAttackTime + castleAttackCooldown)
        {
            lastCastleAttackTime = Time.time;
            castle.TakeDamage(castleAttackDamage);
            Debug.Log($"🏰 {gameObject.name} kaleye saldırdı: -{castleAttackDamage}");
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, castleAttackRange);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, playerPriorityDistance);
    }
}