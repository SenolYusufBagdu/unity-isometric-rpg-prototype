using UnityEngine;

[RequireComponent(typeof(Animator))]
public class PlayerAttack : MonoBehaviour
{
    [Header("Kılıç (E tuşu)")]
    public float attackCooldown = 0.4f;
    public float comboWindow = 0.8f;
    public GameObject swordObject;

    [Header("Ok / Yay (A tuşu)")]
    public GameObject bowObject;
    public GameObject arrowPrefab;
    public Transform arrowFirePoint;
    public float arrowCooldown = 0.6f;
    public float arrowSpeed = 25f;
    public float arrowDamage = 20f;
    public float arrowSpawnDelay = 0.3f; // Animasyonun kaçıncı saniyesinde ok fırlar

    [Header("1. Büyü - Q (sağ tık + Q, 1.5sn cooldown)")]
    public GameObject projectilePrefab;
    public Transform firePoint;
    public float projectileCooldown = 1.5f;

    [Header("2. Büyü - W (sağ tık + W, 1.5sn cooldown)")]
    public GameObject fireballPrefab;
    public Transform fireballPoint;
    public float fireballCooldown = 1.5f;

    [Header("Nişan Alma")]
    public LayerMask groundLayer;
    public AimIndicator aimIndicator;

    // Public properties
    public bool IsCasting => isCasting;
    public bool IsAiming => isAiming;
    public bool IsAttacking => isAttacking;
    public bool IsBowMode => isBowMode;
    public float LastProjectileTime => lastProjectileTime;
    public float LastFireballTime => lastFireballTime;
    public float ProjectileCooldown => projectileCooldown;
    public float FireballCooldown => fireballCooldown;

    // Private
    private Animator animator;
    private PlayerMouseRotation mouseRot;
    private Rigidbody rb;
    private SwordDamage swordDamage;
    private Camera mainCamera;

    private bool isCasting = false;
    private bool isAiming = false;
    private bool isAttacking = false;
    private bool isBowMode = false;
    private bool isSwitching = false;

    private int comboStep = 0;
    private int queuedSpell = 0;

    private float lastAttackTime = -99f;
    private float lastProjectileTime = -99f;
    private float lastFireballTime = -99f;
    private float lastArrowTime = -99f;

    private static readonly int Attack1Hash = Animator.StringToHash("Attack1");
    private static readonly int Attack2Hash = Animator.StringToHash("Attack2");
    private static readonly int Spell1Hash = Animator.StringToHash("Spell1");
    private static readonly int Spell2Hash = Animator.StringToHash("Spell2");
    private static readonly int SwordSheatheHash = Animator.StringToHash("SwordSheathe");
    private static readonly int SwordDrawHash = Animator.StringToHash("SwordDraw");
    private static readonly int ArrowHash = Animator.StringToHash("Arrow");

    void Start()
    {
        animator = GetComponent<Animator>();
        mouseRot = GetComponent<PlayerMouseRotation>();
        rb = GetComponent<Rigidbody>();
        mainCamera = Camera.main;

        if (swordObject != null)
        {
            swordObject.SetActive(true); // Kılıcın oyun başında aktif olmasını garanti ettik
            swordDamage = swordObject.GetComponent<SwordDamage>();
            swordDamage?.DisableDamage();
        }

        if (bowObject != null)
        {
            bowObject.SetActive(false); // Yayın oyun başında KESİN OLARAK kapalı olmasını sağladık
        }

        if (aimIndicator == null)
            aimIndicator = GetComponent<AimIndicator>();

        Debug.Log("✅ PLAYER ATTACK: Hazır.");
    }

    void Update()
    {
        if (isAttacking && Time.time >= lastAttackTime + attackCooldown + 0.05f)
            isAttacking = false;

        if (Input.GetMouseButtonDown(0) && isCasting && !isAiming)
        {
            isCasting = false;
        }

        // A tuşu → Ok moduna geç
        if (Input.GetKeyDown(KeyCode.A) && !isBowMode && !isSwitching && !isCasting && !isAiming)
        {
            StartCoroutine(SwitchToBow());
            return;
        }

        // A tuşu (ok modundayken) → ok at
        if (Input.GetKeyDown(KeyCode.A) && isBowMode && !isSwitching)
        {
            if (Time.time >= lastArrowTime + arrowCooldown)
            {
                lastArrowTime = Time.time;
                RotateTowardsMouse();
                animator.SetTrigger(ArrowHash);

                // Animation Event yerine Invoke — arrowSpawnDelay saniye sonra oku fırlat
                CancelInvoke(nameof(SpawnArrow));
                Invoke(nameof(SpawnArrow), arrowSpawnDelay);

                Debug.Log("🏹 Ok ateşlendi!");
            }
            else
            {
                float kalan = (lastArrowTime + arrowCooldown) - Time.time;
                Debug.Log($"⏳ Ok cooldown: {kalan:F1}s kaldı");
            }
            return;
        }

        // E tuşu (ok modundayken) → kılıç moduna dön
        if (Input.GetKeyDown(KeyCode.E) && isBowMode && !isSwitching)
        {
            StartCoroutine(SwitchToSword());
            return;
        }

        HandleAiming();
        if (isCasting) return;
        if (!isBowMode) HandleCombo();
    }

    // ── MOD GEÇİŞLERİ ──────────────────────────────────────

