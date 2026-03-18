using UnityEngine;

/// <summary>
/// PlayerAttack.cs
///
/// BÜYÜ MEKANİĞİ:
/// - Q ve W SADECE sağ tık nişan alındıktan sonra çalışır
/// - Sağ tık basılı → nişan al → Q veya W bas → sağ tıktan el çek → ateşle
/// - Sağ tık olmadan Q/W basmak hiçbir şey yapmaz
/// - Kılıç (E) her zaman çalışır, sağ tıktan bağımsız
///
/// NİŞAN DÜZELTMESİ:
/// - Önce groundLayer'a bakar, bulamazsa tüm collider'lara bakar
/// - Enemy üstünde, boş alanda, her yerde nişan çalışır
/// - Ground Layer atanmamış olsa bile çalışır
///
/// ANIMATION EVENTS:
/// - Spell1 ve Spell2 animasyonlarının son karesine OnSpellEnd() ekle
/// </summary>
[RequireComponent(typeof(Animator))]
public class PlayerAttack : MonoBehaviour
{
    [Header("Kılıç (E tuşu)")]
    public float attackCooldown = 0.3f;
    public float comboWindow = 0.8f;
    public GameObject swordObject;

    [Header("1. Büyü - Q tuşu (sağ tık gerekli)")]
    public GameObject projectilePrefab;
    public Transform firePoint;
    public float projectileCooldown = 0.5f;

    [Header("2. Büyü - W tuşu (sağ tık gerekli)")]
    public GameObject fireballPrefab;
    public Transform fireballPoint;
    public float fireballCooldown = 1f;

    [Header("Nişan Alma")]
    public LayerMask groundLayer;   // Atanmazsa da çalışır
    public AimIndicator aimIndicator;

    // Dışarıdan okunabilir
    public bool IsCasting => isCasting;
    public bool IsAiming => isAiming;

    private Animator animator;
    private PlayerMouseRotation mouseRot;
    private Rigidbody rb;
    private SwordDamage swordDamage;
    private Camera mainCamera;

    private bool isCasting = false;
    private bool isAiming = false;
    private int comboStep = 0;
    private int queuedSpell = 0; // 0=yok 1=Q 2=W

    private float lastAttackTime;
    private float lastProjectileTime;
    private float lastFireballTime;

    private static readonly int Attack1Hash = Animator.StringToHash("Attack1");
    private static readonly int Attack2Hash = Animator.StringToHash("Attack2");
    private static readonly int Spell1Hash = Animator.StringToHash("Spell1");
    private static readonly int Spell2Hash = Animator.StringToHash("Spell2");

    void Start()
    {
        animator = GetComponent<Animator>();
        mouseRot = GetComponent<PlayerMouseRotation>();
        rb = GetComponent<Rigidbody>();
        mainCamera = Camera.main;

        if (swordObject != null)
        {
            swordDamage = swordObject.GetComponent<SwordDamage>();
            if (swordDamage != null)
                swordDamage.DisableDamage();
            else
                Debug.LogError("❌ SW08 üzerinde SwordDamage scripti yok!");
        }

        if (aimIndicator == null)
            aimIndicator = GetComponent<AimIndicator>();

        if (projectilePrefab == null) Debug.LogWarning("⚠️ Q Projectile Prefab atanmamış!");
        if (firePoint == null) Debug.LogWarning("⚠️ Fire Point atanmamış!");
        if (fireballPrefab == null) Debug.LogWarning("⚠️ W Fireball Prefab atanmamış!");
        if (fireballPoint == null) Debug.LogWarning("⚠️ Fireball Point atanmamış!");

        Debug.Log("✅ PLAYER ATTACK: Hazır.");
    }

    void Update()
    {
        HandleAiming();
        if (isCasting) return;
        HandleCombo();
    }

    // =========================================================
    // SAĞ TIK — NİŞAN ALMA
    // =========================================================

