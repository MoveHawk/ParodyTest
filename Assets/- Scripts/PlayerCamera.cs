using UnityEngine;

public class PlayerCamera : MonoBehaviour
{
    [SerializeField] Transform target;

    [Header("Camera Controls")]
    [SerializeField] float distance = 5f;
    [SerializeField] float heightOffset = 1.5f;
    [SerializeField] float mouseSensitivity = 200f;
    [SerializeField] float minY = -30f;
    [SerializeField] float maxY = 70f;

    float yaw;
    float pitch;

    // Cached orbit axes
    Vector3 camUp;
    Vector3 camRight;

    void Start()
    {
        camUp = target.up;
        camRight = target.right;
    }

    void LateUpdate()
    {
        if (!target) return;

        // Mouse orbit input
        yaw += Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        pitch -= Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;
        pitch = Mathf.Clamp(pitch, minY, maxY);

        // If gravity shifted (player.up changed), update orbit axes
        if (Vector3.Dot(camUp, target.up) < 0.999f)
        {
            camUp = target.up;
            camRight = target.right;
        }

        // Orbit rotation
        Quaternion horizontalRot = Quaternion.AngleAxis(yaw, camUp);
        Quaternion verticalRot = Quaternion.AngleAxis(pitch, camRight);
        Quaternion finalRot = horizontalRot * verticalRot;

        // Offset
        Vector3 offset = finalRot * (Vector3.back * distance);
        Vector3 height = camUp * heightOffset;

        // Position
        transform.position = target.position + height + offset;

        // Look at player
        transform.rotation = Quaternion.LookRotation(
            (target.position + height) - transform.position,
            camUp
        );
    }
}
