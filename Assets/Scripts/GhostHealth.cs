using UnityEngine;
using UnityEngine.UI;

public class GhostHealth : MonoBehaviour
{
    [Header("Health Settings")]
    [SerializeField] private float maxHealth = 3f;   
    [SerializeField] private Slider healthBar;   // Use Slider instead of Image

    private float currentHealth;

    [Header("FX")]
    [SerializeField] private GameObject defaultSmokeEffect;

    private void Awake()
    {
        // Randomize health between 2 and 3
        maxHealth = Random.Range(2, 4);
        currentHealth = maxHealth;

        if (healthBar != null)
        {
            healthBar.maxValue = 1f;
            healthBar.value = 1f;
        }
    }

    public void TakeDamage(float amount)
    {
        currentHealth -= amount;
        if (currentHealth < 0) currentHealth = 0;

        UpdateHealthBar();

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void UpdateHealthBar()
    {
        if (healthBar != null)
        {
            healthBar.value = currentHealth / maxHealth;
        }
    }

    public void Die()
    {
        if (defaultSmokeEffect != null)
        {
            GameObject spawnedFx = Instantiate(
                defaultSmokeEffect,
                transform.position,
                defaultSmokeEffect.transform.rotation
            );

            spawnedFx.transform.localScale =
                Vector3.one * defaultSmokeEffect.transform.localScale.x;
        }

        Destroy(gameObject);
    }
}