    void HandleAiming()
    {
        if (Input.GetMouseButtonDown(1))
        {
            isAiming = true;
            queuedSpell = 0;
            LockMovement();
            Debug.Log("🎯 Nişan BAŞLADI");
        }

        if (isAiming)
        {
            LockMovement();
            RotateTowardsMouse();
            UpdateAimIndicator();

            if (Input.GetKeyDown(KeyCode.Q) && Time.time >= lastProjectileTime + projectileCooldown)
            {
                queuedSpell = 1;
                Debug.Log("⚡ Q sıraya alındı");
            }
            if (Input.GetKeyDown(KeyCode.W) && Time.time >= lastFireballTime + fireballCooldown)
            {
                queuedSpell = 2;
                Debug.Log("🔥 W sıraya alındı");
            }
        }

        if (Input.GetMouseButtonUp(1) && isAiming)
        {
            isAiming = false;
            aimIndicator?.Hide();
            Debug.Log("🎯 Nişan BİTTİ");

            if (queuedSpell == 1)
            {
                lastProjectileTime = Time.time;
                StartCast();
                animator.SetTrigger(Spell1Hash);
                Debug.Log("⚡ Q ateşlendi!");
            }
            else if (queuedSpell == 2)
            {
                lastFireballTime = Time.time;
                StartCast();
                animator.SetTrigger(Spell2Hash);
                Debug.Log("🔥 W ateşlendi!");
            }

            queuedSpell = 0;
        }
    }

    // =========================================================
    // RAYCAST — önce zemin, bulamazsa her şey
    // =========================================================

    bool GetMouseWorldPoint(out Vector3 point)
    {
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);

        // 1. Ground Layer atanmışsa önce zemine bak
        if (groundLayer.value != 0)
        {
            if (Physics.Raycast(ray, out RaycastHit hitGround, 200f, groundLayer))
            {
                point = hitGround.point;
                return true;
            }
        }

        // 2. Zemin bulunamadı veya layer atanmamış — her şeye bak
        // Enemy üstünde, nesne üstünde, boş alanda hepsi çalışır
        if (Physics.Raycast(ray, out RaycastHit hitAny, 200f))
        {
            point = hitAny.point;
            point.y = transform.position.y; // Çemberi yere sabitle
            return true;
        }

        point = Vector3.zero;
        return false;
    }

    void RotateTowardsMouse()
    {
        if (!GetMouseWorldPoint(out Vector3 hitPoint)) return;
        Vector3 dir = hitPoint - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude > 0.01f)
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                Quaternion.LookRotation(dir),
                20f * Time.deltaTime);
    }

    void UpdateAimIndicator()
    {
        if (aimIndicator == null) return;
        if (GetMouseWorldPoint(out Vector3 hitPoint))
            aimIndicator.Show(hitPoint);
    }

    // =========================================================
    // KILIÇ COMBO — sağ tıktan bağımsız
    // =========================================================

    void HandleCombo()
    {
        if (isAiming) return;

        if (comboStep == 1 && Time.time > lastAttackTime + comboWindow)
            comboStep = 0;

        if (Input.GetKeyDown(KeyCode.E) && Time.time >= lastAttackTime + attackCooldown)
        {
            lastAttackTime = Time.time;
            if (comboStep == 0) { comboStep = 1; animator.SetTrigger(Attack1Hash); Debug.Log("⚔️ Attack 1"); }
            else { comboStep = 0; animator.SetTrigger(Attack2Hash); Debug.Log("⚔️⚔️ Attack 2"); }
        }
    }

    // =========================================================
    // YARDIMCI
    // =========================================================

    void LockMovement()
    {
        mouseRot?.CancelTarget();
        if (rb != null) rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
    }

    void StartCast()
    {
        isCasting = true;
        LockMovement();
    }

    // Animation Event — Spell1 ve Spell2 animasyonlarının son karesine ekle
    public void OnSpellEnd()
    {
        isCasting = false;
        Debug.Log("✅ Büyü bitti — hareket açıldı");
    }

    // =========================================================
    // ANIMATION EVENTS
    // =========================================================

    public void SpawnProjectile()
    {
        if (projectilePrefab == null || firePoint == null) return;
        Instantiate(projectilePrefab, firePoint.position, transform.rotation);
        Debug.Log("⚡ Projektil ateşlendi!");
    }

    public void SpawnFireball()
    {
        if (fireballPrefab == null || fireballPoint == null) return;
        Instantiate(fireballPrefab, fireballPoint.position, transform.rotation);
        Debug.Log("🔥 Alev topu ateşlendi!");
    }

    public void EnableSwordDamage() { swordDamage?.EnableDamage(); }
    public void DisableSwordDamage() { swordDamage?.DisableDamage(); }
}