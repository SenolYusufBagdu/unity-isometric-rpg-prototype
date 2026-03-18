using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// EnemyAI.cs (NavMesh versiyonu) → Enemy GameObject'ine ekle
/// Mevcut mantık korundu, sadece hareket NavMeshAgent ile yapılıyor
/// 
/// GEREKSİNİMLER:
/// - NavMeshAgent component (Rigidbody'yi SİL)
/// - EnemyHealth.cs
/// - Sahne NavMesh'i bake edilmiş olmalı
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class EnemyAI : MonoBehaviour
{
    [Header("Algılama")]
    public float detectionRange = 10f;
    public float attackRange = 2f;

    [Header("Saldırı")]
    public float attackCooldown = 2f;
    public float attackDamage = 15f;
    public float attackLungeForce = 3f;

    [Header("NavMesh Ayarları")]
    public float moveSpeed = 3f;          // NavMeshAgent speed
    public float acceleration = 8f;       // Hızlanma
    public float angularSpeed = 300f;     // Dönüş hızı
    public float stoppingDistance = 1.5f; // Hedefe ne kadar yaklaşınca dursun

    // Referanslar
    public Transform player;
    private NavMeshAgent agent;
    private Animator animator;
    private EnemyHealth enemyHealth;

    // State
    private float lastAttackTime;
    private bool isAttacking;

    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int AttackHash = Animator.StringToHash("Attack");
    private static readonly int DeathHash = Animator.StringToHash("Death");

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();

        // ÇOK ÖNEMLİ DEĞİŞİKLİK: Sadece ana objeye değil, karakterin asıl kemiklerinin
        // (Avatarının) olduğu iç (Child) objelere de bakıp Animator'ı oradan alır!
        animator = GetComponentInChildren<Animator>();

        // Eğer Animator hala bulunamadıysa uyarı ver
        if (animator == null)
        {
            Debug.LogError("❌ ENEMY: Karakterin içinde hiçbir Animator bulunamadı! Boss modelini kontrol et.");
        }

        enemyHealth = GetComponent<EnemyHealth>();

        // NavMeshAgent ayarları
        agent.speed = moveSpeed;
        agent.acceleration = acceleration;
        agent.angularSpeed = angularSpeed;
        agent.stoppingDistance = stoppingDistance;
        agent.updateRotation = true;  // Agent otomatik dönsün

        // Player tag ile bul
        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
            else Debug.LogError("❌ ENEMY: Player tag'li obje bulunamadı!");
        }
    }

    void Update()
    {
        if (enemyHealth != null && enemyHealth.isDead) return;
        if (player == null) return;

        // Eğer animator bulunamadıysa kodu durdur ki hata spamı (NullReference) yapmasın
        if (animator == null) return;

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        // Güvenlik sigortası
        if (isAttacking && Time.time >= lastAttackTime + attackCooldown)
            isAttacking = false;

        if (isAttacking) return;

        // State machine — mantık aynı
        if (distanceToPlayer <= attackRange)
            StopAndAttack();
        else if (distanceToPlayer <= detectionRange)
            ChasePlayer();
        else
            Idle();

        UpdateAnimator();
    }

    void ChasePlayer()
    {
        agent.isStopped = false;
        agent.SetDestination(player.position);
    }

    void StopAndAttack()
    {
        // Dur
        agent.isStopped = true;
        agent.velocity = Vector3.zero;

        // Oyuncuya dön
        Vector3 dir = (player.position - transform.position).normalized;
        dir.y = 0f;
        if (dir != Vector3.zero)
            transform.rotation = Quaternion.LookRotation(dir);

        if (Time.time >= lastAttackTime + attackCooldown)
        {
            lastAttackTime = Time.time;
            isAttacking = true;
            animator.SetTrigger(AttackHash);

            // Lunge: NavMesh ile uyumlu küçük ileri atılma
            Vector3 lungeDir = dir;
            StartCoroutine(LungeCoroutine(lungeDir));

            Debug.Log("⚔️ ENEMY: Saldırı!");
        }
    }

    System.Collections.IEnumerator LungeCoroutine(Vector3 dir)
    {
        agent.isStopped = true;
        float elapsed = 0f;
        float lungeDuration = 0.15f;

        while (elapsed < lungeDuration)
        {
            agent.Move(dir * attackLungeForce * Time.deltaTime);
            elapsed += Time.deltaTime;
            yield return null;
        }
    }

    void Idle()
    {
        agent.isStopped = true;
        agent.velocity = Vector3.zero;
    }

    void UpdateAnimator()
    {
        // NavMeshAgent'ın gerçek hızını Animator'a ver
        float speed = agent.velocity.magnitude;
        animator.SetFloat(SpeedHash, speed);
    }

    // Animation Event
    public void DealDamageToPlayer()
    {
        float dist = Vector3.Distance(transform.position, player.position);
        if (dist <= attackRange + 0.5f)
        {
            player.GetComponent<PlayerHealth>()?.TakeDamage(attackDamage);
            Debug.Log($"💢 ENEMY: Player'a {attackDamage} hasar!");
        }
    }

    // Animation Event
    public void OnAttackEnd()
    {
        isAttacking = false;
        agent.isStopped = false;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}