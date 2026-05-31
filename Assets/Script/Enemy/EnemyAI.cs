using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// EnemyAI.cs — Tüm hareket ve saldırı mantığı burada.
/// Ses slotları eklendi.
///
/// ÖNEMLİ: EnemyHealth scriptine şu iki event eklenmiş olmalı:
///   public System.Action OnHurt;
///   public System.Action OnDied;
/// TakeDamage() içinde OnHurt?.Invoke(), Die() içinde OnDied?.Invoke() çağır.
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(AudioSource))]
public class EnemyAI : MonoBehaviour
{
    [Header("─ Algılama")]
    [SerializeField] public float detectionRange = 35f;
    [SerializeField] public float attackRange = 2f;

    [Header("─ Player Interrupt")]
    [Tooltip("Player bu mesafeye girerse enemy kaleyi bırakıp player'a saldırır")]
    [SerializeField] public float playerInterruptRange = 6f;

    [Header("─ Player Saldırı")]
    [SerializeField] public float attackCooldown = 2f;
    [SerializeField] public float attackDamage = 15f;
    [SerializeField] public float attackLungeForce = 3f;

    [Header("─ Kale Saldırı")]
    [SerializeField] public float castleAttackRange = 3.5f;
    [SerializeField] public float castleAttackDamage = 20f;
    [SerializeField] public float castleAttackCooldown = 2f;
    [SerializeField] public float castleSurfaceOffset = 1.5f;

    [Header("─ NavMesh")]
    [SerializeField] public float moveSpeed = 3f;
    [SerializeField] public float acceleration = 8f;
    [SerializeField] public float angularSpeed = 300f;
    [SerializeField] public float stoppingDistance = 1.5f;

    [HideInInspector] public float playerTargetChance = 0f;

    [Header("─ Referanslar (otomatik bulunur)")]
    [SerializeField] public Transform player;
    [SerializeField] public CastleHealth castle;

    // ─────────────────────────────────────────────────────────
    // 🔊 SES SLOTLARI
    // ─────────────────────────────────────────────────────────
    [Header("─ Sesler: Saldırı")]
    [Tooltip("Player'a saldırı sesi — her vurduğunda çalar")]
    public AudioClip attackPlayerSound;

    [Tooltip("Kaleye saldırı sesi — kaleye her vurduğunda çalar\n(darbe + taş/ahşap çarpma sesi idealdir)")]
    public AudioClip attackCastleSound;

    [Header("─ Sesler: Hasar / Ölüm")]
    [Tooltip("Hasar alma sesi — enemy vurulunca çalar\n(acı çekme, inleme sesi)")]
    public AudioClip hurtSound;

    [Tooltip("Ölüm sesi — enemy ölünce çalar\n(son nefes, düşme sesi)")]
    public AudioClip deathSound;

    [Header("─ Sesler: Ambient")]
    [Tooltip("Growl / nefes sesi — periyodik olarak çalar\n(enemy'nin varlığını hissettiren ambient ses)\nOpsiyonel, bırakabilirsin")]
    public AudioClip growlSound;

    [Tooltip("Growl sesi kaç saniyede bir çalar")]
    public float growlInterval = 6f;

    [Range(0f, 1f)]
    public float soundVolume = 1f;
    // ─────────────────────────────────────────────────────────

    private NavMeshAgent agent;
    private Animator animator;
    private EnemyHealth enemyHealth;
    private AudioSource audioSource;

    private float lastAttackTime;
    private float lastCastleAttackTime;
    private float lastGrowlTime;
    private bool isAttackingPlayer;

    private float castleRadius = 3f;

    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int AttackHash = Animator.StringToHash("Attack");

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponentInChildren<Animator>();
        enemyHealth = GetComponent<EnemyHealth>();
        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 1f;   // 3D ses
        audioSource.maxDistance = 20f;
        audioSource.rolloffMode = AudioRolloffMode.Linear;

        // EnemyHealth ses hookları — EnemyHealth'te OnHurt ve OnDied event'i olmalı
        if (enemyHealth != null)
        {
            enemyHealth.OnHurt += () => PlaySound(hurtSound);
            enemyHealth.OnDied += () => PlaySound(deathSound);
        }

        agent.speed = moveSpeed;
        agent.acceleration = acceleration;
        agent.angularSpeed = angularSpeed;
        agent.stoppingDistance = stoppingDistance;
        agent.updateRotation = true;
        agent.autoBraking = true;

