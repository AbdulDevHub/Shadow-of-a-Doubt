using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance;

    [Header("Optional - Scene UI (will be found automatically if left empty)")]
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI timerText;

    private int currentScore = 0;
    private float elapsedTime = 0f;
    private bool timerIsRunning = false;

    private readonly HashSet<string> levelNames = new HashSet<string>()
    {
        "Level 1",
        "Level 2",
        "Boss Battle"
    };

    private int level1Score = 0;
    private int level2Score = 0;
    private int bossScore = 0;

    private float level1Time = 0f;
    private float level2Time = 0f;
    private float bossTime = 0f;

    private string lastSceneName = "";

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        lastSceneName = SceneManager.GetActiveScene().name;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void Start()
    {
        TryFindSceneUI();
    }

    private void Update()
    {
        if (!timerIsRunning) return;

        elapsedTime += Time.deltaTime;
        UpdateTimerText();
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Reset UI refs so the new scene lookup happens
        scoreText = null;
        timerText = null;

        // Save last scene results if it was a tracked level
        if (!string.IsNullOrEmpty(lastSceneName) && levelNames.Contains(lastSceneName))
        {
            SaveResultsForScene(lastSceneName);
        }

        // Delay a few frames to allow UI to spawn, then search
        StartCoroutine(DelayedUIFind());

        if (levelNames.Contains(scene.name))
            ResetCurrentLevel();
        else
            timerIsRunning = false;

        lastSceneName = scene.name;
    }

    private IEnumerator DelayedUIFind()
    {
        // Wait 2–3 frames (enough time for UI to enable/instantiate)
        yield return null;
        yield return null;
        yield return null;

        TryFindSceneUI();
    }

    private void TryFindSceneUI()
    {
        // First try direct name-based search if not already found
        if (scoreText == null)
            scoreText = FindTMPInInactive("Score");

        if (timerText == null)
            timerText = FindTMPInInactive("Timer");

        // As fallback, pick TMPs containing 'score' or 'timer'
        if (scoreText == null)
        {
            foreach (var t in FindObjectsOfType<TextMeshProUGUI>(true))
            {
                if (t.name.ToLower().Contains("score"))
                {
                    scoreText = t;
                    break;
                }
            }
        }

        if (timerText == null)
        {
            foreach (var t in FindObjectsOfType<TextMeshProUGUI>(true))
            {
                if (t.name.ToLower().Contains("timer"))
                {
                    timerText = t;
                    break;
                }
            }
        }

        UpdateScoreText();
        UpdateTimerText();
    }

    private TextMeshProUGUI FindTMPInInactive(string name)
    {
        // Search canvases first
        var canvases = FindObjectsOfType<Canvas>(true);
        foreach (var c in canvases)
        {
            var tms = c.GetComponentsInChildren<TextMeshProUGUI>(true);
            foreach (var tmp in tms)
            {
                if (tmp.name == name)
                    return tmp;
            }
        }

        // Then search all TMPs in scene
        foreach (var tmp in FindObjectsOfType<TextMeshProUGUI>(true))
        {
            if (tmp.name == name)
                return tmp;
        }

        return null;
    }

    private void UpdateScoreText()
    {
        if (scoreText != null)
            scoreText.text = currentScore.ToString();
    }

    private void UpdateTimerText()
    {
        if (timerText == null) return;

        int minutes = Mathf.FloorToInt(elapsedTime / 60f);
        int seconds = Mathf.FloorToInt(elapsedTime % 60f);
        timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
    }

    public void AddScore(int amount)
    {
        currentScore += amount;
        UpdateScoreText();
    }

    public void SetScore(int newScore)
    {
        currentScore = newScore;
        UpdateScoreText();
    }

    public int GetScore() => currentScore;

    public void StopTimer() => timerIsRunning = false;
    public void ResumeTimer() => timerIsRunning = true;

    public void ResetCurrentLevel()
    {
        currentScore = 0;
        elapsedTime = 0f;
        timerIsRunning = true;
        UpdateScoreText();
        UpdateTimerText();
    }

    public string GetCurrentTimeFormatted()
    {
        int minutes = Mathf.FloorToInt(elapsedTime / 60f);
        int seconds = Mathf.FloorToInt(elapsedTime % 60f);
        return string.Format("{0:00}:{1:00}", minutes, seconds);
    }

    public void SaveCurrentLevelResults()
    {
        string active = SceneManager.GetActiveScene().name;
        if (levelNames.Contains(active))
            SaveResultsForScene(active);
    }

    private void SaveResultsForScene(string sceneName)
    {
        if (sceneName == "Level 1")
        {
            level1Score = currentScore;
            level1Time = elapsedTime;
        }
        else if (sceneName == "Level 2")
        {
            level2Score = currentScore;
            level2Time = elapsedTime;
        }
        else if (sceneName == "Boss Battle")
        {
            bossScore = currentScore;
            bossTime = elapsedTime;
        }
    }

    public int Level1Score => level1Score;
    public int Level2Score => level2Score;
    public int BossScore => bossScore;

    public float Level1TimeSeconds => level1Time;
    public float Level2TimeSeconds => level2Time;
    public float BossTimeSeconds => bossTime;

    public string GetLevelTimeFormatted(string sceneName)
    {
        float t = 0f;
        if (sceneName == "Level 1") t = level1Time;
        else if (sceneName == "Level 2") t = level2Time;
        else if (sceneName == "Boss Battle") t = bossTime;

        int minutes = Mathf.FloorToInt(t / 60f);
        int seconds = Mathf.FloorToInt(t % 60f);
        return string.Format("{0:00}:{1:00}", minutes, seconds);
    }

    public int GetTotalScore() => level1Score + level2Score + bossScore;
    public float GetTotalTimeSeconds() => level1Time + level2Time + bossTime;

    public string GetTotalTimeFormatted()
    {
        float total = GetTotalTimeSeconds();
        int minutes = Mathf.FloorToInt(total / 60f);
        int seconds = Mathf.FloorToInt(total % 60f);
        return string.Format("{0:00}:{1:00}", minutes, seconds);
    }

    public void ResetLevelTimer()
    {
        elapsedTime = 0f;
        UpdateTimerText();
    }

    public void ResetLevelScore()
    {
        currentScore = 0;
        UpdateScoreText();
    }

    public void ResetAll()
    {
        // Reset current level values
        currentScore = 0;
        elapsedTime = 0f;
        timerIsRunning = false;

        // Reset saved level results
        level1Score = 0;
        level2Score = 0;
        bossScore = 0;

        level1Time = 0f;
        level2Time = 0f;
        bossTime = 0f;

        // Optionally reset lastSceneName
        lastSceneName = "";

        // Clear UI display if needed
        UpdateScoreText();
        UpdateTimerText();
    }
}
