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

    // --------------------------------------------------------------
    // MOVEMENT (WASD only)
    // --------------------------------------------------------------
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

            Vector3 moveDir = (camForward * v + camRight * h).normalized;
            Vector3 desiredForward = Vector3.ProjectOnPlane(moveDir, up).normalized;

            targetRotation = Quaternion.LookRotation(desiredForward, up);

            Vector3 horizVel = Vector3.ProjectOnPlane(moveDir * moveSpeed, up);
            Vector3 vertical = Vector3.Project(rb.linearVelocity, up);
            rb.linearVelocity = horizVel + vertical;
        }
    }

    // --------------------------------------------------------------
    // JUMP / GROUND CHECK
    // --------------------------------------------------------------
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

    // --------------------------------------------------------------
    // ARROW KEYS → LOCAL 90° GRAVITY ROTATION
    // --------------------------------------------------------------
    void HandleGravitySelectionInput()
    {
        bool pressed = false;
        Quaternion rotation = Quaternion.identity;

        // ALWAYS use player's CURRENT orientation
        Vector3 localX = transform.right;     // rotate around X for forward/back tilt
        Vector3 localZ = transform.forward;   // rotate around Z for left/right roll

        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            rotation = Quaternion.AngleAxis(-90f, localX);
            pressed = true;
        }
        else if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            rotation = Quaternion.AngleAxis(+90f, localX);
            pressed = true;
        }
        else if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            rotation = Quaternion.AngleAxis(-90f, localZ);
            pressed = true;
        }
        else if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            rotation = Quaternion.AngleAxis(+90f, localZ);
            pressed = true;
        }

        if (pressed)
        {
            // Rotate gravity relative to player's CURRENT local axes
            pendingGravity = rotation * currentGravity;
            hasPendingGravity = true;

            // Arrow shows new down direction
            GameManager.Instance?.ShowArrow();
            GameManager.Instance?.UpdateArrowVisual(pendingGravity.normalized);
        }

        if (hasPendingGravity && Input.GetKeyDown(KeyCode.Return))
        {
            ApplyNewGravity(pendingGravity);
            hasPendingGravity = false;
            GameManager.Instance?.HideArrow();
        }
    }

    // --------------------------------------------------------------
    // APPLY GRAVITY & SNAP FORWARD USING PLAYER LOCAL AXES
    // --------------------------------------------------------------
    void ApplyNewGravity(Vector3 newGravity)
    {
        currentGravity = newGravity;

        Vector3 newUp = -currentGravity.normalized;

        // Find player's CURRENT forward projected to new up-plane
        Vector3 forwardCandidate = Vector3.ProjectOnPlane(transform.forward, newUp).normalized;

        if (forwardCandidate.sqrMagnitude < 0.01f)
            forwardCandidate = Vector3.ProjectOnPlane(transform.right, newUp).normalized;

        // SNAP using LOCAL axes, not world axes
        forwardCandidate = SnapToLocalAxis(forwardCandidate);

        targetRotation = Quaternion.LookRotation(forwardCandidate, newUp);

        Vector3 horiz = Vector3.ProjectOnPlane(rb.linearVelocity, newUp);
        rb.linearVelocity = horiz;
    }

    // --------------------------------------------------------------
    // SNAP TO NEAREST PLAYER-LOCAL AXIS (NOT WORLD AXES)
    // --------------------------------------------------------------
    Vector3 SnapToLocalAxis(Vector3 dir)
    {
        Vector3[] localAxes =
        {
            transform.forward,
            -transform.forward,
            transform.right,
            -transform.right
        };

        float best = -999f;
        Vector3 bestAxis = transform.forward;

        foreach (var axis in localAxes)
        {
            float d = Vector3.Dot(dir, axis);
            if (d > best)
            {
                best = d;
                bestAxis = axis;
            }
        }

        // Re-project to ensure perfect 90° alignment
        return Vector3.ProjectOnPlane(bestAxis, -currentGravity.normalized).normalized;
    }

    // --------------------------------------------------------------
    // ANIMATIONS
    // --------------------------------------------------------------
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

    // --------------------------------------------------------------
    // GIZMOS
    // --------------------------------------------------------------
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
