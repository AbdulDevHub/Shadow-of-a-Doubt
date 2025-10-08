using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;
using UnityEngine.InputSystem;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health Settings")]
    public float maxHealth = 50f;
    private float currentHealth;

    [Header("UI")]
    [SerializeField] private Slider healthBar;
    public Slider HealthBar { get => healthBar; set => healthBar = value; }

    [Header("Game Over UI References")]
    public Image fadePanel;
    public GameObject gameOverUI;

    [Header("Fade Settings")]
    public float fadeDuration = 0.5f;
    [Range(0f, 1f)] public float targetAlpha = 0.7f;
    [Tooltip("Check if DialogueSequence or another script handles the initial fade.")]
    public bool otherScriptHandlesFade = true;

    private Coroutine fadeCoroutine;
    private bool isDead = false;
    public bool isHealthLocked = false;

    [Header("Gameplay References")]
    public PlayerInput playerInput;

    [Header("Buttons")]
    public Button respawnButton;
    public Button restartButton;
    public Button skipButton;
    public Button quitButton;
    public Button difficultyButton;

    [Header("Scene Settings")]
    public string nextSceneName;

    private void Start()
    {
        currentHealth = maxHealth;
        UpdateHealthUI();

        if (gameOverUI != null)
            gameOverUI.SetActive(false);

        // Only initialize fade panel if no other script is handling it
        if (!otherScriptHandlesFade && fadePanel != null)
        {
            SetImageAlpha(0f);
            fadePanel.gameObject.SetActive(false);
        }

        // Hook up buttons
        if (respawnButton != null) respawnButton.onClick.AddListener(Respawn);
        if (restartButton != null) restartButton.onClick.AddListener(RestartLevel);
        if (skipButton != null) skipButton.onClick.AddListener(SkipLevel);
        if (quitButton != null) quitButton.onClick.AddListener(QuitGame);
        if (difficultyButton != null) difficultyButton.onClick.AddListener(ChangeDifficulty);

        UpdateDifficultyButtonText();
    }

    public void TakeDamage(float amount)
    {
        if (isDead || isHealthLocked) return;

        currentHealth = Mathf.Max(0, currentHealth - amount);
        UpdateHealthUI();

        if (currentHealth <= 0)
            Die();
    }

    public void Heal(float amount)
    {
        if (isDead) return;

        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        UpdateHealthUI();
    }

    private void UpdateHealthUI()
    {
        if (healthBar != null)
            healthBar.value = currentHealth / maxHealth;
    }

    private void Die()
    {
        if (isDead) return;
        isDead = true;

        Debug.Log("Player has died!");
        ShowGameOverUI();

        Time.timeScale = 0f;

        if (playerInput != null)
            playerInput.enabled = false;

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    private void ShowGameOverUI()
    {
        if (fadePanel != null)
        {
            fadePanel.gameObject.SetActive(true);
            if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
            fadeCoroutine = StartCoroutine(FadeImage(fadePanel, fadePanel.color.a, targetAlpha));
        }

        if (gameOverUI != null)
            gameOverUI.SetActive(true);
    }

    private void HideGameOverUI()
    {
        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        fadeCoroutine = StartCoroutine(FadeOutAndDisable());
    }

    private IEnumerator FadeOutAndDisable()
    {
        yield return StartCoroutine(FadeImage(fadePanel, fadePanel.color.a, 0f));
        fadePanel.gameObject.SetActive(false);
        gameOverUI.SetActive(false);
    }

    private IEnumerator FadeImage(Image img, float start, float end)
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

    private void SetImageAlpha(float alpha)
    {
        if (fadePanel == null) return;
        Color c = fadePanel.color;
        c.a = alpha;
        fadePanel.color = c;
    }

    // === BUTTON FUNCTIONS ===

    public void Respawn()
    {
        if (!isDead) return;

        isDead = false;
        currentHealth = maxHealth;
        UpdateHealthUI();

        HideGameOverUI();

        Time.timeScale = 1f;

        if (playerInput != null)
            playerInput.enabled = true;

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        Debug.Log("Player respawned with full health.");
    }

    public void RestartLevel()
    {
        Time.timeScale = 1f;

        if (playerInput != null)
            playerInput.enabled = true;

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

            if (playerInput != null)
                playerInput.enabled = true;

            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
            SceneManager.LoadScene(nextSceneName);
        }
        else
        {
            Debug.LogWarning("Next Scene Name not set in PlayerHealth!");
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