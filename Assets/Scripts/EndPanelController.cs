using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class EndPanelController : MonoBehaviour
{
    [Header("UI References")]
    public TMP_Text scoreText;
    public TMP_Text timerText;

    public Button quitButton;
    public Button playAgainButton;
    public ScoreManager scoreManager;
    public GameTimer timer;

    void Start()
    {
        // Hook up button functions
        quitButton.onClick.AddListener(QuitGame);
        playAgainButton.onClick.AddListener(PlayAgain);
    }

    public void SetScore()
    {

        if (scoreManager == null)
            scoreManager = FindObjectOfType<ScoreManager>();

       if (timer == null)
            timer = FindObjectOfType<GameTimer>();

        if (scoreManager == null)
            Debug.LogError("EndPanelController: scoreManager is NULL!");
        if (scoreText == null)
            Debug.LogError("EndPanelController: scoreText is NULL!");

        if (scoreText != null && scoreManager != null)
        {
            scoreText.text = "Your Score: " + this.scoreManager.GetScore();
        }

        if (timerText != null && timer != null)
        {
            Debug.Log("entered the text");
            timerText.text = "Your Time: " + this.timer.GetTime();
        }

        this.timer.ResetTimer();
        this.scoreManager.SetScore(0);

    }

    void QuitGame()
    {
        Debug.Log("Quit Game pressed!");
        Application.Quit();

        // NOTE: Application.Quit() won't stop the editor, so:
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    void PlayAgain()
    {
        // Load second scene in Build Settings list
        SceneManager.LoadScene("House"); 
        // or use index: SceneManager.LoadScene(1);
    }
}
