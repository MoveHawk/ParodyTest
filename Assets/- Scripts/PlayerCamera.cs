using UnityEngine;

public class PlayerCamera : MonoBehaviour
{
    [SerializeField] Transform target;

    [SerializeField] float distance = 5f;
    [SerializeField] float mouseSensitivity = 200f;
    [SerializeField] float minY = -30f;
    [SerializeField] float maxY = 70f;

    float yaw;
    float pitch;

    void LateUpdate()
    {
        if (!target) return;

        yaw += Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        pitch -= Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;
        pitch = Mathf.Clamp(pitch, minY, maxY);

        // Build rotation in local camera space, then transform to world space with target's up as 'up'
        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0);

        // offset is relative to camera local axes; then we want it oriented in world using target.up
        Vector3 offset = rotation * new Vector3(0, 0, -distance);

        // Position camera relative to target
        transform.position = target.position + offset;

        // Look at the target but use target.up (so camera's up is player's up)
        transform.rotation = Quaternion.LookRotation((target.position + target.up * 1.5f) - transform.position, target.up);
    }
}
