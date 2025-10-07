using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using System.Collections;

public class WandShooter : MonoBehaviour
{
    [Header("Camera & Shooting")]
    public Camera playerCamera;
    public float shootingRange = 100f;
    public LayerMask hitMask = ~0;

    [Header("Wands & Spells")]
    [Tooltip("Assign the wand GameObjects here (visuals/models). Only one will be active at a time.")]
    public GameObject[] wands;
    [Tooltip("Assign spell prefabs here. Index must match the wand index.")]
    public GameObject[] spellPrefabs = new GameObject[3];
    [SerializeField] private float burstLifetime = 2f;
    private int currentIndex = 0;

    [Header("Mana")]
    public Slider manaBar;
    public float maxMana = 10f;
    private float currentMana;

    [Tooltip("Seconds per shot when holding fire.")]
    public float rapidFireRate = 0.2f;
    [Tooltip("Mana per second while holding at 0.")]
    public float slowRechargeRate = 1f;
    [Tooltip("Mana per second after idle.")]
    public float fastRechargeRate = 5f;
    [Tooltip("Delay before fast recharge begins.")]
    public float rechargeDelay = 2f;

    [Header("Mana Lock")]
    public bool isManaLocked = false; // Prevents mana use during ultimate mana potion

    [Header("UI")]
    public GameObject fireOverlay;
    public GameObject waterOverlay;
    public GameObject windOverlay;
    private GameObject[] spellOverlays;

    private bool isFiring = false;
    private float lastShotTime;
    private Coroutine rechargeCoroutine;

    private void Awake()
    {
        spellOverlays = new GameObject[] { fireOverlay, waterOverlay, windOverlay };
    }

    private void Start()
    {
        currentMana = maxMana;
        UpdateManaBar();
        SelectWand(0); // Start with first wand
    }

    private void Update()
    {
        HandleFiringInput();
        HandleWandSwitching();
        UpdateManaBar();
    }

    private void HandleFiringInput()
    {
        if (Mouse.current == null) return;

        if (Mouse.current.leftButton.isPressed)
        {
            if (!isFiring)
            {
                isFiring = true;
                StartCoroutine(FireRoutine());
            }
        }
        else
        {
            isFiring = false;
        }
    }

    private void HandleWandSwitching()
    {
        // Cycle through wands when pressing Q
        if (Keyboard.current.qKey.wasPressedThisFrame)
        {
            int nextIndex = (currentIndex + 1) % wands.Length;
            SelectWand(nextIndex);
        }
    }

    private void SelectWand(int index)
    {
        if (index < 0 || index >= wands.Length) return;

        currentIndex = index;

        // Activate only the selected wand
        for (int i = 0; i < wands.Length; i++)
        {
            if (wands[i] != null)
                wands[i].SetActive(i == index);

            if (spellOverlays[i] != null)
                spellOverlays[i].SetActive(i != index);
        }
    }

    private IEnumerator FireRoutine()
    {
        while (isFiring)
        {
            // Allow shooting if you have at least 1 mana
            if (currentMana >= 1f)
            {
                Shoot();

                // Only consume mana if not locked by Ultimate Mana Potion
                if (!isManaLocked)
                {
                    currentMana -= 1f;
                }

                lastShotTime = Time.time;

                // Stop any ongoing recharge while firing
                if (rechargeCoroutine != null)
                {
                    StopCoroutine(rechargeCoroutine);
                    rechargeCoroutine = null;
                }
            }

            // If mana is low and not locked, start slow recharge while holding
            if ((!isManaLocked && currentMana < 1f) && rechargeCoroutine == null)
            {
                rechargeCoroutine = StartCoroutine(HoldRecharge());
            }

            yield return new WaitForSeconds(rapidFireRate);
        }

        // After releasing fire, start fast recharge after delay (if not locked)
        if (rechargeCoroutine == null && !isManaLocked)
        {
            rechargeCoroutine = StartCoroutine(FastRechargeAfterDelay());
        }
    }

    private IEnumerator HoldRecharge()
    {
        while (isFiring && currentMana < maxMana)
        {
            currentMana += slowRechargeRate * Time.deltaTime;
            currentMana = Mathf.Min(currentMana, maxMana);
            yield return null;
        }
        rechargeCoroutine = null;
    }

    private IEnumerator FastRechargeAfterDelay()
    {
        float waitTime = Mathf.Max(0f, rechargeDelay - (Time.time - lastShotTime));
        if (waitTime > 0f)
            yield return new WaitForSeconds(waitTime);

        while (!isFiring && currentMana < maxMana)
        {
            currentMana += fastRechargeRate * Time.deltaTime;
            currentMana = Mathf.Min(currentMana, maxMana);
            yield return null;
        }
        rechargeCoroutine = null;
    }

    private void Shoot()
    {
        if (spellPrefabs.Length == 0 || spellPrefabs[currentIndex] == null)
            return;

        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

        if (Physics.Raycast(ray, out RaycastHit hit, shootingRange, hitMask, QueryTriggerInteraction.Ignore))
        {
            // Check for GhostHealth
            GhostHealth ghost = hit.collider.GetComponentInParent<GhostHealth>()
                            ?? hit.collider.GetComponentInChildren<GhostHealth>();

            // Spell type based on wand index
            ElementType spellType = (ElementType)currentIndex; // 0=Fire, 1=Water, 2=Wind

            if (ghost != null)
            {
                SpawnSpell(hit.point, Quaternion.identity);
                ghost.ApplySpellHit(spellType, hit.point);
                return;
            }

            WitchHealth witch = hit.collider.GetComponentInParent<WitchHealth>()
                ?? hit.collider.GetComponentInChildren<WitchHealth>();

            if (witch != null)
            {
                SpawnSpell(hit.point, Quaternion.identity);
                witch.TakeDamage(1f, currentIndex);
                return;
            }

            // ✅ NEW: Apply physics force to movable objects
            Rigidbody rb = hit.collider.attachedRigidbody;
            if (rb != null)
            {
                float pushForce = 5f; // You can tweak this
                rb.AddForce(ray.direction * pushForce, ForceMode.Impulse);
            }

            // If no health component, just spawn spell at hit point
            SpawnSpell(hit.point, Quaternion.LookRotation(ray.direction));
        }
        else
        {
            Vector3 missPoint = ray.origin + ray.direction * shootingRange;
            SpawnSpell(missPoint, Quaternion.LookRotation(ray.direction));
        }
    }

    private void SpawnSpell(Vector3 position, Quaternion rotation)
    {
        GameObject prefab = spellPrefabs[currentIndex];
        if (prefab == null) return;

        GameObject spell = Instantiate(prefab, position, rotation);

        // 🔹 Scale per spell type
        // Fire (0) and Water (1) are smaller; Wind (2) is full size
        float scaleFactor = (currentIndex == 0 || currentIndex == 1) ? 0.05f : 1f;

        // 🔹 Adjust internal particle systems
        foreach (var ps in spell.GetComponentsInChildren<ParticleSystem>())
        {
            var main = ps.main;
            main.startSizeMultiplier *= scaleFactor;
            main.startSpeedMultiplier *= scaleFactor;
            main.gravityModifierMultiplier *= scaleFactor;

            var shape = ps.shape;
            if (shape.enabled)
                shape.radius *= scaleFactor;
        }

        Destroy(spell, burstLifetime);
    }

    private void UpdateManaBar()
    {
        if (manaBar != null)
            manaBar.value = currentMana / maxMana;
    }

    // ✅ New method to add mana from potions
    public void AddMana(float amount)
    {
        currentMana = Mathf.Min(currentMana + amount, maxMana);
        UpdateManaBar();
    }
}
