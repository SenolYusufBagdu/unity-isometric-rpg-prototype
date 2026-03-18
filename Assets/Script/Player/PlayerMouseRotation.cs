using UnityEngine;

public class PlayerMouseRotation : MonoBehaviour
{
    [Header("Ayarlar")]
    public LayerMask groundLayer;
    public float rotationSpeed = 15f;
    public float stopDistance = 0.5f;
    public float moveSpeed = 3f;

    private Camera mainCamera;
    private Rigidbody rb;
    private Animator animator;
    private PlayerDash playerDash;
    private PlayerAttack playerAttack;

    private Vector3 clickTarget;
    private bool isMovingToTarget = false;

    public bool IsMovingToTarget => isMovingToTarget;
    public Vector3 ClickTarget => clickTarget;

    private static readonly int SpeedHash = Animator.StringToHash("Speed");

    public void CancelTarget()
    {
        isMovingToTarget = false;
        if (rb != null)
            rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
    }

    void Start()
    {
        mainCamera = Camera.main;
        rb = GetComponent<Rigidbody>();
        animator = GetComponent<Animator>();
        playerDash = GetComponent<PlayerDash>();
        playerAttack = GetComponent<PlayerAttack>();
    }

    void Update()
    {
        if (playerDash != null && playerDash.IsDashing) return;
        if (playerAttack != null && playerAttack.IsCasting) return;
        if (playerAttack != null && playerAttack.IsAiming) return; // Nişan sırasında sol tık hareketi yok

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

        Vector3 horizontalVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        animator.SetFloat(SpeedHash, horizontalVelocity.magnitude);
    }

    void FixedUpdate()
    {
        if (isMovingToTarget) MoveToTarget();
    }

    void MoveToTarget()
    {
        if (playerDash != null && playerDash.IsDashing) { isMovingToTarget = false; return; }
        if (playerAttack != null && playerAttack.IsCasting) { isMovingToTarget = false; return; }
        if (playerAttack != null && playerAttack.IsAiming) { isMovingToTarget = false; return; }

        float distance = Vector3.Distance(transform.position, clickTarget);

        if (distance < stopDistance)
        {
            isMovingToTarget = false;
            rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
            return;
        }

        Vector3 dir = (clickTarget - transform.position).normalized;
        dir.y = 0f;

        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), rotationSpeed * Time.fixedDeltaTime);
        rb.linearVelocity = new Vector3(dir.x * moveSpeed, rb.linearVelocity.y, dir.z * moveSpeed);
    }

    void OnDrawGizmosSelected()
    {
        if (!isMovingToTarget) return;
        Gizmos.color = Color.cyan;
        Gizmos.DrawSphere(clickTarget, 0.2f);
    }
}