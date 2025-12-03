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

        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0);
        Vector3 offset = rotation * new Vector3(0, 0, -distance);

        transform.position = target.position + offset;
        transform.LookAt(target.position + Vector3.up * 1.5f);
    }
}

