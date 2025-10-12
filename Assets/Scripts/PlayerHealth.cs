using UnityEngine;
using UnityEngine.UI;  // For Slider, RawImage
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

    [Header("Status Overlays (UI)")]
    [SerializeField] private GameObject frozenUI;    // UI for frozen effect
    [SerializeField] private GameObject damageUI;    // UI for damage flash
    [SerializeField] private float damageFlashDuration = 0.2f;
    [SerializeField] private float frozenFadeDuration = 0.5f;
    [SerializeField] private float frozenOverlayAlpha = 0.25f;

    private RawImage frozenImage;   // Changed to RawImage
    private Image damageImage;
    private Coroutine frozenFadeCoroutine;
    private Coroutine damageFlashCoroutine;

    [Header("Game Over UI References")]
    public Image fadePanel;
    public GameObject gameOverUI;

    [Header("Fade Settings")]
    public float fadeDuration = 0.5f;
    [Range(0f, 1f)] public float targetAlpha = 0.7f;
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

    [Header("Button Sounds")]
    [SerializeField] private AudioClip healSound;     // For respawn button
    [SerializeField] private AudioClip clickSound;    // For other buttons
    [SerializeField] private AudioClip toggleSound;   // For difficulty button

    [Header("Scene Settings")]
    public string nextSceneName;

    // === Slow / Frozen Handling ===
    private float slowTimer = 0f;
    private bool isSlowed = false;
    private StarterAssets.FirstPersonController playerController;
    private float originalMoveSpeed, originalSprintSpeed, originalRotationSpeed;

    private ScoreManager scoreManager;

    private void Start()
    {
        currentHealth = maxHealth;
        UpdateHealthUI();

        scoreManager = FindObjectOfType<ScoreManager>();

        if (gameOverUI != null)
            gameOverUI.SetActive(false);

        if (!otherScriptHandlesFade && fadePanel != null)
        {
            SetImageAlpha(0f);
            fadePanel.gameObject.SetActive(false);
        }

        if (respawnButton != null) respawnButton.onClick.AddListener(Respawn);
        if (restartButton != null) restartButton.onClick.AddListener(RestartLevel);
        if (skipButton != null) skipButton.onClick.AddListener(SkipLevel);
        if (quitButton != null) quitButton.onClick.AddListener(QuitGame);
        if (difficultyButton != null) difficultyButton.onClick.AddListener(ChangeDifficulty);

        UpdateDifficultyButtonText();

        // Cache image components if available
        if (frozenUI != null)
        {
            frozenImage = frozenUI.GetComponent<RawImage>(); // Updated
            frozenUI.SetActive(false);
        }

        if (damageUI != null)
        {
            damageImage = damageUI.GetComponent<Image>();
            damageUI.SetActive(false);
        }

        // Cache player controller for slow/frozen effects
        playerController = GetComponent<StarterAssets.FirstPersonController>();
        if (playerController != null)
        {
            originalMoveSpeed = playerController.MoveSpeed;
            originalSprintSpeed = playerController.SprintSpeed;
            originalRotationSpeed = playerController.RotationSpeed;
        }
    }

    private void Update()
    {
        // Handle slow timer
        if (isSlowed)
        {
            slowTimer -= Time.deltaTime;
            if (slowTimer <= 0f)
            {
                ResetPlayerSpeed();
                SetFrozen(false);
                isSlowed = false;
            }
        }
    }

    public void TakeDamage(float amount)
    {
        if (isDead || isHealthLocked) return;

        currentHealth = Mathf.Max(0, currentHealth - amount);
        UpdateHealthUI();

        if (damageUI != null)
        {
            if (damageFlashCoroutine != null)
                StopCoroutine(damageFlashCoroutine);
            damageFlashCoroutine = StartCoroutine(FlashDamageUI());
        }

        if (currentHealth <= 0)
            Die();
    }

    private IEnumerator FlashDamageUI()
    {
        damageUI.SetActive(true);
        if (damageImage == null)
            damageImage = damageUI.GetComponent<Image>();

        Color startColor = damageImage.color;

        float elapsed = 0f;
        while (elapsed < damageFlashDuration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(startColor.a, 0f, elapsed / damageFlashDuration);
            damageImage.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
            yield return null;
        }

        damageUI.SetActive(false);
        damageImage.color = startColor;
    }

    // === Slow / Frozen Methods ===
    public void ApplySlow(float multiplier, float duration)
    {
        if (playerController == null) return;

        slowTimer = duration;

        if (!isSlowed)
        {
            isSlowed = true;

            originalMoveSpeed = playerController.MoveSpeed;
            originalSprintSpeed = playerController.SprintSpeed;
            originalRotationSpeed = playerController.RotationSpeed;

            playerController.MoveSpeed *= multiplier;
            playerController.SprintSpeed *= multiplier;
            playerController.RotationSpeed *= multiplier;

            SetFrozen(true);
        }
    }

    private void ResetPlayerSpeed()
    {
        if (playerController == null) return;

        playerController.MoveSpeed = originalMoveSpeed;
        playerController.SprintSpeed = originalSprintSpeed;
        playerController.RotationSpeed = originalRotationSpeed;
    }

    public void SetFrozen(bool isFrozen)
    {
        if (frozenUI == null || frozenImage == null)
            return;

        if (frozenFadeCoroutine != null)
            StopCoroutine(frozenFadeCoroutine);
        frozenFadeCoroutine = StartCoroutine(FadeFrozenUI(isFrozen));
    }

    private IEnumerator FadeFrozenUI(bool fadeIn)
    {
        frozenUI.SetActive(true);
        float startAlpha = frozenImage.color.a;
        float endAlpha = fadeIn ? frozenOverlayAlpha : 0f;
        float elapsed = 0f;

        while (elapsed < frozenFadeDuration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(startAlpha, endAlpha, elapsed / frozenFadeDuration);
            Color c = frozenImage.color;
            c.a = alpha;
            frozenImage.color = c;
            yield return null;
        }

        Color finalColor = frozenImage.color;
        finalColor.a = endAlpha;
        frozenImage.color = finalColor;

        if (!fadeIn)
            frozenUI.SetActive(false);
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

        // ✅ Pause timer here
        if (scoreManager != null)
            scoreManager.StopTimer();

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
        // Play heal sound
        if (healSound != null)
            PlayUISound(healSound);
        
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

        // ✅ Subtract 50 points
        if (scoreManager != null)
            scoreManager.AddScore(-50);

        // ✅ Resume timer
        if (scoreManager != null)
            scoreManager.ResumeTimer();

        Debug.Log("Player respawned with full health.");
    }

    private void HideGameOverUI()
    {
        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        StartCoroutine(FadeOutAndDisable());
    }

    private IEnumerator FadeOutAndDisable()
    {
        yield return StartCoroutine(FadeImage(fadePanel, fadePanel.color.a, 0f));
        fadePanel.gameObject.SetActive(false);
        gameOverUI.SetActive(false);
    }

    public void RestartLevel()
    {
        // Play click sound
        if (clickSound != null)
            PlayUISound(clickSound);

        Time.timeScale = 1f;

        if (playerInput != null)
            playerInput.enabled = true;

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        // ✅ Reset timer + score
        if (scoreManager != null)
        {
            scoreManager.ResetLevelTimer();
            scoreManager.ResetLevelScore();
        }

        Scene current = SceneManager.GetActiveScene();
        SceneManager.LoadScene(current.name);
    }

    public void SkipLevel()
    {
        // Play click sound
        if (clickSound != null)
            PlayUISound(clickSound);

        if (!string.IsNullOrEmpty(nextSceneName))
        {
            Time.timeScale = 1f;

            if (playerInput != null)
                playerInput.enabled = true;

            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;

            // ✅ Reset timer (and optionally score)
            if (scoreManager != null)
            {
                scoreManager.ResetLevelTimer();
                // scoreManager.ResetLevelScore(); // uncomment if needed
            }

            SceneManager.LoadScene(nextSceneName);
        }
        else
        {
            Debug.LogWarning("Next Scene Name not set in PlayerHealth!");
        }
    }

    public void QuitGame()
    {
        // Play click sound
        if (clickSound != null)
            PlayUISound(clickSound);

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
        // Play toggle sound
        if (toggleSound != null)
            PlayUISound(toggleSound);

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

    private void PlayUISound(AudioClip clip, float volume = 1f)
    {
        if (clip == null) return;

        GameObject tempGO = new GameObject("TempAudio");
        AudioSource aSource = tempGO.AddComponent<AudioSource>();
        aSource.clip = clip;
        aSource.volume = volume;
        aSource.spatialBlend = 0f; // 0 = 2D
        aSource.Play();
        Destroy(tempGO, clip.length);
    }
}