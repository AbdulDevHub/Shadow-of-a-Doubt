using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;
using System.Collections.Generic;

public enum ElementType { Fire, Water, Wind }

[Serializable]
public class PotionDrop
{
    public GameObject potionPrefab;
    [Range(0f, 1f)] public float dropChance = 0.2f;
}

[DisallowMultipleComponent]
public class GhostHealth : MonoBehaviour
{
    [Header("❤️ Health Settings")]
    [SerializeField] private float maxHealth = 3f;
    private float currentHealth;
    [SerializeField] private Slider healthBar;

    [Header("🧩 Spell Weakness")]
    public ElementType weaknessTo;

    [Header("💨 Linked Components")]
    private GhostMoveAndAttack movement;
    public Transform playerTransform;

    [Header("✨ FX & VFX")]
    [SerializeField] private GameObject defaultSmokeEffect;

    [Header("🧪 Potion Drop Settings")]
    public PotionDrop[] potionDrops;

    [Header("🔥 Elemental Effect Settings")]
    public float burnDamagePerSecond = 0.5f;
    public float burnDuration = 3f;
    public float pushForce = 10f;

    private Coroutine burnCoroutine;

    public bool IsDead => currentHealth <= 0f;
    public event Action<GhostHealth> onGhostDied;

    private void Awake()
    {
        maxHealth = UnityEngine.Random.Range(2, 4);
        currentHealth = maxHealth;

        movement = GetComponent<GhostMoveAndAttack>();
        healthBar = GetComponentInChildren<Slider>();

        if (healthBar != null)
        {
            healthBar.maxValue = 1f;
            healthBar.value = 1f;
        }

        if (playerTransform == null)
        {
            GameObject player = GameObject.FindWithTag("Player");
            if (player != null) playerTransform = player.transform;
        }
    }

    public void ApplySpellHit(ElementType spellType, Vector3 hitPoint)
    {
        bool isWeak = (spellType == weaknessTo);
        ApplyElementalEffect(spellType, hitPoint, isWeak);

        if (isWeak)
            TakeDamage(1f);
    }

    private void ApplyElementalEffect(ElementType spellType, Vector3 hitPoint, bool applyDamage)
    {
        switch (spellType)
        {
            case ElementType.Fire:
                if (applyDamage)
                {
                    if (burnCoroutine != null) StopCoroutine(burnCoroutine);
                    burnCoroutine = StartCoroutine(ApplyBurn());
                }
                break;

            case ElementType.Wind:
                if (movement != null && playerTransform != null)
                {
                    Vector3 pushDir = (transform.position - playerTransform.position).normalized;
                    movement.ApplyPushback(pushDir * pushForce);
                }
                break;
        }
    }

    private IEnumerator ApplyBurn()
    {
        float elapsed = 0f;
        while (elapsed < burnDuration)
        {
            TakeDamage(burnDamagePerSecond * Time.deltaTime);
            elapsed += Time.deltaTime;
            yield return null;
        }
    }

    public void TakeDamage(float amount)
    {
        currentHealth -= amount;
        if (currentHealth < 0f) currentHealth = 0f;

        UpdateHealthBar();

        if (currentHealth <= 0f)
            Die();
    }

    private void UpdateHealthBar()
    {
        if (healthBar != null)
            healthBar.value = currentHealth / maxHealth;
    }

    private void Die()
    {
        if (defaultSmokeEffect != null)
            Instantiate(defaultSmokeEffect, transform.position, defaultSmokeEffect.transform.rotation);

        TryDropPotion();
        onGhostDied?.Invoke(this);
        Destroy(gameObject);
    }

    private void TryDropPotion()
    {
        if (potionDrops == null || potionDrops.Length == 0) return;

        List<PotionDrop> candidates = new List<PotionDrop>();
        foreach (PotionDrop drop in potionDrops)
        {
            if (drop.potionPrefab == null) continue;
            if (UnityEngine.Random.value <= drop.dropChance)
                candidates.Add(drop);
        }

        if (candidates.Count > 0)
        {
            PotionDrop chosen = candidates[UnityEngine.Random.Range(0, candidates.Count)];
            Instantiate(chosen.potionPrefab, transform.position, Quaternion.identity);
        }
    }
}
