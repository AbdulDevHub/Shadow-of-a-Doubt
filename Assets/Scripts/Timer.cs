using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class GameTimer : MonoBehaviour
{
    [Header("UI Reference")]
    public TextMeshProUGUI timerText;  // Assign your TMP Text object here

    [Header("Timer Settings")]
    public float elapsedTime = 0f;     // Time starts at 0
    public bool timerIsRunning = false;

    public static GameTimer Instance;


    private void Awake()
    {
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
        // Find the TMP text named "Time" in this scene
        GameObject textGO = GameObject.Find("timer");
        if (textGO != null)
        {
            timerText = textGO.GetComponent<TextMeshProUGUI>();
        }
        else
        {
            Debug.LogWarning("TimerManager: Could not find TextMeshProUGUI named 'Time' in scene " + scene.name);
        }

        // Automatically pause if the scene is "Ending"
        if (scene.name == "Ending")
            timerIsRunning = false;
        else
            timerIsRunning = true; // resume in other scenes
    }


    private void Start()
    {
        if (timerText == null)
        {
            Debug.LogError("GameTimer: Timer Text not assigned!");
            return;
        }

        timerIsRunning = true;  // Start counting immediately
        UpdateTimerText();
    }

    private void Update()
    {
        if (!timerIsRunning) return;

        // Increase elapsed time
        elapsedTime += Time.deltaTime;

        UpdateTimerText();
    }

    private void UpdateTimerText()
    {
        int minutes = Mathf.FloorToInt(elapsedTime / 60);
        int seconds = Mathf.FloorToInt(elapsedTime % 60);
        timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
    }

    // Optional: Stop the timer
    public void StopTimer()
    {
        timerIsRunning = false;
    }

    public void ResumeTimer(){
        timerIsRunning = true;

    }

    // Optional: Reset timer
    public void ResetTimer()
    {
        elapsedTime = 0f;
        UpdateTimerText();
    }

    public string GetTime(){
        int minutes = Mathf.FloorToInt(elapsedTime / 60);
        int seconds = Mathf.FloorToInt(elapsedTime % 60);
        return string.Format("{0:00}:{1:00}", minutes, seconds);
    }
}
