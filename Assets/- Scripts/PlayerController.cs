using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] float moveSpeed = 6f;
    [SerializeField] float rotationSpeed = 10f;
    [SerializeField] float jumpForce = 8f;
    [SerializeField] float gravity = -25f;

    Rigidbody rb;
    Animator animator;
    Transform cam;

    bool isGrounded;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;

        animator = GetComponent<Animator>();
        cam = Camera.main.transform;
    }

    void Update()
    {
        HandleMovement();
        HandleJump();
        HandleAnimations();
    }

    void HandleMovement()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        Vector3 input = new Vector3(h, 0, v).normalized;

        if (input.magnitude >= 0.1f)
        {
            Vector3 camForward = cam.forward; camForward.y = 0; camForward.Normalize();
            Vector3 camRight = cam.right; camRight.y = 0; camRight.Normalize();

            Vector3 moveDir = camForward * v + camRight * h;

            Quaternion targetRot = Quaternion.LookRotation(moveDir);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSpeed * Time.deltaTime);

            Vector3 vel = moveDir * moveSpeed;
            vel.y = rb.linearVelocity.y;
            rb.linearVelocity = vel;
        }
        else
        {
            rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, 0);
        }
    }

    void HandleJump()
    {
        isGrounded = Physics.Raycast(transform.position + Vector3.up * 0.1f, Vector3.down, 1.2f);

        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
        {
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        }

        if (!isGrounded)
            rb.AddForce(Vector3.up * gravity, ForceMode.Acceleration);
    }

    void HandleAnimations()
    {
        Vector3 horizontalVel = rb.linearVelocity;
        horizontalVel.y = 0;

        bool running = horizontalVel.magnitude > 0.2f;
        bool falling = !isGrounded;

        animator.SetBool("isRunning", running && !falling);
        animator.SetBool("isFalling", falling);
        animator.SetBool("isIdle", !running && isGrounded);
    }
}
