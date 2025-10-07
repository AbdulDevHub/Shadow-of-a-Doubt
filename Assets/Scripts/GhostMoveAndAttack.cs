using UnityEngine;
using System.Collections;
using StarterAssets;

public enum GhostKind { Ice, Fire, Poison }

[DisallowMultipleComponent]
public class GhostMoveAndAttack : MonoBehaviour
{
    [Header("👻 Ghost Identity")]
    [Tooltip("Select what kind of ghost this is (Ice, Fire, or Poison). Determines attack behavior.")]
    public GhostKind ghostKind = GhostKind.Ice;

    [Tooltip("Reference to this ghost's health component. Auto-assigned on Awake.")]
    public GhostHealth ghostHealth;

    [Header("🎯 Target & Movement")]
    [Tooltip("The player the ghost will chase. Auto-assigned on Awake (by tag 'Player').")]
    public Transform target;

    [Tooltip("Base movement speed toward the player.")]
    public float speed = 2f;

    [Tooltip("How close the ghost must get before stopping and attacking.")]
    public float stopDistance = 2f;

    [Tooltip("How quickly the ghost rotates to face the player.")]
    public float rotationSpeed = 5f;

    [Header("🌀 Separation Behavior")]
    [Tooltip("Distance at which ghosts start pushing apart to avoid overlapping.")]
    public float separationDistance = 0.3f;

    [Tooltip("Strength of the push-apart force between ghosts.")]
    public float separationStrength = 0.25f;

    [Tooltip("Smooths the movement correction from separation.")]
    public float separationSmoothing = 5f;

    [Header("📏 Height Settings")]
    [Tooltip("Minimum Y offset below the player's height.")]
    public float minHeightOffset = -0.5f;

    [Tooltip("Speed at which the ghost adjusts to its correct height.")]
    public float heightAdjustSpeed = 3f;

    [Header("💨 Pushback System (Wind Spell Effect)")]
    [Tooltip("How fast the pushback force fades out.")]
    public float pushDecay = 2f;

    [Tooltip("When push speed exceeds this threshold, forward motion slows.")]
    public float pushForwardReductionThreshold = 0.2f;

    [Range(0f, 1f)]
    [Tooltip("How much forward movement remains while being pushed back.")]
    public float forwardReductionWhilePushed = 0.5f;

    [Header("⚔️ Attack Settings (Ice & Fire Ghosts)")]
    [Tooltip("Enable or disable attacks entirely.")]
    public bool canAttack = true;

    [Tooltip("Damage dealt per attack by Ice/Fire ghosts.")]
    public float attackDamage = 1f;

    [Tooltip("Time between each attack (seconds).")]
    public float attackCooldown = 5f;

    [Tooltip("Effect prefab spawned when attacking.")]
    public GameObject attackEffectPrefab;

    [Tooltip("Visual size of attack/poison effect.")]
    [Range(0.01f, 2f)] public float attackEffectScale = 0.05f;

    [Header("🔥 Fire Ghost Knockback")]
    [Tooltip("Force applied to the player when hit by a Fire ghost.")]
    public float fireKnockbackForce = 2f;

    [Tooltip("How long the knockback lasts (seconds).")]
    public float fireKnockbackDuration = 0.2f;

    [Header("☠️ Poison Ghost Settings")]
    [Tooltip("Damage per tick while near the player.")]
    public float poisonDamagePerSecond = 0.2f;

    [Tooltip("How often poison damage is applied (seconds).")]
    public float poisonCheckInterval = 1f;

    [Header("❄️ Ice Ghost Slow Effect")]
    [Tooltip("How much to reduce the player's movement speed when hit by Ice ghost.")]
    [Range(0.1f, 1f)] public float slowMultiplier = 0.5f;

    [Tooltip("How long the slow lasts (seconds).")]
    public float slowDuration = 3f;


    // --- Internal ---
    private static readonly string GhostTag = "Ghost";
    
    private static bool isPlayerSlowed = false;
    private static float slowTimer = 0f;
    private static StarterAssets.FirstPersonController slowedController = null;
    private static float originalMoveSpeed, originalSprintSpeed, originalRotationSpeed;
    
    private Vector3 smoothedSeparation = Vector3.zero;
    private Vector3 pushVelocity = Vector3.zero;
    private float speedMultiplier = 1f;
    private float lastAttackTime = -999f;
    private Coroutine poisonCoroutine;
    [HideInInspector] public PlayerHealth playerHealth;

    // ---------------- Initialization ---------------- //
    private void Awake()
    {
        if (ghostHealth == null)
            ghostHealth = GetComponent<GhostHealth>();

        if (target == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
                target = playerObj.transform;
        }
    }

    private void Start()
    {
        if (target != null)
            playerHealth = target.GetComponent<PlayerHealth>();
    }

    // ---------------- Public Helpers ---------------- //
    public void SetTarget(Transform newTarget) => target = newTarget;
    public void SetSpeedMultiplier(float multiplier) => speedMultiplier = multiplier;
    public void ApplyPushback(Vector3 push) => pushVelocity += push;

