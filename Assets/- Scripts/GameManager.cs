using UnityEngine;
using TMPro;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Gravity Hologram Previews")]
    public GameObject holoUp;
    public GameObject holoDown;
    public GameObject holoLeft;
    public GameObject holoRight;

    [Header("Points System")]
    [SerializeField] TextMeshProUGUI scoreText;
    int totalPoints;
    int collectedPoints = 0;

    [Header("Timer")]
    [SerializeField] TextMeshProUGUI timerText;
    [SerializeField] float timerDuration = 120f;
    float timer;

    [Header("Game Over / Win Canvas")]
    [SerializeField] GameObject resultCanvas;
    [SerializeField] TextMeshProUGUI resultText;

    bool gameEnded = false;

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

        totalPoints = GameObject.FindGameObjectsWithTag("Points").Length;

        timer = timerDuration;
        UpdateScoreText();
        UpdateTimerText();

        if (resultCanvas) resultCanvas.SetActive(false);
    }

    void Update()
    {
        if (gameEnded) return;

        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            timer = 0f;
            TriggerLose();
        }

        UpdateTimerText();
    }

    // Holograms
    public void DisableAllHolograms()
    {
        if (holoUp) holoUp.SetActive(false);
        if (holoDown) holoDown.SetActive(false);
        if (holoLeft) holoLeft.SetActive(false);
        if (holoRight) holoRight.SetActive(false);
    }

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

    // Points
    public void CollectPoint(GameObject pointObj)
    {
        collectedPoints++;
        Destroy(pointObj);
        UpdateScoreText();

        if (collectedPoints >= totalPoints)
            TriggerWin();
    }

    void UpdateScoreText()
    {
        if (scoreText)
            scoreText.text = $"{collectedPoints}/{totalPoints}";
    }

    // Timer
    void UpdateTimerText()
    {
        if (!timerText) return;

        int minutes = Mathf.FloorToInt(timer / 60);
        int seconds = Mathf.FloorToInt(timer % 60);

        timerText.text = $"{minutes:00}:{seconds:00}";
    }

    // WIN
    public void TriggerWin()
    {
        if (gameEnded) return;
        gameEnded = true;

        DisableAllHolograms();

        if (resultText) resultText.text = "You Win!";
        if (resultCanvas) resultCanvas.SetActive(true);
    }

    // LOSE
    public void TriggerLose()
    {
        if (gameEnded) return;
        gameEnded = true;

        DisableAllHolograms();

        if (resultText) resultText.text = "Game Over!";
        if (resultCanvas) resultCanvas.SetActive(true);
    }
}
