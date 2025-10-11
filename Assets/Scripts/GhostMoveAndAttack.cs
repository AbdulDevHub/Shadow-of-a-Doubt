using UnityEngine;
using System.Collections;
using StarterAssets;

public enum GhostKind { Ice, Fire, Poison }

[DisallowMultipleComponent]
public class GhostMoveAndAttack : MonoBehaviour
{
    [Header("👻 Ghost Identity")]
    public GhostKind ghostKind = GhostKind.Ice;
    public GhostHealth ghostHealth;

    [Header("🎯 Target & Movement")]
    public Transform target;
    public float speed = 2f;
    public float stopDistance = 2f;
    public float rotationSpeed = 5f;

    [Header("🌀 Separation Behavior")]
    public float separationDistance = 0.3f;
    public float separationStrength = 0.25f;
    public float separationSmoothing = 5f;

    [Header("📏 Height Settings")]
    public float minHeightOffset = -0.5f;
    public float heightAdjustSpeed = 3f;

    [Header("💨 Pushback System (Wind Spell Effect)")]
    public float pushDecay = 2f;
    public float pushForwardReductionThreshold = 0.2f;
    [Range(0f, 1f)] public float forwardReductionWhilePushed = 0.5f;

    [Header("⚔️ Attack Settings (Ice & Fire Ghosts)")]
    public bool canAttack = true;
    public float attackDamage = 1f;
    public float attackCooldown = 5f;
    public GameObject attackEffectPrefab;
    [Range(0.01f, 2f)] public float attackEffectScale = 0.05f;

    [Header("🔥 Fire Ghost Knockback")]
    public float fireKnockbackForce = 2f;
    public float fireKnockbackDuration = 0.2f;

    [Header("☠️ Poison Ghost Settings")]
    public float poisonDamagePerSecond = 0.2f;
    public float poisonCheckInterval = 1f;

    [Header("❄️ Ice Ghost Slow Effect")]
    [Range(0.1f, 1f)] public float slowMultiplier = 0.5f;
    public float slowDuration = 3f;

    // --- Internal ---
    private static readonly string GhostTag = "Ghost";

    private Vector3 smoothedSeparation = Vector3.zero;
    private Vector3 pushVelocity = Vector3.zero;
    private float speedMultiplier = 1f;
    private float lastAttackTime = -999f;
    private Coroutine poisonCoroutine;
    [HideInInspector] public PlayerHealth playerHealth;

