using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class EndPanelController : MonoBehaviour
{
    [Header("UI References")]
    public TMP_Text scoreText;
    public Button quitButton;
    public Button playAgainButton;

    void Start()
    {
        // Hook up button functions
        quitButton.onClick.AddListener(QuitGame);
        playAgainButton.onClick.AddListener(PlayAgain);
    }

    public void SetScore(int score)
    {
        if (scoreText != null)
        {
            scoreText.text = "Your Score: " + score;
        }
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
