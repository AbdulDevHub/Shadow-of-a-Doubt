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

    [Header("Spells")]
    [Tooltip("Assign up to 3 spell prefabs here (slot 0 = key 1, slot 1 = key 2, slot 2 = key 3).")]
    public GameObject[] spellPrefabs = new GameObject[3];
    [SerializeField] private float burstLifetime = 2f;
    private int currentSpellIndex = 0;

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

    private bool isFiring = false;
    private float lastShotTime;
    private Coroutine rechargeCoroutine;

    private void Start()
    {
        currentMana = maxMana;
        UpdateManaBar();
    }

    private void Update()
    {
        HandleFiringInput();
        HandleSpellSwitching();
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

    private void HandleSpellSwitching()
    {
        if (Keyboard.current.digit1Key.wasPressedThisFrame) currentSpellIndex = 0;
        if (Keyboard.current.digit2Key.wasPressedThisFrame && spellPrefabs.Length > 1) currentSpellIndex = 1;
        if (Keyboard.current.digit3Key.wasPressedThisFrame && spellPrefabs.Length > 2) currentSpellIndex = 2;
    }

    private IEnumerator FireRoutine()
    {
        while (isFiring)
        {
            if (currentMana >= 1f)
            {
                Shoot();
                currentMana -= 1f;
                lastShotTime = Time.time;

                // Stop any recharge while firing
                if (rechargeCoroutine != null)
                {
                    StopCoroutine(rechargeCoroutine);
                    rechargeCoroutine = null;
                }
            }

            // If mana is 0, allow slow incremental recharge while holding
            if (currentMana < 1f)
            {
                if (rechargeCoroutine == null)
                    rechargeCoroutine = StartCoroutine(HoldRecharge());
            }

            yield return new WaitForSeconds(rapidFireRate);
        }

        // When player stops firing, start fast recharge if mana < max
        if (rechargeCoroutine == null)
            rechargeCoroutine = StartCoroutine(FastRechargeAfterDelay());
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
        if (spellPrefabs.Length == 0 || spellPrefabs[currentSpellIndex] == null)
            return;

        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

        if (Physics.Raycast(ray, out RaycastHit hit, shootingRange, hitMask, QueryTriggerInteraction.Ignore))
        {
            GhostHealth ghost = hit.collider.GetComponentInParent<GhostHealth>()
                              ?? hit.collider.GetComponentInChildren<GhostHealth>();

            if (ghost != null)
            {
                SpawnSpell(hit.point, Quaternion.identity);
                ghost.TakeDamage(1f);
                return;
            }

            SpawnSpell(hit.point, Quaternion.LookRotation(ray.direction));
        }
        else
        {
            // Spawn effect far along the ray if nothing hit
            Vector3 missPoint = ray.origin + ray.direction * shootingRange;
            SpawnSpell(missPoint, Quaternion.LookRotation(ray.direction));
        }
    }

    private void SpawnSpell(Vector3 position, Quaternion rotation)
    {
        GameObject prefab = spellPrefabs[currentSpellIndex];
        if (prefab == null) return;

        GameObject spell = Instantiate(prefab, position, rotation);
        Destroy(spell, burstLifetime);
    }

    private void UpdateManaBar()
    {
        if (manaBar != null)
            manaBar.value = currentMana / maxMana;
    }
}