    // ---------------- Update Loop ---------------- //
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

    // ---------------- Movement Logic ---------------- //
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

        // Define the minimum height the ghost can have relative to the player.
        // Example: never lower than player.y - 0.5f (slightly below feet if on level ground)
        float minAllowedHeight = target.position.y;

        // Define the desired hover height — around player's chest/eye level
        float desiredHeight = target.position.y + 0.3f;

        // If ghost is below minAllowedHeight, bring it up smoothly
        if (pos.y < minAllowedHeight)
        {
            pos.y = Mathf.Lerp(pos.y, desiredHeight, Time.deltaTime * heightAdjustSpeed);
            transform.position = pos;
        }
    }

    // ---------------- Attack Logic ---------------- //
    private void HandleAttack(float distanceToTarget)
    {
        if (!canAttack || playerHealth == null || ghostHealth == null)
            return;

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

                    // Immediately clean up any lingering poison visuals
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

        // --- Fire Ghost Knockback ---
        if (ghostKind == GhostKind.Fire && target != null)
        {
            StartCoroutine(ApplyFireKnockback());
        }

        // --- Ice Ghost Special Slow Effect --- //
        if (ghostKind == GhostKind.Ice && target != null)
        {
            StartCoroutine(ApplyIceSlowEffect());
        }

        // --- Spawn visual attack effect --- //
        if (attackEffectPrefab != null && target != null)
        {
            GameObject effect = Instantiate(attackEffectPrefab, target.position, Quaternion.identity);
            ScaleEffect(effect, attackEffectScale);
            Destroy(effect, 2f);
        }
    }

    private IEnumerator ApplyFireKnockback()
    {
        // Require CharacterController on player
        var controller = target.GetComponent<CharacterController>();
        if (controller == null) yield break;

        Vector3 knockDir = (target.position - transform.position).normalized;
        knockDir.y = 0f; // keep horizontal only

        float elapsed = 0f;

        while (elapsed < fireKnockbackDuration)
        {
            // Apply small movement respecting collisions
            controller.Move(knockDir * fireKnockbackForce * Time.deltaTime);
            elapsed += Time.deltaTime;
            yield return null;
        }
    }

    private IEnumerator ApplyIceSlowEffect()
    {
        if (target == null) yield break;

        var controller = target.GetComponent<StarterAssets.FirstPersonController>();
        if (controller == null) yield break;

        // --- If already slowed, just refresh timer ---
        if (isPlayerSlowed)
        {
            slowTimer = slowDuration; // reset the remaining time
            yield break;
        }

        // --- Apply slow effect ---
        isPlayerSlowed = true;
        slowedController = controller;

        originalMoveSpeed = controller.MoveSpeed;
        originalSprintSpeed = controller.SprintSpeed;
        originalRotationSpeed = controller.RotationSpeed;

        controller.MoveSpeed = originalMoveSpeed * slowMultiplier;
        controller.SprintSpeed = originalSprintSpeed * slowMultiplier;
        controller.RotationSpeed = originalRotationSpeed * slowMultiplier;

        // --- Timer loop (runs once globally) ---
        slowTimer = slowDuration;
        while (slowTimer > 0f)
        {
            slowTimer -= Time.deltaTime;
            yield return null;
        }

        // --- Restore original values safely ---
        if (slowedController != null)
        {
            slowedController.MoveSpeed = originalMoveSpeed;
            slowedController.SprintSpeed = originalSprintSpeed;
            slowedController.RotationSpeed = originalRotationSpeed;
        }

        slowedController = null;
        isPlayerSlowed = false;
    }

    private IEnumerator ApplyPoisonDamage()
    {
        GameObject poisonEffect = null;

        if (attackEffectPrefab != null)
        {
            poisonEffect = Instantiate(attackEffectPrefab, transform.position, Quaternion.identity, transform);
            ScaleEffect(poisonEffect, attackEffectScale);
        }

        // Continue as long as ghost and player are alive and in range
        while (true)
        {
            if (playerHealth == null || ghostHealth == null || ghostHealth.IsDead)
                break;

            float distance = Vector3.Distance(transform.position, target.position);

            // Stop immediately if no longer close enough
            if (distance > stopDistance)
                break;

            // Apply poison damage instantly each interval while in range
            playerHealth.TakeDamage(poisonDamagePerSecond);

            // Wait a short time before next tick, but exit instantly if pushed away
            float elapsed = 0f;
            while (elapsed < poisonCheckInterval)
            {
                if (Vector3.Distance(transform.position, target.position) > stopDistance)
                    goto ExitPoison; // break out immediately if ghost is pushed away

                elapsed += Time.deltaTime;
                yield return null;
            }
        }

    ExitPoison:
        if (poisonEffect != null)
            Destroy(poisonEffect);

        poisonCoroutine = null;
    }

    // ---------------- Utilities ---------------- //
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
