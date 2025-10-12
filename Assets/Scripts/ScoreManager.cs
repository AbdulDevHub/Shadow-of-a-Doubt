using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class ScoreManager : MonoBehaviour
{
    [Header("UI Reference")]
    public TextMeshProUGUI scoreText; // assign in Inspector

    private int currentScore = 0;
    public static ScoreManager Instance;


    private void Awake()
    {
        UpdateScoreText();
        
        // Make this object persist between scene loads
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (Instance != this)
        {
            Destroy(gameObject); // ensure only one instance exists
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Subscribe to scene load events
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Find the TMP text named "score" in this scene
        GameObject textGO = GameObject.Find("Score");
        if (textGO != null)
        {
            scoreText = textGO.GetComponent<TextMeshProUGUI>();
        }
    }


    private void Start()
    {
        UpdateScoreText();
    }

    // Call this whenever you want to add to the score
    public void AddScore(int amount)
    {
        currentScore += amount;
        UpdateScoreText();
    }

    // Optional: set the score directly
    public void SetScore(int newScore)
    {

        currentScore = newScore;
        UpdateScoreText();
    }

    public int GetScore(){


        

        return this.currentScore;
        Debug.Log(this.currentScore);
    }

    private void UpdateScoreText()
    {
        scoreText.text = currentScore.ToString();
    }
}