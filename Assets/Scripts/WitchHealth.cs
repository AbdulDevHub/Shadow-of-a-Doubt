using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class WitchHealth : MonoBehaviour
{
    [Header("Health Settings")]
    [SerializeField] private float maxHealth = 5f;
    [SerializeField] private float rechargeSpeed = 0.2f;

    private Slider healthBar;
    private float currentHealth;
    private Image healthFill;

    [Header("UI References")]
    public Image fadePanel;      
    public Image ghostIcon;      
    [SerializeField] private float fadeDuration = 2f;

    private bool isDead = false;
    private bool canTakeDamage = false;

    [Header("Target Reference")]
    [SerializeField] private Transform player; // 🔹 assign Player transform in Inspector

    private void Awake()
    {
        currentHealth = maxHealth;

        healthBar = GetComponentInChildren<Slider>(true);
        if (healthBar != null)
        {
            healthBar.maxValue = 1f;
            healthBar.value = 1f;
            healthBar.gameObject.SetActive(false);

            if (healthBar.fillRect != null)
                healthFill = healthBar.fillRect.GetComponent<Image>();
        }

        if (fadePanel != null)
        {
            fadePanel.color = new Color(0, 0, 0, 0);
        }

        // 🔹 Auto-find player if not set
        if (player == null && GameObject.FindGameObjectWithTag("Player") != null)
        {
            player = GameObject.FindGameObjectWithTag("Player").transform;
        }
    }

    private void Update()
    {
        FacePlayer(); // 🔹 Always face the player
    }

    // 🔹 Make witch face player
    private void FacePlayer()
    {
        if (player == null || isDead) return;

        Vector3 direction = player.position - transform.position;
        direction.y = 0; // keep only horizontal rotation
        if (direction.sqrMagnitude > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 5f);
        }
    }

    /// <summary>
    /// Called externally by DialogueSequence after dialogue finishes.
    /// </summary>
    public void EnableHealthBar()
    {
        if (healthBar != null)
            healthBar.gameObject.SetActive(true);

        canTakeDamage = true; 
    }

    public void TakeDamage(float amount, int spellIndex)
    {
        if (isDead || !canTakeDamage) return;

        WitchShield shield = GetComponentInChildren<WitchShield>();
        if (shield != null && !shield.CanTakeDamage(spellIndex))
        {
            return;
        }

        currentHealth -= amount;
        if (currentHealth < 0) currentHealth = 0;

        UpdateHealthBar();

        if (currentHealth <= 0)
        {
            StartCoroutine(DeathSequence());
        }
    }

    private void UpdateHealthBar()
    {
        if (healthBar != null)
            healthBar.value = currentHealth / maxHealth;
    }

    private IEnumerator DeathSequence()
    {
        isDead = true;

        if (ghostIcon != null)
            ghostIcon.gameObject.SetActive(false);

        if (healthFill != null)
            healthFill.color = Color.red;

        WitchAttackController attackController = GetComponent<WitchAttackController>();
        if (attackController != null)
            attackController.StopAttacks();

        while (currentHealth < maxHealth)
        {
            currentHealth += Time.deltaTime * rechargeSpeed * maxHealth;
            if (currentHealth > maxHealth) currentHealth = maxHealth;
            UpdateHealthBar();
            yield return null;
        }

        if (fadePanel != null)
            yield return StartCoroutine(FadeInPanel());

        SceneManager.LoadScene("Ending");
    }

    private IEnumerator FadeInPanel()
    {
        float elapsed = 0f;
        Color c = fadePanel.color;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Clamp01(elapsed / fadeDuration);
            fadePanel.color = new Color(c.r, c.g, c.b, alpha);
            yield return null;
        }
        fadePanel.color = new Color(c.r, c.g, c.b, 1f);
    }
}
