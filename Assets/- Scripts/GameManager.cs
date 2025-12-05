using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Gravity Hologram Previews")]
    public GameObject holoUp;
    public GameObject holoDown;
    public GameObject holoLeft;
    public GameObject holoRight;

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
        DisableAllHolograms();
    }

    public void DisableAllHolograms()
    {
        if (holoUp) holoUp.SetActive(false);
        if (holoDown) holoDown.SetActive(false);
        if (holoLeft) holoLeft.SetActive(false);
        if (holoRight) holoRight.SetActive(false);
    }

    // Call this when choosing a direction
    public void ShowHologram(string name)
    {
        DisableAllHolograms();

        switch (name)
        {
            case "Up": if (holoUp) holoUp.SetActive(true); break;
            case "Down": if (holoDown) holoDown.SetActive(true); break;
            case "Left": if (holoLeft) holoLeft.SetActive(true); break;
            case "Right": if (holoRight) holoRight.SetActive(true); break;
        }
    }
}
