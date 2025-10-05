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
    public GameObject[] wands;
    [Tooltip("Assign spell prefabs here. Index must match the wand index.")]
    public GameObject[] spellPrefabs = new GameObject[3];
    [SerializeField] private float burstLifetime = 2f;
    private int currentIndex = 0;

    [Header("Mana")]
    public Slider manaBar;
    public float maxMana = 10f;
    private float currentMana;
    public float rapidFireRate = 0.2f;
    public float slowRechargeRate = 1f;
    public float fastRechargeRate = 5f;
    public float rechargeDelay = 2f;

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
        SelectWand(0); // Start with Fire wand
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
        if (Keyboard.current.digit1Key.wasPressedThisFrame) SelectWand(0);
        if (Keyboard.current.digit2Key.wasPressedThisFrame) SelectWand(1);
        if (Keyboard.current.digit3Key.wasPressedThisFrame) SelectWand(2);
    }

    private void SelectWand(int index)
    {
        if (index < 0 || index >= wands.Length) return;

        currentIndex = index;

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
            if (currentMana >= 1f)
            {
                Shoot();
                currentMana -= 1f;
                lastShotTime = Time.time;

                if (rechargeCoroutine != null)
                {
                    StopCoroutine(rechargeCoroutine);
                    rechargeCoroutine = null;
                }
            }

            if (currentMana < 1f && rechargeCoroutine == null)
                rechargeCoroutine = StartCoroutine(HoldRecharge());

            yield return new WaitForSeconds(rapidFireRate);
        }

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
        if (spellPrefabs.Length == 0 || spellPrefabs[currentIndex] == null)
            return;

        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

        if (Physics.Raycast(ray, out RaycastHit hit, shootingRange, hitMask, QueryTriggerInteraction.Ignore))
        {
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
        Destroy(spell, burstLifetime);
    }

    private void UpdateManaBar()
    {
        if (manaBar != null)
            manaBar.value = currentMana / maxMana;
    }

    public void AddMana(float amount)
    {
        currentMana = Mathf.Min(currentMana + amount, maxMana);
        UpdateManaBar();
    }
}