    private void Awake()
    {
        if (ghostHealth == null) ghostHealth = GetComponent<GhostHealth>();

        if (target == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) target = playerObj.transform;
        }
    }

    private void Start()
    {
        if (target != null) playerHealth = target.GetComponent<PlayerHealth>();
    }

    public void SetTarget(Transform newTarget) => target = newTarget;
    public void SetSpeedMultiplier(float multiplier) => speedMultiplier = multiplier;
    public void ApplyPushback(Vector3 push) => pushVelocity += push;

    private void Update()
    {
        if (target == null) return;
        if (playerHealth == null) playerHealth = target.GetComponent<PlayerHealth>();

        MoveAndFaceTarget();
    }

    private void MoveAndFaceTarget()
    {
        Vector3 toTarget = target.position - transform.position;
        float distanceToTarget = toTarget.magnitude;
        Vector3 direction = toTarget.normalized;

        ApplySeparation(ref direction);
        FaceDirection(direction);
        MoveForward(direction, distanceToTarget);
        ClampHeight();
        HandleAttack(distanceToTarget);
    }

    private void ApplySeparation(ref Vector3 direction)
    {
        Vector3 rawSeparation = Vector3.zero;
        GameObject[] allGhosts = GameObject.FindGameObjectsWithTag(GhostTag);

        foreach (var ghost in allGhosts)
        {
            if (ghost == gameObject) continue;
            float dist = Vector3.Distance(transform.position, ghost.transform.position);
            if (dist < separationDistance && dist > 0.001f)
            {
                float strength = Mathf.Lerp(separationStrength, 0f, dist / separationDistance);
                rawSeparation += (transform.position - ghost.transform.position).normalized * strength;
            }
        }

        smoothedSeparation = Vector3.Lerp(smoothedSeparation, rawSeparation, Time.deltaTime * separationSmoothing);
        if (smoothedSeparation != Vector3.zero)
            direction = (direction + smoothedSeparation).normalized;
    }

    private void FaceDirection(Vector3 direction)
    {
        if (direction == Vector3.zero) return;
        Quaternion lookRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, rotationSpeed * Time.deltaTime);
    }

    private void MoveForward(Vector3 direction, float distanceToTarget)
    {
        Vector3 forwardMove = Vector3.zero;

        if (distanceToTarget > stopDistance)
        {
            float forwardFactor = pushVelocity.magnitude > pushForwardReductionThreshold ? forwardReductionWhilePushed : 1f;
            forwardMove = direction * speed * speedMultiplier * forwardFactor * Time.deltaTime;
        }

        Vector3 pushDelta = pushVelocity * Time.deltaTime;
        transform.position += forwardMove + pushDelta;

        pushVelocity = Vector3.Lerp(pushVelocity, Vector3.zero, Time.deltaTime * pushDecay);
    }

    private void ClampHeight()
    {
        if (target == null) return;

        Vector3 pos = transform.position;
        float minAllowedHeight = target.position.y;
        float desiredHeight = target.position.y + 0.3f;

        if (pos.y < minAllowedHeight)
        {
            pos.y = Mathf.Lerp(pos.y, desiredHeight, Time.deltaTime * heightAdjustSpeed);
            transform.position = pos;
        }
    }

    private void HandleAttack(float distanceToTarget)
    {
        if (!canAttack || playerHealth == null || ghostHealth == null) return;

        bool inRange = distanceToTarget <= stopDistance;

        switch (ghostKind)
        {
            case GhostKind.Ice:
            case GhostKind.Fire:
                if (inRange && Time.time - lastAttackTime >= attackCooldown)
                {
                    lastAttackTime = Time.time;
                    AttackPlayerAtPlayer();
                }
                break;

            case GhostKind.Poison:
                if (inRange)
                {
                    if (poisonCoroutine == null)
                        poisonCoroutine = StartCoroutine(ApplyPoisonDamage());
                }
                else if (poisonCoroutine != null)
                {
                    StopCoroutine(poisonCoroutine);
                    poisonCoroutine = null;

                    var existingEffect = transform.Find(attackEffectPrefab.name + "(Clone)");
                    if (existingEffect != null)
                        Destroy(existingEffect.gameObject);
                }
                break;
        }
    }

    private void AttackPlayerAtPlayer()
    {
        playerHealth.TakeDamage(attackDamage);

        if (ghostKind == GhostKind.Fire && target != null)
            StartCoroutine(ApplyFireKnockback());

        if (ghostKind == GhostKind.Ice && target != null)
            StartCoroutine(ApplyIceSlowEffect());

        if (attackEffectPrefab != null && target != null)
        {
            GameObject effect = Instantiate(attackEffectPrefab, target.position, Quaternion.identity);
            ScaleEffect(effect, attackEffectScale);
            Destroy(effect, 2f);
        }
    }

    private IEnumerator ApplyFireKnockback()
    {
        var controller = target.GetComponent<CharacterController>();
        if (controller == null) yield break;

        Vector3 knockDir = (target.position - transform.position).normalized;
        knockDir.y = 0f;

        float elapsed = 0f;
        while (elapsed < fireKnockbackDuration)
        {
            controller.Move(knockDir * fireKnockbackForce * Time.deltaTime);
            elapsed += Time.deltaTime;
            yield return null;
        }
    }

    private IEnumerator ApplyIceSlowEffect()
{
    if (playerHealth == null) yield break;

    // Apply slow via PlayerHealth, which handles UI
    playerHealth.ApplySlow(slowMultiplier, slowDuration);

    // Just wait for the duration to finish before ending coroutine
    yield return new WaitForSeconds(slowDuration);
}


    private IEnumerator ApplyPoisonDamage()
    {
        GameObject poisonEffect = null;
        if (attackEffectPrefab != null)
        {
            poisonEffect = Instantiate(attackEffectPrefab, transform.position, Quaternion.identity, transform);
            ScaleEffect(poisonEffect, attackEffectScale);
        }

        while (true)
        {
            if (playerHealth == null || ghostHealth == null || ghostHealth.IsDead) break;
            float distance = Vector3.Distance(transform.position, target.position);
            if (distance > stopDistance) break;

            playerHealth.TakeDamage(poisonDamagePerSecond);

            float elapsed = 0f;
            while (elapsed < poisonCheckInterval)
            {
                if (Vector3.Distance(transform.position, target.position) > stopDistance)
                    goto ExitPoison;

                elapsed += Time.deltaTime;
                yield return null;
            }
        }

    ExitPoison:
        if (poisonEffect != null) Destroy(poisonEffect);
        poisonCoroutine = null;
    }

    private void ScaleEffect(GameObject effect, float scaleFactor)
    {
        if (effect == null) return;
        effect.transform.localScale = Vector3.one * scaleFactor;

        foreach (var ps in effect.GetComponentsInChildren<ParticleSystem>())
        {
            var main = ps.main;
            main.startSizeMultiplier *= scaleFactor;
            main.startSpeedMultiplier *= scaleFactor;
            main.gravityModifierMultiplier *= scaleFactor;

            var shape = ps.shape;
            if (shape.enabled)
                shape.radius *= scaleFactor;
        }
    }
}
