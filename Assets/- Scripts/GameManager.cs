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
        // Default gravity = down → arrow should point down
        UpdateArrowVisual(Vector3.down);
        HideArrow();
    }

    /// <summary>
    /// Rotates arrow so that arrow UP direction matches worldDirection exactly.
    /// Sprite initially faces UP.
    /// </summary>
    public void UpdateArrowVisual(Vector3 worldDirection)
    {
        if (arrowImage == null) return;

        worldDirection.Normalize();

        float angle = 0f;

        // Map world-space direction → angle for UP-facint sprite
        if (worldDirection == Vector3.up) angle = 0f;
        else if (worldDirection == Vector3.right) angle = -90f;
        else if (worldDirection == Vector3.down) angle = 180f;
        else if (worldDirection == Vector3.left) angle = 90f;

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
