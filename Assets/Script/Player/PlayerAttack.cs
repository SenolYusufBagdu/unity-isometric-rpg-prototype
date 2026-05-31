using UnityEngine;

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(AudioSource))]
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
    public float arrowDamage = 50f;
    public float arrowSpawnDelay = 0.3f;

    [Header("1. Büyü - Q")]
    public GameObject projectilePrefab;
    public Transform firePoint;
    public float projectileCooldown = 1.5f;

    [Header("2. Büyü - W")]
    public GameObject fireballPrefab;
    public Transform fireballPoint;
    public float fireballCooldown = 1.5f;

    [Header("Nişan Alma")]
    public LayerMask groundLayer;
    public AimIndicator aimIndicator;

    [Header("─ Sesler: Kılıç")]
    public AudioClip swordSwingSound;
    public AudioClip swordDrawSound;
    public AudioClip swordSheatheSound;

    [Header("─ Sesler: Ok")]
    public AudioClip bowFireSound;

    [Header("─ Sesler: Büyü")]
    public AudioClip spell1Sound;
    public AudioClip spell2Sound;
    public AudioClip aimStartSound;

    [Header("─ Ses Ayarları")]
    [Range(0f, 1f)] public float soundVolume = 1f;

    // ── Büyü hasar çarpanı — PlayerLevelSystem tarafından artırılır ──
    [HideInInspector] public float spellDamageMultiplier = 2f;

    // Public properties
    public bool IsCasting => isCasting;
    public bool IsAiming => isAiming;
    public bool IsAttacking => isAttacking;
    public bool IsBowMode => isBowMode;

    public float LastProjectileTime => lastProjectileTime;
    public float LastFireballTime => lastFireballTime;
    public float ProjectileCooldown => projectileCooldown;
    public float FireballCooldown => fireballCooldown;

    private Animator animator;
    private PlayerMouseRotation mouseRot;
    private Rigidbody rb;
    private SwordDamage swordDamage;
    private Camera mainCamera;
    private AudioSource audioSource;

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
        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;

        if (swordObject != null)
        {
            swordObject.SetActive(true);
            swordDamage = swordObject.GetComponent<SwordDamage>();
            swordDamage?.DisableDamage();
        }

        if (bowObject != null)
            bowObject.SetActive(false);

        if (aimIndicator == null)
            aimIndicator = GetComponent<AimIndicator>();
    }

    void PlaySound(AudioClip clip)
    {
        if (clip == null || audioSource == null) return;
        audioSource.PlayOneShot(clip, soundVolume);
    }

    void Update()
    {
        if (isAttacking && Time.time >= lastAttackTime + attackCooldown + 0.05f)
            isAttacking = false;

        if (Input.GetMouseButtonDown(0) && isCasting && !isAiming)
            isCasting = false;

        if (Input.GetKeyDown(KeyCode.A) && !isBowMode && !isSwitching && !isCasting && !isAiming)
        {
            StartCoroutine(SwitchToBow());
            return;
        }

        if (Input.GetKeyDown(KeyCode.A) && isBowMode && !isSwitching)
        {
            if (Time.time >= lastArrowTime + arrowCooldown)
            {
                lastArrowTime = Time.time;
                RotateTowardsMouse();
                animator.SetTrigger(ArrowHash);
                PlaySound(bowFireSound);
                CancelInvoke(nameof(SpawnArrow));
                Invoke(nameof(SpawnArrow), arrowSpawnDelay);
            }
            return;
        }

        if (Input.GetKeyDown(KeyCode.E) && isBowMode && !isSwitching)
        {
            StartCoroutine(SwitchToSword());
            return;
        }

        HandleAiming();
        if (isCasting) return;
        if (!isBowMode) HandleCombo();
    }

    System.Collections.IEnumerator SwitchToBow()
    {
        isSwitching = true;
        LockMovementFull();
        PlaySound(swordSheatheSound);
        animator.SetTrigger(SwordSheatheHash);
        yield return new WaitForSeconds(0.05f);

        float w = GetAnimationLength("SwordSheathe");
        if (w <= 0f) w = 0.6f;
        yield return new WaitForSeconds(w);

        if (swordObject != null) swordObject.SetActive(false);
        if (bowObject != null) bowObject.SetActive(true);

        isBowMode = true;
        isSwitching = false;
    }

    System.Collections.IEnumerator SwitchToSword()
    {
        isSwitching = true;
        LockMovementFull();

        if (bowObject != null) bowObject.SetActive(false);
        if (swordObject != null) swordObject.SetActive(true);
        animator.SetTrigger(SwordDrawHash);
        PlaySound(swordDrawSound);
        yield return new WaitForSeconds(0.05f);

        float w = GetAnimationLength("SwordDraw");
        if (w <= 0f) w = 0.5f;
        yield return new WaitForSeconds(w);

        isBowMode = false;
        isSwitching = false;
        comboStep = 0;
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

    void HandleAiming()
    {
        if (Input.GetMouseButtonDown(1))
        {
            isAiming = true;
            PlaySound(aimStartSound);
        }

        if (isAiming)
        {
            LockMovementFull();
            RotateTowardsMouse();
            UpdateAimIndicator();

            if (Input.GetKeyDown(KeyCode.Q) && Time.time >= lastProjectileTime + projectileCooldown)
                queuedSpell = 1;

            if (Input.GetKeyDown(KeyCode.W) && Time.time >= lastFireballTime + fireballCooldown)
                queuedSpell = 2;
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
                PlaySound(spell1Sound);
            }
            else if (queuedSpell == 2)
            {
                lastFireballTime = Time.time;
                StartCast();
                animator.SetTrigger(Spell2Hash);
                PlaySound(spell2Sound);
            }

            queuedSpell = 0;
        }
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
            RotateTowardsMouse();
            PlaySound(swordSwingSound);

            if (comboStep == 0) { comboStep = 1; animator.SetTrigger(Attack1Hash); }
            else { comboStep = 0; animator.SetTrigger(Attack2Hash); }
        }
    }

    bool GetMouseWorldPoint(out Vector3 point)
    {
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);

        if (groundLayer.value != 0 &&
            Physics.Raycast(ray, out RaycastHit hitGround, 200f, groundLayer))
        { point = hitGround.point; return true; }

        if (Physics.Raycast(ray, out RaycastHit hitAny, 200f))
        { point = hitAny.point; point.y = transform.position.y; return true; }

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

    void LockMovementFull()
    {
        mouseRot?.CancelTarget();
        if (rb != null) rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
    }

    void StartCast()
    {
        isCasting = true;
        LockMovementFull();
    }

    public void OnAttackEnd() => isAttacking = false;
    public void OnSpellEnd() => isCasting = false;

    // ── Büyü spawn — çarpanı uygula ──
    public void SpawnProjectile()
    {
        if (projectilePrefab == null || firePoint == null) return;
        GameObject go = Instantiate(projectilePrefab, firePoint.position, transform.rotation);
        Projectile p = go.GetComponent<Projectile>();
        if (p != null) p.damage = Mathf.RoundToInt(p.damage * spellDamageMultiplier);
    }

    public void SpawnFireball()
    {
        if (fireballPrefab == null || fireballPoint == null) return;
        GameObject go = Instantiate(fireballPrefab, fireballPoint.position, transform.rotation);
        Projectile p = go.GetComponent<Projectile>();
        if (p != null) p.damage = Mathf.RoundToInt(p.damage * spellDamageMultiplier);
    }

    public void EnableSwordDamage() => swordDamage?.EnableDamage();
    public void DisableSwordDamage() => swordDamage?.DisableDamage();

    public void SpawnArrow()
    {
        if (arrowPrefab == null) return;

        if (ArrowCounter.Instance != null)
        {
            if (ArrowCounter.Instance.GetArrowCount() <= 0) return;
            ArrowCounter.Instance.UseArrow();
        }

        Transform sp = arrowFirePoint != null ? arrowFirePoint : transform;
        GameObject arrowGO = Instantiate(arrowPrefab, sp.position, transform.rotation);

        ArrowProjectile ap = arrowGO.GetComponent<ArrowProjectile>();
        if (ap != null)
        {
            ap.damage = arrowDamage;
            ap.speed = arrowSpeed;
        }
    }
}