    System.Collections.IEnumerator SwitchToBow()
    {
        isSwitching = true;
        LockMovement();
        Debug.Log("🗡→🏹 Kılıç kılıfa giriyor...");

        animator.SetTrigger(SwordSheatheHash);
        yield return new WaitForSeconds(0.05f);

        float sheatheWait = GetAnimationLength("SwordSheathe");
        if (sheatheWait <= 0f) sheatheWait = 0.6f;
        yield return new WaitForSeconds(sheatheWait);

        if (swordObject != null) swordObject.SetActive(false);
        if (bowObject != null) bowObject.SetActive(true);

        isBowMode = true;
        isSwitching = false;
        Debug.Log("✅ Yay modu aktif!");
    }

    System.Collections.IEnumerator SwitchToSword()
    {
        isSwitching = true;
        LockMovement();
        Debug.Log("🏹→🗡 Kılıç çekiliyor...");

        if (bowObject != null) bowObject.SetActive(false);
        if (swordObject != null) swordObject.SetActive(true);
        animator.SetTrigger(SwordDrawHash);

        yield return new WaitForSeconds(0.05f);

        float drawWait = GetAnimationLength("SwordDraw");
        if (drawWait <= 0f) drawWait = 0.5f;
        yield return new WaitForSeconds(drawWait);

        isBowMode = false;
        isSwitching = false;
        comboStep = 0;
        Debug.Log("✅ Kılıç modu aktif!");
    }

    float GetAnimationLength(string stateName)
    {
        if (animator == null) return 0f;
        RuntimeAnimatorController rac = animator.runtimeAnimatorController;
        if (rac == null) return 0f;
        foreach (AnimationClip clip in rac.animationClips)
            if (clip.name == stateName) return clip.length * 0.85f;
        return 0f;
    }

    // ── BÜYÜ / NİŞAN ALMA ───────────────────────────────────

    void HandleAiming()
    {
        if (Input.GetMouseButtonDown(1))
        {
            isAiming = true;
            queuedSpell = 0;
            LockMovement();
        }

        if (isAiming)
        {
            LockMovement();
            RotateTowardsMouse();
            UpdateAimIndicator();

            if (Input.GetKeyDown(KeyCode.Q))
            {
                if (Time.time >= lastProjectileTime + projectileCooldown)
                {
                    queuedSpell = 1;
                    Debug.Log("⚡ Q sıraya alındı");
                }
                else
                {
                    float kalan = (lastProjectileTime + projectileCooldown) - Time.time;
                    Debug.Log($"⏳ Q cooldown: {kalan:F1}s kaldı");
                }
            }

            if (Input.GetKeyDown(KeyCode.W))
            {
                if (Time.time >= lastFireballTime + fireballCooldown)
                {
                    queuedSpell = 2;
                    Debug.Log("🔥 W sıraya alındı");
                }
                else
                {
                    float kalan = (lastFireballTime + fireballCooldown) - Time.time;
                    Debug.Log($"⏳ W cooldown: {kalan:F1}s kaldı");
                }
            }
        }

        if (Input.GetMouseButtonUp(1) && isAiming)
        {
            isAiming = false;
            aimIndicator?.Hide();

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

    bool GetMouseWorldPoint(out Vector3 point)
    {
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);

        if (groundLayer.value != 0 &&
            Physics.Raycast(ray, out RaycastHit hitGround, 200f, groundLayer))
        {
            point = hitGround.point;
            return true;
        }

        if (Physics.Raycast(ray, out RaycastHit hitAny, 200f))
        {
            point = hitAny.point;
            point.y = transform.position.y;
            return true;
        }

        point = Vector3.zero;
        return false;
    }

    void RotateTowardsMouse()
    {
        if (!GetMouseWorldPoint(out Vector3 hit)) return;
        Vector3 dir = hit - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude > 0.01f)
            transform.rotation = Quaternion.Slerp(
                transform.rotation, Quaternion.LookRotation(dir), 20f * Time.deltaTime);
    }

    void UpdateAimIndicator()
    {
        if (aimIndicator == null) return;
        if (GetMouseWorldPoint(out Vector3 hit))
            aimIndicator.Show(hit);
    }

    void HandleCombo()
    {
        if (isAiming) return;

        if (comboStep == 1 && Time.time > lastAttackTime + comboWindow)
            comboStep = 0;

        if (Input.GetKeyDown(KeyCode.E) && Time.time >= lastAttackTime + attackCooldown)
        {
            lastAttackTime = Time.time;
            isAttacking = true;

            if (comboStep == 0)
            {
                comboStep = 1;
                animator.SetTrigger(Attack1Hash);
                Debug.Log("⚔️ Attack 1");
            }
            else
            {
                comboStep = 0;
                animator.SetTrigger(Attack2Hash);
                Debug.Log("⚔️⚔️ Attack 2");
            }
        }
    }

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

    // ── ANIMATION EVENTS ────────────────────────────────────

    public void OnAttackEnd() { isAttacking = false; }
    public void OnSpellEnd() { isCasting = false; }

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

    // ── OK SPAWN (Invoke ile çağrılır) ──────────────────────

    public void SpawnArrow()
    {
        if (arrowPrefab == null)
        {
            Debug.LogError("❌ arrowPrefab atanmamış! PlayerAttack Inspector'ını kontrol et.");
            return;
        }

        Transform spawnPoint = arrowFirePoint != null ? arrowFirePoint : transform;
        GameObject arrowGO = Instantiate(arrowPrefab, spawnPoint.position, transform.rotation);

        ArrowProjectile ap = arrowGO.GetComponent<ArrowProjectile>();
        if (ap != null)
        {
            ap.damage = arrowDamage;
            ap.speed = arrowSpeed;
        }

        Debug.Log("🏹 Ok fırlatıldı!");
    }
}