using UnityEngine;

/// <summary>
/// PlayerMovement.cs → carackte rv12'ye ekle
/// WASD hareketi KALDIRILDI — sadece mouse ile hareket
/// Animator parametreleri: Speed, IsMoving, IsGrounded, Jump
/// </summary>
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Animator))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Ground Check")]
    public Transform groundCheck;
    public float groundCheckRadius = 0.2f;
    public LayerMask groundLayer;

    [Header("Smoothing")]
    public float speedSmoothTime = 0.1f;

    private Rigidbody rb;
    private Animator animator;
    private bool isGrounded;
    private float currentSpeed;
    private float speedSmoothVelocity;

    [HideInInspector] public bool isMovingThisFrame;

    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int IsMovingHash = Animator.StringToHash("IsMoving");
    private static readonly int IsGroundedHash = Animator.StringToHash("IsGrounded");

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        animator = GetComponent<Animator>();
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
    }

    void Update()
    {
        CheckGround();
        UpdateAnimator();
    }

    void CheckGround()
    {
        if (groundCheck == null) return;
        isGrounded = Physics.CheckSphere(groundCheck.position, groundCheckRadius, groundLayer);
    }

    void UpdateAnimator()
    {
        float speed = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z).magnitude;
        currentSpeed = Mathf.SmoothDamp(currentSpeed, speed, ref speedSmoothVelocity, speedSmoothTime);
        isMovingThisFrame = currentSpeed > 0.1f;

        animator.SetFloat(SpeedHash, currentSpeed);
        animator.SetBool(IsMovingHash, isMovingThisFrame);
        animator.SetBool(IsGroundedHash, isGrounded);
    }

    void OnDrawGizmosSelected()
    {
        if (groundCheck == null) return;
        Gizmos.color = isGrounded ? Color.green : Color.red;
        Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
    }
}