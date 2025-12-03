using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] float moveSpeed = 6f;
    [SerializeField] float rotationSpeed = 10f;
    [SerializeField] float jumpForce = 8f;
    [SerializeField] float gravityStrength = 25f;

    [Header("Animator")]
    [SerializeField] Animator animator;

    Rigidbody rb;
    Transform cam;

    bool isGrounded;

    Vector3 currentGravity;
    Vector3 pendingGravity;
    bool hasPendingGravity;

    Quaternion targetRotation;
    float rotationLerpSpeed = 8f;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;

        if (!animator) animator = GetComponent<Animator>();
        cam = Camera.main.transform;

        currentGravity = Vector3.down * gravityStrength;
        targetRotation = transform.rotation;

        GameManager.Instance?.UpdateArrowVisual(Vector3.down);
    }

    void Update()
    {
        HandleMovement();
        HandleJump();
        HandleGravitySelectionInput();
        HandleAnimations();
    }

    void FixedUpdate()
    {
        rb.AddForce(currentGravity, ForceMode.Acceleration);

        Quaternion newRot = Quaternion.Slerp(rb.rotation, targetRotation, rotationLerpSpeed * Time.fixedDeltaTime);
        rb.MoveRotation(newRot);
    }

    // ------------------ MOVEMENT (WASD only) -----------------------
    void HandleMovement()
    {
        float h = (Input.GetKey(KeyCode.A) ? -1 : 0) + (Input.GetKey(KeyCode.D) ? 1 : 0);
        float v = (Input.GetKey(KeyCode.W) ? 1 : 0) + (Input.GetKey(KeyCode.S) ? -1 : 0);

        Vector3 input = new Vector3(h, v).normalized;

        if (input.magnitude >= 0.1f)
        {
            Vector3 up = -currentGravity.normalized;

            Vector3 camForward = Vector3.ProjectOnPlane(cam.forward, up).normalized;
            Vector3 camRight = Vector3.ProjectOnPlane(cam.right, up).normalized;

            Vector3 moveDir = camForward * v + camRight * h;
            moveDir.Normalize();

            Vector3 desiredForward = Vector3.ProjectOnPlane(moveDir, up).normalized;

            targetRotation = Quaternion.LookRotation(desiredForward, up);

            Vector3 horizVel = Vector3.ProjectOnPlane(moveDir * moveSpeed, up);
            Vector3 vertical = Vector3.Project(rb.linearVelocity, up);
            rb.linearVelocity = horizVel + vertical;
        }
    }

    // ------------------ JUMP / GROUND CHECK ------------------------
    void HandleJump()
    {
        Vector3 downDir = currentGravity.normalized;
        Vector3 origin = transform.position + (-downDir) * 0.1f;

        isGrounded = Physics.Raycast(origin, downDir, 1.2f);

        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
        {
            Vector3 upDir = -downDir;
            Vector3 horizontal = Vector3.ProjectOnPlane(rb.linearVelocity, upDir);

            rb.linearVelocity = horizontal;
            rb.AddForce(upDir * jumpForce, ForceMode.Impulse);
        }
    }

    // ------------------ ARROW KEYS SELECT GRAVITY -------------------
    void HandleGravitySelectionInput()
    {
        bool pressed = false;
        Vector3 candidate = Vector3.zero;

        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            candidate = Vector3.up;
            pressed = true;
        }
        else if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            candidate = Vector3.down;
            pressed = true;
        }
        else if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            candidate = Vector3.left;
            pressed = true;
        }
        else if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            candidate = Vector3.right;
            pressed = true;
        }

        if (pressed)
        {
            pendingGravity = candidate * gravityStrength;
            hasPendingGravity = true;

            GameManager.Instance?.ShowArrow();
            GameManager.Instance?.UpdateArrowVisual(candidate);
        }

        if (hasPendingGravity && Input.GetKeyDown(KeyCode.Return))
        {
            ApplyNewGravity(pendingGravity);
            hasPendingGravity = false;

            GameManager.Instance?.HideArrow();
        }
    }

    // ------------------ APPLY NEW GRAVITY ---------------------------
    void ApplyNewGravity(Vector3 newGravity)
    {
        currentGravity = newGravity;

        Vector3 newUp = -currentGravity.normalized;

        // Snap forward to nearest cardinal direction
        Vector3[] cardinals = {
            Vector3.forward, Vector3.back, Vector3.right, Vector3.left
        };

        float best = -999f;
        Vector3 bestDir = Vector3.forward;

        foreach (var c in cardinals)
        {
            float d = Vector3.Dot(transform.forward, c);
            if (d > best)
            {
                best = d;
                bestDir = c;
            }
        }

        Vector3 forwardCorrected = Vector3.ProjectOnPlane(bestDir, newUp).normalized;

        targetRotation = Quaternion.LookRotation(forwardCorrected, newUp);

        Vector3 horiz = Vector3.ProjectOnPlane(rb.linearVelocity, newUp);
        rb.linearVelocity = horiz;
    }

    // ------------------ ANIMATIONS -------------------------------
    void HandleAnimations()
    {
        Vector3 up = -currentGravity.normalized;

        Vector3 horizontal = Vector3.ProjectOnPlane(rb.linearVelocity, up);

        bool running = horizontal.magnitude > 0.2f;
        bool falling = !isGrounded;

        animator.SetBool("isRunning", running && !falling);
        animator.SetBool("isFalling", falling);
        animator.SetBool("isIdle", !running && isGrounded);
    }

    // ------------------ GIZMOS -------------------------------
    void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;

        Vector3 origin = transform.position;

        Gizmos.color = Color.red;
        Gizmos.DrawLine(origin, origin + currentGravity.normalized * 2f);

        if (hasPendingGravity)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(origin, origin + pendingGravity.normalized * 2f);
        }
    }
}
