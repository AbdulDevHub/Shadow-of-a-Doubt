using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class EndPanelController : MonoBehaviour
{
    [Header("Per-level score text (assign in inspector)")]
    public TMP_Text level1ScoreText;
    public TMP_Text level2ScoreText;
    public TMP_Text bossScoreText;

    [Header("Per-level time text (assign in inspector)")]
    public TMP_Text level1TimeText;
    public TMP_Text level2TimeText;
    public TMP_Text bossTimeText;

    [Header("Button Sounds")]
    [SerializeField] private AudioClip clickSound;  // Assign in inspector

    [Header("Totals (assign in inspector)")]
    public TMP_Text totalScoreText;
    public TMP_Text totalTimeText;

    [Header("Buttons")]
    public Button quitButton;
    public Button mainMenuButton;

    private ScoreManager scoreManager;

    void Start()
    {
        if (quitButton != null)
            quitButton.onClick.AddListener(QuitGame);
        if (mainMenuButton != null)
            mainMenuButton.onClick.AddListener(ReturnToMainMenu);
    }

    // Called by your DialogueSequence when showing the end panel
    public void SetScore()
    {
        if (ScoreManager.Instance == null)
        {
            Debug.LogError("EndPanelController: ScoreManager.Instance is NULL!");
            return;
        }

        scoreManager = ScoreManager.Instance;

        // ✅ Stop the live timer so it doesn't overwrite the TMP
        scoreManager.StopTimer();

        // Ensure latest results are captured
        scoreManager.SaveCurrentLevelResults();

        // Scores with labels
        if (level1ScoreText != null)
            level1ScoreText.text = $"Level 1 Score: {scoreManager.Level1Score}";
        if (level2ScoreText != null)
            level2ScoreText.text = $"Level 2 Score: {scoreManager.Level2Score}";
        if (bossScoreText != null)
            bossScoreText.text = $"Boss Score: {scoreManager.BossScore}";

        // Times with labels
        if (level1TimeText != null)
            level1TimeText.text = $"Level 1 Time: {scoreManager.GetLevelTimeFormatted("Level 1")}";
        if (level2TimeText != null)
            level2TimeText.text = $"Level 2 Time: {scoreManager.GetLevelTimeFormatted("Level 2")}";
        if (bossTimeText != null)
            bossTimeText.text = $"Boss Time: {scoreManager.GetLevelTimeFormatted("Boss Battle")}";

        // Totals with context
        if (totalScoreText != null)
            totalScoreText.text = $"Total Score: {scoreManager.GetTotalScore()}";

        if (totalTimeText != null)
        {
            float totalSeconds =
                scoreManager.Level1TimeSeconds +
                scoreManager.Level2TimeSeconds +
                scoreManager.BossTimeSeconds;

            int minutes = Mathf.FloorToInt(totalSeconds / 60f);
            int seconds = Mathf.FloorToInt(totalSeconds % 60f);

            totalTimeText.text = $"Total Time: {minutes:00}:{seconds:00}";
        }
    }

    void QuitGame()
    {
        if (clickSound != null)
            AudioSource.PlayClipAtPoint(clickSound, Vector3.zero);

        Debug.Log("Quit Game pressed!");
        Application.Quit();

    #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
    #endif
    }

    void ReturnToMainMenu()
    {
        if (clickSound != null)
            AudioSource.PlayClipAtPoint(clickSound, Vector3.zero);

        // Reset ScoreManager if needed
        if (ScoreManager.Instance != null)
            ScoreManager.Instance.ResetAll(); // Make this method in your manager if it doesn't exist

        UnityEngine.SceneManagement.SceneManager.LoadScene("Main Menu");
    }
}
