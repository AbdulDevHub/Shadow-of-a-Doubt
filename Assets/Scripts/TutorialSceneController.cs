using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using StarterAssets;

public class TutorialSceneStandalone : MonoBehaviour
{
    [Header("UI")]
    public Image fadePanel;
    public GameObject dialogueUI;
    public TMP_Text dialogueText;
    public TMP_Text characterNameText;
    public GameObject combatUIParent;
    public Slider healthSlider;
    public Slider manaSlider;
    public TextMeshProUGUI interactText;

    [Header("Camera & Shooting")]
    public Camera playerCamera;
    public float shootingRange = 100f;
    public LayerMask hitMask = ~0;
    public QueryTriggerInteraction rayQueryTrigger = QueryTriggerInteraction.Ignore;

    [Header("Prefabs")]
    public GameObject spellPrefab;
    public GameObject healthPotionPrefab;
    public GameObject ghostPrefab;
    public GameObject ghostSmokeFX;

    [Header("Player")]
    public Transform playerRoot;
    private FirstPersonController playerController;

    [Header("Mana / Spell")]
    public float maxMana = 10f;
    private float currentMana;

    public float rapidFireRate = 0.2f;
    public float manaPerShot = 1f;
    public float burstLifetime = 2f;

    [Header("Recharge")]
    public float slowRechargeRate = 1f;
    public float fastRechargeRate = 5f;
    public float rechargeDelay = 2f;

    [Header("Health (tutorial)")]
    public float maxHealth = 100f;
    private float currentHealth = 70f;
    private float minTutorialHealth;

    [Header("Potion Interaction")]
    public float interactDistance = 4f;

    [Header("Dialogue Settings")]
    [TextArea(3, 10)] public string walkingTutorialDialogue;
    [TextArea(3, 10)] public string shootingTutorialDialogue;
    [TextArea(3, 10)] public string ghostEncounterDialogue;
    [TextArea(3, 10)] public string potionDropDialogue;
    [TextArea(3, 10)] public string healthRestoredDialogue;
    [TextArea(3, 10)] public string guardingDutyDialogue;
    [TextArea(3, 10)] public string keepEyesPeeledDialogue;

    [Header("Ghost Attack Settings")]
    public GameObject iceAttackEffectPrefab;
    public Slider playerHealthBar;
    public float ghostAttackDamage = 0.5f;
    public float ghostAttackCooldown = 4f;
    public float ghostStopDistance = 2f;
    public float ghostMoveSpeed = 1.5f;
    
    [Header("Ice Ghost Slow Effect")]
    public float slowMultiplier = 0.5f;
    public float slowDuration = 3f;

    // Shooting state
    private bool allowShooting = false;
    private bool isFiring = false;
    private Coroutine fireRoutine = null;
    private Coroutine rechargeCoroutine = null;
    private float lastShotTime = 0f;

    // Ghost state
    private GameObject spawnedGhost = null;
    private float ghostMaxHealth;
    private float ghostCurrentHealth;
    private Slider ghostHealthBar;
    private float ghostLastAttackTime = -999f;
    private Coroutine ghostAttackCoroutine = null;

    // Burn system
    private Coroutine burnCoroutine;
    public float burnDamagePerSecond = 0.5f;
    public float burnDuration = 3f;

    // Potion state
    private GameObject spawnedPotion = null;

    // Player Health
    private PlayerHealth playerHealthComponent;

    private void Awake()
    {
        if (playerCamera == null)
            playerCamera = Camera.main;
    }

    private void Start()
    {
        // Hide cursor
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        currentMana = maxMana;
        UpdateManaBar();

        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);
        UpdateHealthBar();
        // ✅ Ensure player can't drop below 30% health (i.e. only lose 70%)
        minTutorialHealth = maxHealth * 0.3f;

        if (dialogueUI != null) dialogueUI.SetActive(false);
        if (combatUIParent != null) combatUIParent.SetActive(false);

        if (ghostPrefab != null)
            ghostPrefab.SetActive(false);

        if (interactText != null)
            interactText.gameObject.SetActive(false);