        if (player == null)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
            else Debug.LogError($"❌ {name}: 'Player' tag'li obje yok!");
        }

        if (castle == null) castle = CastleHealth.Instance;

        if (castle != null)
        {
            var col = castle.GetComponent<Collider>();
            if (col != null)
                castleRadius = col.bounds.extents.magnitude * 0.6f;
            else
                castleRadius = 3f;
        }

        lastCastleAttackTime = Time.time - Random.Range(0f, castleAttackCooldown);
        lastAttackTime = Time.time - Random.Range(0f, attackCooldown);
        lastGrowlTime = Time.time + Random.Range(0f, growlInterval);

        if (!agent.isOnNavMesh)
            Debug.LogError($"❌ {name}: NavMesh üzerinde değil!");
    }

    public void DecideTarget()
    {
        Debug.Log($"[{name}] Hedef: KALE (player yaklaşınca interrupt)");
    }

    void PlaySound(AudioClip clip)
    {
        if (clip == null || audioSource == null) return;
        audioSource.PlayOneShot(clip, soundVolume);
    }

    void Update()
    {
        if (enemyHealth != null && enemyHealth.isDead) return;
        if (!agent.isOnNavMesh) return;

        // 🔊 Periyodik growl
        if (growlSound != null && Time.time >= lastGrowlTime + growlInterval)
        {
            lastGrowlTime = Time.time;
            PlaySound(growlSound);
        }

        if (isAttackingPlayer && Time.time >= lastAttackTime + attackCooldown)
            isAttackingPlayer = false;

        if (isAttackingPlayer) return;

        // INTERRUPT: player yakınsa kovala
        if (player != null)
        {
            float distToPlayer = Vector3.Distance(transform.position, player.position);
            if (distToPlayer <= playerInterruptRange)
            {
                if (distToPlayer <= attackRange)
                {
                    StopAgent();
                    FaceTarget(player.position);
                    TryAttackPlayer();
                }
                else
                {
                    agent.isStopped = false;
                    agent.stoppingDistance = stoppingDistance;
                    agent.SetDestination(player.position);
                }
                UpdateAnimator();
                return;
            }
        }

        if (castle != null && !castle.isDestroyed)
            HandleCastleTarget();
        else if (player != null)
            HandlePlayerTarget();

        UpdateAnimator();
    }

    void HandlePlayerTarget()
    {
        float dist = Vector3.Distance(transform.position, player.position);

        if (dist <= attackRange)
        {
            StopAgent();
            FaceTarget(player.position);
            TryAttackPlayer();
        }
        else if (dist <= detectionRange)
        {
            agent.isStopped = false;
            agent.stoppingDistance = stoppingDistance;
            agent.SetDestination(player.position);
        }
        else
        {
            agent.isStopped = true;
        }
    }

    void TryAttackPlayer()
    {
        if (Time.time < lastAttackTime + attackCooldown) return;

        lastAttackTime = Time.time;
        isAttackingPlayer = true;
        animator?.SetTrigger(AttackHash);
        PlaySound(attackPlayerSound); // 🔊 Player'a saldırı sesi

        Vector3 dir = (player.position - transform.position).normalized;
        dir.y = 0f;
        StartCoroutine(LungeCoroutine(dir));
    }

    void HandleCastleTarget()
    {
        Vector3 castleEdgePoint = GetCastleSurfacePoint();
        float distToEdge = Vector3.Distance(transform.position, castleEdgePoint);

        if (distToEdge <= castleAttackRange)
        {
            StopAgent();
            FaceTarget(castle.transform.position);
            TryAttackCastle();
        }
        else
        {
            agent.isStopped = false;
            agent.stoppingDistance = castleSurfaceOffset;
            agent.SetDestination(castleEdgePoint);
        }
    }

    Vector3 GetCastleSurfacePoint()
    {
        Vector3 toEnemy = (transform.position - castle.transform.position).normalized;
        Vector3 surfacePoint = castle.transform.position + toEnemy * (castleRadius + castleSurfaceOffset);
        surfacePoint.y = transform.position.y;

        if (NavMesh.SamplePosition(surfacePoint, out NavMeshHit hit, 3f, NavMesh.AllAreas))
            return hit.position;

        return surfacePoint;
    }

    void TryAttackCastle()
    {
        if (Time.time < lastCastleAttackTime + castleAttackCooldown) return;

        lastCastleAttackTime = Time.time;
        castle.TakeDamage(castleAttackDamage);
        animator?.SetTrigger(AttackHash);
        PlaySound(attackCastleSound); // 🔊 Kaleye saldırı sesi
        Debug.Log($"🏰 [{name}] kaleye vurdu: -{castleAttackDamage} (kalan: {castle.currentHealth:F0})");
    }

    void StopAgent()
    {
        agent.isStopped = true;
        agent.velocity = Vector3.zero;
    }

    void FaceTarget(Vector3 pos)
    {
        Vector3 dir = (pos - transform.position).normalized;
        dir.y = 0f;
        if (dir.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.LookRotation(dir);
    }

    void UpdateAnimator()
    {
        if (animator == null) return;
        float speed = agent.velocity.magnitude;
        animator.SetFloat(SpeedHash, speed);
    }

    System.Collections.IEnumerator LungeCoroutine(Vector3 dir)
    {
        float elapsed = 0f, dur = 0.15f;
        while (elapsed < dur)
        {
            if (agent.isOnNavMesh)
                agent.Move(dir * attackLungeForce * Time.deltaTime);
            elapsed += Time.deltaTime;
            yield return null;
        }
    }

    public void DealDamageToPlayer()
    {
        if (player == null) return;
        if (Vector3.Distance(transform.position, player.position) <= attackRange + 0.5f)
            player.GetComponent<PlayerHealth>()?.TakeDamage(attackDamage);
    }

    public void OnAttackEnd()
    {
        isAttackingPlayer = false;
        if (agent.isOnNavMesh) agent.isStopped = false;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, castleAttackRange);
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, playerInterruptRange);

        if (castle != null)
        {
            Gizmos.color = new Color(0f, 1f, 1f, 0.2f);
            Gizmos.DrawWireSphere(castle.transform.position, castleRadius + castleSurfaceOffset);
        }
    }
}