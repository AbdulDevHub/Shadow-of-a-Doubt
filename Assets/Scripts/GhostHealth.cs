using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;

[Serializable]
public class PotionDrop
{
    public GameObject potionPrefab;   // prefab to drop
    [Range(0f, 1f)] public float dropChance = 0.2f; // independent chance (0–1)
}

public class GhostHealth : MonoBehaviour
{
    [Header("Health Settings")]
    [SerializeField] private float maxHealth = 3f;   

    private Slider healthBar;
    private float currentHealth;

    [Header("FX")]
    [SerializeField] private GameObject defaultSmokeEffect;

    [Header("Potion Drop Settings")]
    [Tooltip("Each potion has an independent chance. Only one max will spawn.")]
    public PotionDrop[] potionDrops;

    // event for death
    public event Action<GhostHealth> onGhostDied;

    private void Awake()
    {
        maxHealth = UnityEngine.Random.Range(2, 4);
        currentHealth = maxHealth;

        healthBar = GetComponentInChildren<Slider>();
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
        // 🔹 Spawn smoke FX
        if (defaultSmokeEffect != null)
        {
            GameObject spawnedFx = Instantiate(
                defaultSmokeEffect,
                transform.position,
                defaultSmokeEffect.transform.rotation
            );
            spawnedFx.transform.localScale = Vector3.one * defaultSmokeEffect.transform.localScale.x;
        }

        // 🔹 Register kill
        if (GhostKillManager.Instance != null)
        {
            GhostKillManager.Instance.RegisterKill();
        }

        // 🔹 Potion drop logic (independent, one max)
        TryDropPotion();

        // 🔹 Notify listeners
        onGhostDied?.Invoke(this);

        // 🔹 Destroy ghost
        Destroy(gameObject);
    }

    private void TryDropPotion()
    {
        if (potionDrops == null || potionDrops.Length == 0) return;

        List<PotionDrop> winners = new List<PotionDrop>();

        foreach (PotionDrop drop in potionDrops)
        {
            if (drop.potionPrefab == null) continue;

            float roll = UnityEngine.Random.value;
            if (roll <= drop.dropChance)
            {
                winners.Add(drop);
            }
        }

        if (winners.Count > 0)
        {
            // pick one at random if multiple succeeded
            PotionDrop chosen = winners[UnityEngine.Random.Range(0, winners.Count)];
            Instantiate(chosen.potionPrefab, transform.position, Quaternion.identity);
        }
    }
}