        // Ensure player has PlayerHealth
        playerHealthComponent = playerRoot.GetComponent<PlayerHealth>();
        if (playerHealthComponent == null)
        {
            playerHealthComponent = playerRoot.gameObject.AddComponent<PlayerHealth>();
            playerHealthComponent.maxHealth = maxHealth;
            playerHealthComponent.HealthBar = playerHealthBar;
        }

        // Get the FirstPersonController component
        playerController = playerRoot.GetComponent<FirstPersonController>();

        StartCoroutine(RunTutorial());
    }

    private void Update()
    {
        if (allowShooting)
            HandleShootingInput();

        // Handle ghost movement and attacks
        if (spawnedGhost != null)
            UpdateGhost();
    }

    #region Ghost Movement & Attack
    private void UpdateGhost()
    {
        if (playerRoot == null) return;

        // Calculate distance and direction to player
        Vector3 toPlayer = playerRoot.position - spawnedGhost.transform.position;
        float distance = toPlayer.magnitude;
        Vector3 direction = toPlayer.normalized;

        // Move toward player if far enough
        if (distance > ghostStopDistance)
        {
            spawnedGhost.transform.position += direction * ghostMoveSpeed * Time.deltaTime;
            spawnedGhost.transform.LookAt(playerRoot);
        }
        else
        {
            // In attack range - attack if cooldown is ready
            if (Time.time - ghostLastAttackTime >= ghostAttackCooldown)
            {
                ghostLastAttackTime = Time.time;
                GhostAttackPlayer();
            }
        }
    }

    private void GhostAttackPlayer()
    {
        if (playerHealthComponent == null) return;

        // Deal damage to player
        playerHealthComponent.TakeDamage(ghostAttackDamage);

        // ✅ Prevent health from dropping below 30% of max health
        float minHealth = playerHealthComponent.maxHealth * 0.3f;
        var field = typeof(PlayerHealth).GetField("currentHealth", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (field != null)
        {
            float current = (float)field.GetValue(playerHealthComponent);
            if (current < minHealth)
            {
                field.SetValue(playerHealthComponent, minHealth);
                // Use existing method name:
                var updateMethod = playerHealthComponent.GetType().GetMethod("UpdateHealthUI", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (updateMethod != null)
                    updateMethod.Invoke(playerHealthComponent, null);
            }
        }

        // Apply ice slow effect to player movement
        if (playerController != null)
            StartCoroutine(ApplyIceSlowToPlayer());

        // Spawn ice attack effect at player position
        if (iceAttackEffectPrefab != null && playerRoot != null)
        {
            GameObject effect = Instantiate(iceAttackEffectPrefab, playerRoot.position, Quaternion.identity);

            float scaleFactor = 0.02f;
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

            Destroy(effect, 2f);
        }

        Debug.Log("Ghost attacked player for " + ghostAttackDamage + " damage!");
    }

    private IEnumerator ApplyIceSlowToPlayer()
    {
        if (playerController == null) yield break;

        // Store original values
        float originalMoveSpeed = playerController.MoveSpeed;
        float originalSprintSpeed = playerController.SprintSpeed;
        float originalRotationSpeed = playerController.RotationSpeed;

        // Apply slow effect
        playerController.MoveSpeed *= slowMultiplier;
        playerController.SprintSpeed *= slowMultiplier;
        playerController.RotationSpeed *= slowMultiplier;

        Debug.Log($"Player slowed! Move: {playerController.MoveSpeed}, Sprint: {playerController.SprintSpeed}, Rotation: {playerController.RotationSpeed}");

        yield return new WaitForSeconds(slowDuration);

        // Restore values safely
        playerController.MoveSpeed = originalMoveSpeed;
        playerController.SprintSpeed = originalSprintSpeed;
        playerController.RotationSpeed = originalRotationSpeed;

        Debug.Log("Player speed restored!");
    }
    #endregion

    #region Shooting
    private void HandleShootingInput()
    {
        if (Mouse.current == null) return;

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            if (!isFiring)
            {
                isFiring = true;
                if (fireRoutine == null)
                    fireRoutine = StartCoroutine(FireRoutine());
            }
        }

        if (Mouse.current.leftButton.wasReleasedThisFrame)
        {
            isFiring = false;
        }
    }

    private IEnumerator FireRoutine()
    {
        while (isFiring)
        {
            if (currentMana >= manaPerShot)
            {
                Shoot();
                currentMana -= manaPerShot;
                UpdateManaBar();
                lastShotTime = Time.time;
                if (rechargeCoroutine != null)
                {
                    StopCoroutine(rechargeCoroutine);
                    rechargeCoroutine = null;
                }
            }

            if (currentMana < manaPerShot && rechargeCoroutine == null)
                rechargeCoroutine = StartCoroutine(HoldRecharge());

            yield return new WaitForSeconds(rapidFireRate);
        }

        if (rechargeCoroutine == null)
            rechargeCoroutine = StartCoroutine(FastRechargeAfterDelay());

        fireRoutine = null;
    }

    private IEnumerator HoldRecharge()
    {
        while (isFiring && currentMana < maxMana)
        {
            currentMana += slowRechargeRate * Time.deltaTime;
            currentMana = Mathf.Min(currentMana, maxMana);
            UpdateManaBar();
            yield return null;
        }
        rechargeCoroutine = null;
    }

    private IEnumerator FastRechargeAfterDelay()
    {
        float waitTime = Mathf.Max(0f, rechargeDelay - (Time.time - lastShotTime));
        if (waitTime > 0f) yield return new WaitForSeconds(waitTime);

        while (!isFiring && currentMana < maxMana)
        {
            currentMana += fastRechargeRate * Time.deltaTime;
            currentMana = Mathf.Min(currentMana, maxMana);
            UpdateManaBar();
            yield return null;
        }
        rechargeCoroutine = null;
    }

    private void Shoot()
    {
        if (playerCamera == null || spellPrefab == null) return;

        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        if (Physics.Raycast(ray, out RaycastHit hit, shootingRange, hitMask, rayQueryTrigger))
        {
            SpawnSpellAt(hit.point, Quaternion.identity);

            if (spawnedGhost != null && hit.collider.transform.IsChildOf(spawnedGhost.transform))
            {
                DamageGhost(1f);
            }
        }
        else
        {
            Vector3 missPoint = ray.origin + ray.direction * shootingRange;
            SpawnSpellAt(missPoint, Quaternion.LookRotation(ray.direction));
        }
    }

    private void SpawnSpellAt(Vector3 pos, Quaternion rot)
    {
        GameObject go = Instantiate(spellPrefab, pos, rot);

        float scaleFactor = 0.05f;
        go.transform.localScale = Vector3.one * scaleFactor;

        foreach (var ps in go.GetComponentsInChildren<ParticleSystem>())
        {
            var main = ps.main;
            main.startSizeMultiplier *= scaleFactor;
            main.startSpeedMultiplier *= scaleFactor;
            main.gravityModifierMultiplier *= scaleFactor;

            var shape = ps.shape;
            if (shape.enabled)
                shape.radius *= scaleFactor;
        }

        Destroy(go, burstLifetime);
    }
    #endregion

    #region UI & Bars
    private void UpdateManaBar()
    {
        if (manaSlider != null) manaSlider.value = currentMana / maxMana;
    }

    private void UpdateHealthBar()
    {
        if (healthSlider != null) healthSlider.value = currentHealth / maxHealth;
    }
    #endregion

    #region Tutorial Sequence
    private IEnumerator RunTutorial()
    {
        yield return StartCoroutine(Fade(1f, 0f, 2f));

        // Walking tutorial
        if (!string.IsNullOrEmpty(walkingTutorialDialogue))
            yield return StartCoroutine(ShowDialogue("Cat", walkingTutorialDialogue));

        yield return StartCoroutine(WaitForMovement(2f));

        // Shooting tutorial
        if (!string.IsNullOrEmpty(shootingTutorialDialogue))
            yield return StartCoroutine(ShowDialogue("Cat", shootingTutorialDialogue));
        if (combatUIParent != null) combatUIParent.SetActive(true);
        allowShooting = true;
        yield return StartCoroutine(WaitForPractice());

        // ------------------ Ghost Encounter ------------------
        if (ghostPrefab != null)
        {
            // Initialize ghost
            spawnedGhost = ghostPrefab;
            InitGhost(spawnedGhost);
            
            // Remove any existing GhostMoveAndAttack component
            var oldAttack = spawnedGhost.GetComponent<GhostMoveAndAttack>();
            if (oldAttack != null) Destroy(oldAttack);

            // Activate the ghost
            spawnedGhost.SetActive(true);

            // Wait until the ghost is visible to the player camera
            yield return new WaitUntil(() => GhostVisibleToCamera(spawnedGhost));

            // Show ghost encounter dialogue
            if (!string.IsNullOrEmpty(ghostEncounterDialogue))
                yield return StartCoroutine(ShowDialogue("Cat", ghostEncounterDialogue, false));

            // Enable player shooting during ghost encounter
            allowShooting = true;
        }

        yield return new WaitUntil(() => spawnedGhost == null);

        if (spawnedPotion != null && !string.IsNullOrEmpty(potionDropDialogue))
        {
            yield return StartCoroutine(ShowDialogue("Cat", potionDropDialogue, false));
        }

        yield return StartCoroutine(HandlePotionPickup());

        if (!string.IsNullOrEmpty(healthRestoredDialogue))
            yield return StartCoroutine(ShowDialogue("Cat", healthRestoredDialogue));
        if (!string.IsNullOrEmpty(guardingDutyDialogue))
            yield return StartCoroutine(ShowDialogue("Cat", guardingDutyDialogue));
        if (!string.IsNullOrEmpty(keepEyesPeeledDialogue))
            yield return StartCoroutine(ShowDialogue("Cat", keepEyesPeeledDialogue));

        yield return StartCoroutine(Fade(0f, 1f, 2f));
        SceneManager.LoadScene("Level 1");
    }

    private IEnumerator WaitForMovement(float requiredDistance = 2f)
    {
        Vector3 startPos = playerRoot.position;
        while (Vector3.Distance(startPos, playerRoot.position) < requiredDistance)
            yield return null;
    }
    #endregion

    #region Ghost Handling
    private void InitGhost(GameObject ghost)
    {
        ghostMaxHealth = Random.Range(2f, 4f);
        ghostCurrentHealth = ghostMaxHealth;

        ghostHealthBar = ghost.GetComponentInChildren<Slider>();
        if (ghostHealthBar != null)
        {
            ghostHealthBar.maxValue = 1f;
            ghostHealthBar.value = 1f;
        }
        
        ghostLastAttackTime = -999f; // Reset attack timer
    }

    private void DamageGhost(float amount)
    {
        ghostCurrentHealth -= amount;
        if (ghostCurrentHealth < 0) ghostCurrentHealth = 0;

        if (ghostHealthBar != null)
            ghostHealthBar.value = ghostCurrentHealth / ghostMaxHealth;

        if (burnCoroutine != null)
            StopCoroutine(burnCoroutine);
        burnCoroutine = StartCoroutine(ApplyBurn());

        if (ghostCurrentHealth <= 0f)
            KillGhost();
    }

    private IEnumerator ApplyBurn()
    {
        float elapsed = 0f;
        while (elapsed < burnDuration && spawnedGhost != null)
        {
            ghostCurrentHealth -= burnDamagePerSecond * Time.deltaTime;
            if (ghostCurrentHealth < 0f) ghostCurrentHealth = 0;

            if (ghostHealthBar != null)
                ghostHealthBar.value = ghostCurrentHealth / ghostMaxHealth;

            if (ghostCurrentHealth <= 0f)
            {
                KillGhost();
                yield break;
            }

            elapsed += Time.deltaTime;
            yield return null;
        }
        burnCoroutine = null;
    }

    private void KillGhost()
    {
        if (ghostSmokeFX != null && spawnedGhost != null)
        {
            GameObject fx = Instantiate(
                ghostSmokeFX,
                spawnedGhost.transform.position,
                ghostSmokeFX.transform.rotation
            );
            fx.transform.localScale = Vector3.one * ghostSmokeFX.transform.localScale.x;
        }

        if (healthPotionPrefab != null && spawnedGhost != null)
        {
            spawnedPotion = Instantiate(healthPotionPrefab, spawnedGhost.transform.position, Quaternion.identity);
        }

        Destroy(spawnedGhost);
        spawnedGhost = null;
    }

    private bool GhostVisibleToCamera(GameObject ghost)
    {
        if (ghost == null || playerCamera == null) return false;
        Vector3 viewPos = playerCamera.WorldToViewportPoint(ghost.transform.position);
        return viewPos.z > 0 && viewPos.x > 0 && viewPos.x < 1 && viewPos.y > 0 && viewPos.y < 1;
    }
    #endregion

    #region Dialogue & Fade
    private IEnumerator ShowDialogue(string character, string text, bool hideCombatUI = true)
    {
        bool prevShoot = allowShooting;
        allowShooting = false;
        isFiring = false;

        if (hideCombatUI && combatUIParent != null)
            combatUIParent.SetActive(false);

        if (dialogueUI != null) dialogueUI.SetActive(true);
        if (characterNameText != null) characterNameText.text = character;
        if (dialogueText != null)
        {
            dialogueText.text = "";
            foreach (char c in text)
            {
                dialogueText.text += c;
                yield return new WaitForSeconds(0.03f);
            }
        }

        yield return new WaitUntil(() =>
            (Keyboard.current != null && (
                (Keyboard.current.enterKey != null && Keyboard.current.enterKey.wasPressedThisFrame) ||
                (Keyboard.current.numpadEnterKey != null && Keyboard.current.numpadEnterKey.wasPressedThisFrame)
            )) ||
            (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        );
        if (dialogueUI != null) dialogueUI.SetActive(false);
        allowShooting = prevShoot;
    }

    private IEnumerator Fade(float from, float to, float duration)
    {
        if (fadePanel == null) yield break;

        float elapsed = 0f;
        Color c = fadePanel.color;
        c.a = from;
        fadePanel.color = c;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            c.a = Mathf.Lerp(from, to, elapsed / duration);
            fadePanel.color = c;
            yield return null;
        }
        c.a = to;
        fadePanel.color = c;
    }
    #endregion

    #region Potion Pickup
    private IEnumerator HandlePotionPickup()
    {
        if (spawnedPotion == null) yield break;

        bool drank = false;

        while (!drank)
        {
            if (interactText != null)
            {
                Vector3 screenPos = playerCamera.WorldToScreenPoint(spawnedPotion.transform.position + Vector3.up * 0.2f);
                if (screenPos.z > 0)
                {
                    interactText.gameObject.SetActive(true);
                    interactText.text = "Press E to Drink";
                    interactText.transform.position = screenPos;
                }
                else
                {
                    interactText.gameObject.SetActive(false);
                }
            }

            if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
            {
                float dist = Vector3.Distance(playerRoot.position, spawnedPotion.transform.position);
                if (dist <= interactDistance)
                {
                    currentHealth = maxHealth;
                    UpdateHealthBar();
                    Destroy(spawnedPotion);
                    spawnedPotion = null;
                    drank = true;
                    if (interactText != null) interactText.gameObject.SetActive(false);
                }
            }

            yield return null;
        }
    }
    #endregion

    #region Spell Practice Helper
    private IEnumerator WaitForPractice()
    {
        bool firedOnce = false;
        bool held = false;
        float holdTimer = 0f;

        while (!(firedOnce && held))
        {
            if (Mouse.current != null)
            {
                if (Mouse.current.leftButton.wasPressedThisFrame && !firedOnce)
                    firedOnce = true;

                if (Mouse.current.leftButton.isPressed)
                {
                    holdTimer += Time.deltaTime;
                    if (holdTimer >= 0.5f && !held)
                        held = true;
                }
                else holdTimer = 0f;
            }
            yield return null;
        }
    }
    #endregion

    public void AddMana(float amount)
    {
        currentMana = Mathf.Min(currentMana + amount, maxMana);
        UpdateManaBar();
    }
}