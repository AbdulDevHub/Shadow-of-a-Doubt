using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;
using System.Collections.Generic;

public enum ElementType { Fire, Water, Wind }

[Serializable]
public class PotionDrop
{
    [Tooltip("Prefab of the potion that may drop when this ghost dies.")]
    public GameObject potionPrefab;

    [Range(0f, 1f)]
    [Tooltip("Chance that this potion will drop (0–1).")]
    public float dropChance = 0.2f;
}

[DisallowMultipleComponent]
public class GhostHealth : MonoBehaviour
{
    [Header("❤️ Health Settings")]
    [SerializeField, Tooltip("Maximum health of this ghost. Set randomly on Awake if not overridden.")]
    private float maxHealth = 3f;
    private float currentHealth;

    [SerializeField, Tooltip("Slider UI used to display ghost health. Auto-assigned if found in children.")]
    private Slider healthBar;

    [Header("🧩 Spell Weakness")]
    [Tooltip("Which spell type (Fire, Water, Wind) deals damage to this ghost.")]
    public ElementType weaknessTo;

    [Header("💨 Linked Components")]
    [Tooltip("The ghost’s movement and attack controller. Auto-assigned on Awake.")]
    private GhostMoveAndAttack movement;

    [Tooltip("Reference to the player for positional effects. Auto-found on Awake.")]
    public Transform playerTransform;

    [Header("✨ FX & VFX")]
    [SerializeField, Tooltip("Effect prefab spawned when the ghost dies.")]
    private GameObject defaultSmokeEffect;

    [Header("🧪 Potion Drop Settings")]
    [Tooltip("Possible potion drops when the ghost dies.")]
    public PotionDrop[] potionDrops;

    [Header("🔥 Elemental Effect Settings")]
    [Tooltip("Damage per second while burning (Fire spell).")]
    public float burnDamagePerSecond = 0.5f;

    [Tooltip("Duration of burn effect (seconds).")]
    public float burnDuration = 3f;

    [Tooltip("Speed multiplier when slowed by Water spell.")]
    public float slowMultiplier = 0.25f;

    [Tooltip("Duration of slow effect (seconds).")]
    public float slowDuration = 3f;

    [Tooltip("Force applied when pushed back by Wind spell.")]
    public float pushForce = 10f;

    private Coroutine burnCoroutine;
    private Coroutine slowCoroutine;

    public bool IsDead => currentHealth <= 0f;
    public event Action<GhostHealth> onGhostDied;

    // -------------------- LIFECYCLE -------------------- //
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
            if (player != null)
                playerTransform = player.transform;
        }
    }

    // -------------------- SPELL INTERACTION -------------------- //
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

    // -------------------- EFFECT COROUTINES -------------------- //
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

    // -------------------- HEALTH MANAGEMENT -------------------- //
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
        {
            Instantiate(defaultSmokeEffect, transform.position, defaultSmokeEffect.transform.rotation);
        }

        // Only report kills if a manager actually exists in this scene
        if (GhostKillManager.Instance != null && GhostKillManager.Instance.gameObject != null)
            GhostKillManager.Instance.RegisterKill();

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
