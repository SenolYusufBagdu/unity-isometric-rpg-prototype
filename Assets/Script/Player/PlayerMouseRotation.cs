using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class PlayerMouseRotation : MonoBehaviour
{
    [Header("Ayarlar")]
    public LayerMask groundLayer;
    public float rotationSpeed = 15f;
    public float stopDistance = 0.5f;
    public float moveSpeed = 3f;

    // ─────────────────────────────────────────────────────────
    // 🔊 SES SLOTLARI
    // ─────────────────────────────────────────────────────────
    [Header("─ Sesler: Ayak")]
    [Tooltip("Ayak sesi klipleri — hareket ederken rastgele biri çalar\nEn az 2-3 farklı klip ekle")]
    public AudioClip[] footstepSounds;

    [Tooltip("Ayak sesi aralığı (saniye) — 0.35-0.45 arası idealdir")]
    public float footstepInterval = 0.4f;

    [Range(0f, 1f)]
    public float footstepVolume = 0.6f;
    // ─────────────────────────────────────────────────────────

    private Camera mainCamera;
    private Rigidbody rb;
    private Animator animator;
    private PlayerDash playerDash;
    private PlayerAttack playerAttack;
    private AudioSource audioSource;

    private Vector3 clickTarget;
    private bool isMovingToTarget = false;
    private float footstepTimer = 0f;

    public bool IsMovingToTarget => isMovingToTarget;
    public Vector3 ClickTarget => clickTarget;

    private static readonly int SpeedHash = Animator.StringToHash("Speed");

    public void CancelTarget()
    {
        isMovingToTarget = false;
    }

    void Start()
    {
        mainCamera = Camera.main;
        rb = GetComponent<Rigidbody>();
        animator = GetComponent<Animator>();
        playerDash = GetComponent<PlayerDash>();
        playerAttack = GetComponent<PlayerAttack>();
        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;
    }

    void Update()
    {
        if (playerDash != null && playerDash.IsDashing) return;

        if (playerAttack != null && playerAttack.IsAiming)
        {
            animator.SetFloat(SpeedHash, 0f);
            isMovingToTarget = false;
            return;
        }

        if (Input.GetMouseButton(0))
        {
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, 100f, groundLayer))
            {
                clickTarget = hit.point;
                clickTarget.y = transform.position.y;
                isMovingToTarget = true;
            }
        }

        Vector3 hVel = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        float speed = hVel.magnitude;
        animator.SetFloat(SpeedHash, speed);

        // 🔊 Ayak sesi — dash sırasında çalmasın
        if (playerDash == null || !playerDash.IsDashing)
            HandleFootsteps(speed);
    }

    void HandleFootsteps(float speed)
    {
        if (footstepSounds == null || footstepSounds.Length == 0) return;

        if (speed < 0.5f)
        {
            footstepTimer = 0f;
            return;
        }

        footstepTimer -= Time.deltaTime;
        if (footstepTimer <= 0f)
        {
            footstepTimer = footstepInterval;
            AudioClip clip = footstepSounds[Random.Range(0, footstepSounds.Length)];
            audioSource.PlayOneShot(clip, footstepVolume);
        }
    }

    void FixedUpdate()
    {
        if (playerDash != null && playerDash.IsDashing) return;

        if (playerAttack != null && playerAttack.IsAiming)
        {
            rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
            return;
        }

        if (isMovingToTarget) MoveToTarget();
    }

    void MoveToTarget()
    {
        float distance = Vector3.Distance(transform.position, clickTarget);

        if (distance < stopDistance)
        {
            isMovingToTarget = false;
            rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
            return;
        }

        Vector3 dir = (clickTarget - transform.position).normalized;
        dir.y = 0f;

        transform.rotation = Quaternion.Slerp(
            transform.rotation, Quaternion.LookRotation(dir), rotationSpeed * Time.fixedDeltaTime);
        rb.linearVelocity = new Vector3(dir.x * moveSpeed, rb.linearVelocity.y, dir.z * moveSpeed);
    }

    void OnDrawGizmosSelected()
    {
        if (!isMovingToTarget) return;
        Gizmos.color = Color.cyan;
        Gizmos.DrawSphere(clickTarget, 0.2f);
    }
}