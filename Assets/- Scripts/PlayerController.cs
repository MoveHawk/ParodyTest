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

    [Header("Ground Raycast")]
    [SerializeField] LayerMask groundLayer;
    [SerializeField] float rayDistance = 5f;

    [Header("Rotation")]
    [SerializeField] float rotationLerpSpeed = 8f;

    Rigidbody rb;
    Transform cam;

    Vector3 currentGravity;    // gravity vector pointing toward ground (e.g. Vector3.down * gravityStrength)
    Quaternion targetRotation;

    bool isGrounded;

    // ---- gravity selection state (arrow keys choose a direction, Enter confirms) ----
    bool hasPendingGravitySelection = false;
    Vector3 pendingLocalAxis = Vector3.zero; // stored as local axis (e.g., Vector3.right or Vector3.forward)
    float pendingAngle = 0f;
    Vector3 pendingPreviewCandidate = Vector3.zero; // preview candidate computed at selection time

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;

        if (!animator) animator = GetComponent<Animator>();
        cam = Camera.main ? Camera.main.transform : null;

        // default gravity points world down
        currentGravity = Vector3.down * gravityStrength;
        targetRotation = transform.rotation;

    }

    void Update()
    {
        HandleInput();
        HandleMovement();
        HandleJump();
        HandleAnimations();
    }

    void FixedUpdate()
    {
        // Apply custom gravity as acceleration
        rb.AddForce(currentGravity, ForceMode.Acceleration);

        // Smoothly interpolate rotation toward targetRotation
        Quaternion newRot = Quaternion.Slerp(rb.rotation, targetRotation, rotationLerpSpeed * Time.fixedDeltaTime);
        rb.MoveRotation(newRot);
    }

    // -----------------------
    // Input for gravity rotate (selection + confirmation)
    // -----------------------
    void HandleInput()
    {
        // Selection with arrow keys (do NOT immediately rotate or apply gravity)
        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            GameManager.Instance?.ShowHologram("Up");
            SetPendingGravitySelection(Vector3.right, -90f);
        }
        else if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            GameManager.Instance?.ShowHologram("Down");
            SetPendingGravitySelection(Vector3.right, +90f);
        }
        else if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            GameManager.Instance?.ShowHologram("Left");
            SetPendingGravitySelection(Vector3.forward, -90f);
        }
        else if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            GameManager.Instance?.ShowHologram("Right");
            SetPendingGravitySelection(Vector3.forward, +90f);
        }

        // Confirm selection with Enter
        if ((Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)) && hasPendingGravitySelection)
        {
            // Recompute candidate using current transform orientation so confirmation uses current facing
            GameManager.Instance?.DisableAllHolograms();
            Vector3 confirmCandidate = ComputeGravityCandidate(pendingLocalAxis, pendingAngle);
            ApplyNewGravityWithRaycast(confirmCandidate);
            hasPendingGravitySelection = false;
            pendingPreviewCandidate = Vector3.zero;
        }

        // Cancel selection with Escape
        if (Input.GetKeyDown(KeyCode.Escape) && hasPendingGravitySelection)
        {
            hasPendingGravitySelection = false;
            pendingPreviewCandidate = Vector3.zero;
         
        }
    }

    // Record a pending selection and show a preview (doesn't actually change gravity)
    void SetPendingGravitySelection(Vector3 localAxis, float angleDegrees)
    {
        pendingLocalAxis = localAxis.normalized;
        pendingAngle = angleDegrees;
        // compute preview based on current transform orientation
        pendingPreviewCandidate = ComputeGravityCandidate(pendingLocalAxis, pendingAngle);
        hasPendingGravitySelection = true;

    }

    // Compute candidate gravity vector (does not apply it)
    Vector3 ComputeGravityCandidate(Vector3 localAxis, float angleDegrees)
    {
        Vector3 worldAxis = transform.TransformDirection(localAxis.normalized);
        Quaternion rot = Quaternion.AngleAxis(angleDegrees, worldAxis);
        Vector3 newGravityCandidate = rot * currentGravity;
        return newGravityCandidate;
    }

    // Rotate the current gravity vector around a local axis by angleDegrees (kept for compatibility; uses ApplyNewGravityWithRaycast)
    void RotateGravityAroundLocalAxis(Vector3 localAxis, float angleDegrees)
    {
        Vector3 newGravityCandidate = ComputeGravityCandidate(localAxis, angleDegrees);
        ApplyNewGravityWithRaycast(newGravityCandidate);
    }

    // -----------------------
    // Movement
    // -----------------------
    void HandleMovement()
    {
        // Using axis input so camera-relative movement still works with controllers/keys
        float h = Input.GetAxisRaw("Horizontal"); // A/D or left/right (note: arrow keys may also be mapped here)
        float v = Input.GetAxisRaw("Vertical");   // W/S or up/down

        // Prevent arrow keys from affecting movement: if the player is pressing arrow keys to select gravity,
        // zero out movement input for that frame so arrows are reserved exclusively for gravity selection.
        if (Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.RightArrow))
        {
            h = 0f;
            v = 0f;
        }

        Vector3 input = new Vector3(h, 0f, v);
        if (input.sqrMagnitude < 0.01f) return;

        // Define "up" relative to gravity (player up is opposite gravity direction)
        Vector3 up = -currentGravity.normalized;

        // Project camera forward/right onto plane perpendicular to current up to get movement directions
        Vector3 camForward = cam ? Vector3.ProjectOnPlane(cam.forward, up).normalized : Vector3.ProjectOnPlane(transform.forward, up).normalized;
        Vector3 camRight = cam ? Vector3.ProjectOnPlane(cam.right, up).normalized : Vector3.ProjectOnPlane(transform.right, up).normalized;

        Vector3 moveDir = (camForward * v + camRight * h).normalized;

        // Determine desired facing (projected onto plane)
        Vector3 desiredForward = Vector3.ProjectOnPlane(moveDir, up);
        if (desiredForward.sqrMagnitude < 0.001f)
            desiredForward = Vector3.ProjectOnPlane(transform.forward, up);

        targetRotation = Quaternion.LookRotation(desiredForward.normalized, up);

        // Set horizontal velocity while preserving vertical velocity along up
        Vector3 horizVel = Vector3.ProjectOnPlane(moveDir * moveSpeed, up);
        Vector3 verticalVel = Vector3.Project(rb.linearVelocity, up);
        rb.linearVelocity = horizVel + verticalVel;
    }

    // -----------------------
    // Jump & Ground Check
    // -----------------------
    void HandleJump()
    {
        Vector3 gravityDir = currentGravity.normalized;   // points toward ground
        Vector3 rayOrigin = transform.position + (-gravityDir) * 0.1f;
        float checkDistance = 1.2f;

        isGrounded = Physics.Raycast(rayOrigin, gravityDir, checkDistance, groundLayer);

        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
        {
            Vector3 upDir = -gravityDir;
            Vector3 horizontal = Vector3.ProjectOnPlane(rb.linearVelocity, upDir);
            rb.linearVelocity = horizontal; // remove vertical component before impulse
            rb.AddForce(upDir * jumpForce, ForceMode.Impulse);
        }
    }

    // -----------------------
    // Apply gravity candidate (with raycast alignment)
    // -----------------------
    void ApplyNewGravityWithRaycast(Vector3 newGravityCandidate)
    {
        // Capture old up BEFORE we change gravity so we can preserve heading.
        Vector3 oldUp = -currentGravity.normalized;

        Vector3 gravityDir = newGravityCandidate.normalized; // points toward ground
        Vector3 rayOrigin = transform.position + (-gravityDir) * 0.2f;
        Vector3 rayDir = gravityDir;

        RaycastHit hit;
        Vector3 chosenUp;

        if (Physics.Raycast(rayOrigin, rayDir, out hit, rayDistance, groundLayer))
        {
            // Align player's up to surface normal so player stands flush on that surface
            chosenUp = hit.normal.normalized;
        }
        else
        {
            // No ground found in that direction: fallback to opposite of gravity direction
            chosenUp = -gravityDir;
        }

        // Set the actual gravity to point toward ground (down = -up)
        currentGravity = -chosenUp * gravityStrength;

        // ---- Preserve the player's existing heading (yaw) across the up-change ----
        // 1) get a stable heading vector in the old-up plane
        Vector3 oldHeading = Vector3.ProjectOnPlane(transform.forward, oldUp);
        if (oldHeading.sqrMagnitude < 0.0001f)
            oldHeading = Vector3.ProjectOnPlane(transform.right, oldUp);
        if (oldHeading.sqrMagnitude < 0.0001f)
            oldHeading = Vector3.ProjectOnPlane(Vector3.forward, oldUp);
        oldHeading.Normalize();

        // 2) rotate that heading from old-up frame into the new-up frame
        Quaternion rotUp = Quaternion.FromToRotation(oldUp, chosenUp);
        Vector3 mappedHeading = rotUp * oldHeading;

        // 3) ensure mappedHeading lies on the chosenUp plane and is valid
        Vector3 forwardCandidate = Vector3.ProjectOnPlane(mappedHeading, chosenUp);
        if (forwardCandidate.sqrMagnitude < 0.0001f)
        {
            // fallback to projected transform.forward or camera forward onto new plane
            forwardCandidate = Vector3.ProjectOnPlane(transform.forward, chosenUp);
            if (forwardCandidate.sqrMagnitude < 0.0001f && cam != null)
                forwardCandidate = Vector3.ProjectOnPlane(cam.forward, chosenUp);
        }

        if (forwardCandidate.sqrMagnitude < 0.0001f)
        {
            // last resort: use arbitrary orthogonal direction
            forwardCandidate = Vector3.Cross(chosenUp, transform.up);
            if (forwardCandidate.sqrMagnitude < 0.0001f)
                forwardCandidate = Vector3.ProjectOnPlane(Vector3.forward, chosenUp);
        }

        forwardCandidate.Normalize();

        // IMPORTANT: Do NOT snap this forwardCandidate to local axes here — we must preserve heading.
        // Set target rotation so the player stands perpendicular to the new up while preserving heading (yaw)
        targetRotation = Quaternion.LookRotation(forwardCandidate, chosenUp);

        // Remove vertical component of velocity relative to new up so player doesn't get launched
        Vector3 horiz = Vector3.ProjectOnPlane(rb.linearVelocity, chosenUp);
        rb.linearVelocity = horiz;

    }

    // Snap a direction to the nearest of the 4 local horizontal axes projected onto the chosenUp plane.
    Vector3 SnapToLocalAxisOnPlane(Vector3 dirOnPlane, Vector3 up)
    {
        dirOnPlane.Normalize();

        Vector3[] localAxes =
        {
            Vector3.ProjectOnPlane(transform.forward, up).normalized,
            Vector3.ProjectOnPlane(-transform.forward, up).normalized,
            Vector3.ProjectOnPlane(transform.right, up).normalized,
            Vector3.ProjectOnPlane(-transform.right, up).normalized
        };

        float best = -Mathf.Infinity;
        Vector3 bestAxis = dirOnPlane;
        foreach (var axis in localAxes)
        {
            if (axis.sqrMagnitude < 0.0001f) continue;
            float d = Vector3.Dot(dirOnPlane, axis.normalized);
            if (d > best)
            {
                best = d;
                bestAxis = axis.normalized;
            }
        }

        return bestAxis;
    }

    // -----------------------
    // Animations
    // -----------------------
    void HandleAnimations()
    {
        if (!animator) return;

        Vector3 up = -currentGravity.normalized;
        Vector3 horizontal = Vector3.ProjectOnPlane(rb.linearVelocity, up);

        bool running = horizontal.magnitude > 0.2f;
        bool falling = !isGrounded;

        animator.SetBool("isRunning", running && !falling);
        animator.SetBool("isFalling", falling);
        animator.SetBool("isIdle", !running && isGrounded);
    }

    void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;

        Vector3 origin = transform.position;
        // Current gravity (red)
        Gizmos.color = Color.red;
        Gizmos.DrawLine(origin, origin + currentGravity.normalized * 2f);

        // Preview candidate gravity (yellow) when there is a pending selection
        if (hasPendingGravitySelection && pendingPreviewCandidate != Vector3.zero)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(origin, origin + pendingPreviewCandidate.normalized * 2f);
        }
    }
}