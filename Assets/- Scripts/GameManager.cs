using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("UI")]
    [SerializeField] RectTransform arrowImage;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Start()
    {
        // Default: show DOWN arrow
        UpdateArrowVisual(Vector3.down);
        HideArrow();
    }

    /// <summary>
    /// Rotates arrow so UI arrow faces the closest matching cardinal direction.
    /// Supports any arbitrary direction (from quaternion gravity rotation).
    /// </summary>
    public void UpdateArrowVisual(Vector3 direction)
    {
        if (arrowImage == null) return;

        direction.Normalize();

        // Find the closest axis (same logic you use in SnapToAxis)
        Vector3[] axes =
        {
            Vector3.up, Vector3.down,
            Vector3.right, Vector3.left,
            Vector3.forward, Vector3.back
        };

        float bestDot = -999f;
        Vector3 bestAxis = Vector3.down; // default

        foreach (var axis in axes)
        {
            float d = Vector3.Dot(direction, axis);
            if (d > bestDot)
            {
                bestDot = d;
                bestAxis = axis;
            }
        }

        float angle = 0f;

        // Map cardinals to UI angles (sprite points UP by default)
        if (bestAxis == Vector3.up) angle = 0f;
        else if (bestAxis == Vector3.right) angle = -90f;
        else if (bestAxis == Vector3.down) angle = 180f;
        else if (bestAxis == Vector3.left) angle = 90f;
        else if (bestAxis == Vector3.forward) angle = 0f;   // treat forward as up
        else if (bestAxis == Vector3.back) angle = 180f;    // treat back as down

        arrowImage.localEulerAngles = new Vector3(0, 0, angle);
    }

    public void ShowArrow()
    {
        if (arrowImage) arrowImage.gameObject.SetActive(true);
    }

    public void HideArrow()
    {
        if (arrowImage) arrowImage.gameObject.SetActive(false);
    }
}
