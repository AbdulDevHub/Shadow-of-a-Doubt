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

public class GhostHealth : MonoBehaviour
{
    [Header("Health Settings")]
    [SerializeField] private float maxHealth = 3f;
    private float currentHealth;
    private Slider healthBar;

    [Header("Elemental Type")]
    public ElementType ghostType;

    [Header("FX")]
    [SerializeField] private GameObject defaultSmokeEffect;

    [Header("Potion Drop Settings")]
    public PotionDrop[] potionDrops;

    private GhostMovement movement;

    [Header("Elemental Effect Settings")]
    public float burnDamagePerSecond = 0.5f;
    public float burnDuration = 3f;
    public float slowMultiplier = 0.25f;
    public float slowDuration = 3f;
    public float pushForce = 10f;

    private Coroutine burnCoroutine;
    private Coroutine slowCoroutine;

    public Transform playerTransform;

    public event Action<GhostHealth> onGhostDied;

    private void Awake()
    {
        maxHealth = UnityEngine.Random.Range(2, 4);
        currentHealth = maxHealth;

        movement = GetComponent<GhostMovement>();

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
        bool isWeak = IsWeakTo(spellType);

        ApplyElementalEffect(spellType, hitPoint, isWeak);

        if (isWeak)
            TakeDamage(1f);
    }

    private bool IsWeakTo(ElementType spellType)
    {
        return ghostType == spellType;
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

            case ElementType.Water:
                if (slowCoroutine != null) StopCoroutine(slowCoroutine);
                slowCoroutine = StartCoroutine(ApplySlow());
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

    private IEnumerator ApplySlow()
    {
        if (movement == null) yield break;

        movement.SetSpeedMultiplier(slowMultiplier);

        yield return new WaitForSeconds(slowDuration);

        movement.SetSpeedMultiplier(1f);
    }

    public void TakeDamage(float amount)
    {
        currentHealth -= amount;
        if (currentHealth < 0) currentHealth = 0;
        UpdateHealthBar();

        if (currentHealth <= 0)
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
            Instantiate(defaultSmokeEffect, transform.position, Quaternion.identity);

        if (GhostKillManager.Instance != null)
            GhostKillManager.Instance.RegisterKill();

        TryDropPotion();

        onGhostDied?.Invoke(this);

        Destroy(gameObject);
    }

    private void TryDropPotion()
    {
        if (potionDrops == null || potionDrops.Length == 0) return;

        List<PotionDrop> winners = new List<PotionDrop>();

        foreach (PotionDrop drop in potionDrops)
        {
            if (drop.potionPrefab == null) continue;
            if (UnityEngine.Random.value <= drop.dropChance)
                winners.Add(drop);
        }

        if (winners.Count > 0)
        {
            PotionDrop chosen = winners[UnityEngine.Random.Range(0, winners.Count)];
            Instantiate(chosen.potionPrefab, transform.position, Quaternion.identity);
        }
    }
}
