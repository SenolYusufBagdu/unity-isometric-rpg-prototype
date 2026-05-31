using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(AudioSource))]
public class PlayerDash : MonoBehaviour
{
    [Header("Dash Ayarları")]
    public float dashSpeed = 20f;
    public float dashDuration = 0.2f;
    public float dashCooldown = 1f;
    public float dashDamage = 40f;

    [Header("VFX")]
    public GameObject dashVFXPrefab;

    [Header("Referanslar")]
    public DashHitbox dashHitbox;

    // ─────────────────────────────────────────────────────────
    // 🔊 SES SLOTLARI
    // ─────────────────────────────────────────────────────────
    [Header("─ Sesler")]
    [Tooltip("Dash başlangıç sesi — Shift'e basınca çalar\n(hızlı whoosh / rüzgar kesme sesi)")]
    public AudioClip dashSound;

    [Range(0f, 1f)]
    public float soundVolume = 1f;
    // ─────────────────────────────────────────────────────────

    private Rigidbody rb;
    private Animator animator;
    private PlayerMouseRotation mouseRot;
    private PlayerHealth playerHealth;
    private AudioSource audioSource;

    private bool isDashing;
    private float dashTimer;
    private float lastDashTime;
    private Vector3 dashDirection;
    private GameObject activeVFX;

    public bool IsDashing => isDashing;

    private static readonly int DashHash = Animator.StringToHash("Dash");

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        animator = GetComponent<Animator>();
        mouseRot = GetComponent<PlayerMouseRotation>();
        playerHealth = GetComponent<PlayerHealth>();
        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;

        if (dashHitbox != null)
            dashHitbox.SetActive(false);
    }

    void Update()
    {
        if (isDashing)
        {
            PerformDash();
            return;
        }

        if (Input.GetKeyDown(KeyCode.LeftShift) && Time.time >= lastDashTime + dashCooldown)
            StartDash();
    }

    void StartDash()
    {
        isDashing = true;
        dashTimer = dashDuration;
        lastDashTime = Time.time;

        if (mouseRot != null && mouseRot.IsMovingToTarget)
            dashDirection = (mouseRot.ClickTarget - transform.position).normalized;
        else
            dashDirection = transform.forward;

        dashDirection.y = 0f;

        if (dashDirection != Vector3.zero)
            transform.rotation = Quaternion.LookRotation(dashDirection);

        if (playerHealth != null) playerHealth.SetInvulnerable(true);

        if (dashHitbox != null)
        {
            dashHitbox.SetDamage(dashDamage);
            dashHitbox.SetActive(true);
        }

        animator?.SetTrigger(DashHash);

        // 🔊 Dash sesi
        if (dashSound != null && audioSource != null)
            audioSource.PlayOneShot(dashSound, soundVolume);

        if (dashVFXPrefab != null)
        {
            activeVFX = Instantiate(dashVFXPrefab, transform.position, Quaternion.identity);
            activeVFX.transform.SetParent(transform);
        }
    }

    void PerformDash()
    {
        dashTimer -= Time.deltaTime;
        rb.linearVelocity = new Vector3(
            dashDirection.x * dashSpeed,
            rb.linearVelocity.y,
            dashDirection.z * dashSpeed
        );

        if (dashTimer <= 0f)
            EndDash();
    }

    void EndDash()
    {
        isDashing = false;
        rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);

        if (mouseRot != null)
            mouseRot.CancelTarget();

        if (playerHealth != null) playerHealth.SetInvulnerable(false);
        if (dashHitbox != null) dashHitbox.SetActive(false);

        if (activeVFX != null)
        {
            Destroy(activeVFX);
            activeVFX = null;
        }
    }
}