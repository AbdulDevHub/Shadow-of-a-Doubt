using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

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

    // 🔹 Burn system (added)
    private Coroutine burnCoroutine;
    public float burnDamagePerSecond = 0.5f;
    public float burnDuration = 3f;

    // Potion state
    private GameObject spawnedPotion = null;

    private void Awake()
    {
        if (playerCamera == null)
            playerCamera = Camera.main;
    }

    private void Start()
    {
        currentMana = maxMana;
        UpdateManaBar();

        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);
        UpdateHealthBar();

        if (dialogueUI != null) dialogueUI.SetActive(false);
        if (combatUIParent != null) combatUIParent.SetActive(false);

        if (ghostPrefab != null)
            ghostPrefab.SetActive(false);

        if (interactText != null)
            interactText.gameObject.SetActive(false);

        StartCoroutine(RunTutorial());
    }

    private void Update()
    {
        if (allowShooting)
            HandleShootingInput();
    }

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

        // Wait until the player moves a little
        yield return StartCoroutine(WaitForMovement(2f)); // player must walk 2 units

        // Shooting tutorial
        if (!string.IsNullOrEmpty(shootingTutorialDialogue))
            yield return StartCoroutine(ShowDialogue("Cat", shootingTutorialDialogue));
        if (combatUIParent != null) combatUIParent.SetActive(true);
        allowShooting = true;
        yield return StartCoroutine(WaitForPractice());

        // Ghost encounter
        if (ghostPrefab != null)
        {
            ghostPrefab.SetActive(true);
            spawnedGhost = ghostPrefab;
            InitGhost(spawnedGhost);

            GhostChase chase = spawnedGhost.AddComponent<GhostChase>();
            chase.target = playerRoot;

            yield return new WaitUntil(() => GhostVisibleToCamera(spawnedGhost));

            if (!string.IsNullOrEmpty(ghostEncounterDialogue))
                yield return StartCoroutine(ShowDialogue("Cat", ghostEncounterDialogue, false));

            allowShooting = true;
        }

        // Wait until ghost dies
        yield return new WaitUntil(() => spawnedGhost == null);

        // Cat notices potion drop
        if (spawnedPotion != null && !string.IsNullOrEmpty(potionDropDialogue))
        {
            yield return StartCoroutine(ShowDialogue("Cat", potionDropDialogue, false));
        }

        // Player picks up potion
        yield return StartCoroutine(HandlePotionPickup());

        // After potion, continue tutorial dialogues
        if (!string.IsNullOrEmpty(healthRestoredDialogue))
            yield return StartCoroutine(ShowDialogue("Cat", healthRestoredDialogue));
        if (!string.IsNullOrEmpty(guardingDutyDialogue))
            yield return StartCoroutine(ShowDialogue("Cat", guardingDutyDialogue));
        if (!string.IsNullOrEmpty(keepEyesPeeledDialogue))
            yield return StartCoroutine(ShowDialogue("Cat", keepEyesPeeledDialogue));

        // Fade out and load next scene
        yield return StartCoroutine(Fade(0f, 1f, 2f));
        SceneManager.LoadScene("Level 1");
    }

    private IEnumerator WaitForMovement(float requiredDistance = 2f)
    {
        Vector3 startPos = playerRoot.position;

        while (Vector3.Distance(startPos, playerRoot.position) < requiredDistance)
        {
            yield return null;
        }
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
    }

    private void DamageGhost(float amount)
    {
        ghostCurrentHealth -= amount;
        if (ghostCurrentHealth < 0) ghostCurrentHealth = 0;

        if (ghostHealthBar != null)
            ghostHealthBar.value = ghostCurrentHealth / ghostMaxHealth;

        // 🔹 Trigger burn effect (matches GhostHealth behavior)
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
            if (ghostCurrentHealth < 0f) ghostCurrentHealth = 0f;

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
            (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame) ||
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

public class GhostChase : MonoBehaviour
{
    public Transform target;
    public float speed = 1.5f;
    public float stopDistance = 1.5f;

    private void Update()
    {
        if (target == null) return;

        Vector3 dir = (target.position - transform.position);
        float dist = dir.magnitude;

        if (dist > stopDistance)
        {
            Vector3 move = dir.normalized;
            transform.position += move * speed * Time.deltaTime;
            transform.LookAt(target);
        }
    }
}
