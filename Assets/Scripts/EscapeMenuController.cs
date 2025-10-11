using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using UnityEngine.InputSystem;
using TMPro; // for TextMeshProUGUI

public class EscapeMenuController : MonoBehaviour
{
    [Header("UI References")]
    public Image fadePanel;           
    public GameObject escapeMenuUI;   

    [Header("Settings")]
    public float fadeDuration = 0.5f;
    [Range(0f, 1f)] public float targetAlpha = 0.7f;

    private bool isMenuOpen = false;
    private Coroutine fadeCoroutine;

    [Header("Gameplay References")]
    public PlayerInput playerInput; // Drag your PlayerInput here in Inspector

    [Header("Scene Settings")]
    public string nextSceneName; // set this in inspector for "Skip Level"

    [Header("UI Buttons")]
    public Button resumeButton;
    public Button restartButton;
    public Button skipButton;
    public Button quitButton;
    public Button difficultyButton;

    void Start()
    {
        SetImageAlpha(0f);
        fadePanel.gameObject.SetActive(false);
        escapeMenuUI.SetActive(false);

        // Hook up button events
        if (resumeButton != null) resumeButton.onClick.AddListener(ResumeGame);
        if (restartButton != null) restartButton.onClick.AddListener(RestartLevel);
        if (skipButton != null) skipButton.onClick.AddListener(SkipLevel);
        if (quitButton != null) quitButton.onClick.AddListener(QuitGame);
        if (difficultyButton != null) difficultyButton.onClick.AddListener(ChangeDifficulty);

        UpdateDifficultyButtonText();
    }

    void Update()
    {
        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (isMenuOpen)
                CloseMenu();
            else
                OpenMenu();
        }
    }

    void OpenMenu()
    {
        // gameTimer = FindObjectOfType<Timer>();
        // gameTimer.StopTimer();

        isMenuOpen = true;
        escapeMenuUI.SetActive(true);
        fadePanel.gameObject.SetActive(true);

        Time.timeScale = 0f;
        if (playerInput != null)
            playerInput.enabled = false;

        // Show & unlock mouse
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        fadeCoroutine = StartCoroutine(FadeImage(fadePanel, fadePanel.color.a, targetAlpha));
    }

    void CloseMenu()
    {
        // gameTimer = FindObjectOfType<Timer>();
        // gameTimer.StopTimer();        

        isMenuOpen = false;
        Time.timeScale = 1f;
        if (playerInput != null)
            playerInput.enabled = true;

        // Hide & lock mouse
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        fadeCoroutine = StartCoroutine(FadeOutAndDisable());
    }

    IEnumerator FadeOutAndDisable()
    {
        yield return StartCoroutine(FadeImage(fadePanel, fadePanel.color.a, 0f));
        fadePanel.gameObject.SetActive(false);
        escapeMenuUI.SetActive(false);
    }

    IEnumerator FadeImage(Image img, float start, float end)
    {
        float time = 0f;
        while (time < fadeDuration)
        {
            time += Time.unscaledDeltaTime;
            float alpha = Mathf.Lerp(start, end, time / fadeDuration);
            SetImageAlpha(alpha);
            yield return null;
        }
        SetImageAlpha(end);
    }

    void SetImageAlpha(float alpha)
    {
        Color c = fadePanel.color;
        c.a = alpha;
        fadePanel.color = c;
    }

    // === BUTTON FUNCTIONS ===

    public void ResumeGame()
    {
        CloseMenu();
    }

    public void RestartLevel()
    {
        Time.timeScale = 1f; 
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        Scene current = SceneManager.GetActiveScene();
        SceneManager.LoadScene(current.name);
    }

    public void SkipLevel()
    {
        if (!string.IsNullOrEmpty(nextSceneName))
        {
            Time.timeScale = 1f;
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;

            SceneManager.LoadScene(nextSceneName);
        }
        else
        {
            Debug.LogWarning("Next Scene Name not set in EscapeMenuController!");
        }
    }

    public void QuitGame()
    {
        Time.timeScale = 1f;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    public void ChangeDifficulty()
    {
        DifficultyManager.Instance.CycleDifficulty();
        UpdateDifficultyButtonText();
        Debug.Log("Difficulty changed to: " + DifficultyManager.Instance.CurrentDifficulty);
    }

    private void UpdateDifficultyButtonText()
    {
        if (difficultyButton != null)
        {
            TextMeshProUGUI btnText = difficultyButton.GetComponentInChildren<TextMeshProUGUI>();
            if (btnText != null)
                btnText.text = "Difficulty: " + DifficultyManager.Instance.CurrentDifficulty;
        }
    }
}
