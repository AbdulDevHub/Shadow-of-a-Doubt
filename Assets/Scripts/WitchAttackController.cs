using UnityEngine;
using System.Collections;

public class WitchAttackController : MonoBehaviour
{
    [Header("Attack Settings")]
    public float initialDelay = 2f;       // Delay before first attack
    public float attackCooldown = 5f;     // Time between attacks

    private bool canAttack = false;
    private bool isTeleporting = false;
    private WitchShield shield;

    [Header("Ghost Spawning")]
    public GhostSpawnerMaster ghostSpawner;        // Assign in Inspector
    public GameObject ghostSummonEffectPrefab;     // Visual cue for ghost summon

    [Header("Crystal Attack Settings")]
    public Transform redCircleParent;              // Parent containing all red circle objects
    public GameObject crystalEffectPrefab;         // Prefab for crystal effect
    public float crystalDelay = 2f;                // Time between circle and crystal
    public int minCrystalsPerAttack = 2;           // Minimum number of crystals per attack
    public int maxCrystalsPerAttack = 5;           // Maximum number of crystals per attack

    private GameObject[] redCircles;               // Array of all red circle gameObjects

    [Header("Teleport Settings")]
    public Transform teleportPointsParent;         // Empty object with teleport children
    public GameObject teleportEffectPrefab;        // Visual effect prefab
    public float proximityThreshold = 3f;          // Distance player must be within
    public float requiredStayTime = 4f;            // Time player must stay close
    public float teleportEffectDuration = 2f;      // Time effect plays before teleport
    public Transform player;                       // Assign player transform

    private Transform[] teleportPoints;
    private int currentTeleportIndex = 0;
    private float playerStayTimer = 0f;

    private void Awake()
    {
        shield = GetComponentInChildren<WitchShield>(true);

        // Cache circles
        if (redCircleParent != null)
        {
            int count = redCircleParent.childCount;
            redCircles = new GameObject[count];
            for (int i = 0; i < count; i++)
            {
                redCircles[i] = redCircleParent.GetChild(i).gameObject;
                redCircles[i].SetActive(false);
            }
        }

        // Cache teleport points
        if (teleportPointsParent != null)
        {
            int count = teleportPointsParent.childCount;
            teleportPoints = new Transform[count];
            for (int i = 0; i < count; i++)
            {
                teleportPoints[i] = teleportPointsParent.GetChild(i);
            }
        }
    }

    private void Start()
    {
        // Place witch at first teleport point
        if (teleportPoints != null && teleportPoints.Length > 0)
        {
            currentTeleportIndex = 0;
            transform.position = teleportPoints[0].position;
        }
    }

    private void Update()
    {
        if (teleportPoints == null || teleportPoints.Length == 0 || player == null) return;
        if (isTeleporting || !canAttack) return; // Don't teleport during attacks

        // Check if player is near a teleport point (not current one)
        for (int i = 0; i < teleportPoints.Length; i++)
        {
            if (i == currentTeleportIndex) continue; // skip current point

            float dist = Vector3.Distance(player.position, teleportPoints[i].position);
            if (dist <= proximityThreshold)
            {
                playerStayTimer += Time.deltaTime;
                if (playerStayTimer >= requiredStayTime)
                {
                    StartCoroutine(TeleportRoutine(i));
                    playerStayTimer = 0f;
                }
                return;
            }
        }

        // Reset timer if player leaves
        playerStayTimer = 0f;
    }

    public void StartAttacks()
    {
        canAttack = true;
        StartCoroutine(AttackRoutine());
        Debug.Log("Witch attacks have started!");
    }

    public void StopAttacks()
    {
        canAttack = false;
        StopAllCoroutines();
        Debug.Log("Witch stopped attacking.");

        if (redCircles != null)
        {
            foreach (var circle in redCircles)
                if (circle != null) circle.SetActive(false);
        }
    }

    private IEnumerator AttackRoutine()
    {
        yield return new WaitForSeconds(initialDelay);

        while (canAttack)
        {
            // If teleporting, just wait and retry
            if (isTeleporting)
            {
                yield return null;
                continue;
            }

            int attackChoice = Random.Range(0, 3);

            switch (attackChoice)
            {
                case 0: // Shield
                    if (shield != null)
                        shield.ActivateShield();
                    break;

                case 1: // Ghost summon
                    yield return StartCoroutine(PerformGhostSummon());
                    break;

                case 2: // Crystal attack
                    yield return StartCoroutine(PerformCrystalAttack());
                    break;
            }

            yield return new WaitForSeconds(attackCooldown);
        }
    }

    private IEnumerator PerformGhostSummon()
    {
        if (ghostSummonEffectPrefab != null)
        {
            GameObject effect = Instantiate(
                ghostSummonEffectPrefab,
                transform.position,
                ghostSummonEffectPrefab.transform.rotation
            );

            effect.transform.localScale = Vector3.one * ghostSummonEffectPrefab.transform.localScale.x;

            float effectDuration = 2f;
            Destroy(effect, effectDuration);
            yield return new WaitForSeconds(effectDuration);
        }

        if (ghostSpawner != null && ghostSpawner.waves.Count > 0)
        {
            StartCoroutine(ghostSpawner.SpawnWave(ghostSpawner.waves[0]));
        }
    }

    private IEnumerator PerformCrystalAttack()
    {
        if (redCircles == null || redCircles.Length == 0) yield break;

        int numCrystals = Random.Range(minCrystalsPerAttack, maxCrystalsPerAttack + 1);

        for (int i = 0; i < numCrystals; i++)
        {
            GameObject chosenCircle = redCircles[Random.Range(0, redCircles.Length)];
            chosenCircle.SetActive(true);
            StartCoroutine(FlashCircle(chosenCircle, 0.2f));
            StartCoroutine(SpawnCrystalDelayed(chosenCircle));
        }

        yield return new WaitForSeconds(crystalDelay + 0.1f);
    }

    private IEnumerator SpawnCrystalDelayed(GameObject circle)
    {
        yield return new WaitForSeconds(crystalDelay);

        if (circle == null) yield break;

        // --- DAMAGE CHECK BEFORE CRYSTAL SPAWNS ---
        if (player != null)
        {
            float circleRadius = 1.5f; // Adjust to match red circle size
            float dist = Vector3.Distance(player.position, circle.transform.position);

            if (dist <= circleRadius)
            {
                PlayerHealth ph = player.GetComponent<PlayerHealth>();
                if (ph != null)
                {
                    ph.TakeDamage(5f); // 💥 Deal 5 damage
                    Debug.Log("Player took 5 damage from crystal attack!");
                }
            }
        }
        // --- END DAMAGE CHECK ---

        circle.SetActive(false);

        if (crystalEffectPrefab != null)
        {
            GameObject crystal = Instantiate(crystalEffectPrefab, circle.transform.position, Quaternion.identity);
            crystal.transform.localScale = circle.transform.localScale;
        }
    }

    private IEnumerator FlashCircle(GameObject circle, float flashSpeed)
    {
        if (circle == null) yield break;

        Renderer rend = circle.GetComponent<Renderer>();
        if (rend == null) yield break;

        while (circle.activeSelf)
        {
            rend.enabled = !rend.enabled;
            yield return new WaitForSeconds(flashSpeed);
        }

        rend.enabled = true;
    }

    private IEnumerator TeleportRoutine(int targetIndex)
    {
        isTeleporting = true;

        Vector3 fromPos = teleportPoints[currentTeleportIndex].position;
        Vector3 toPos = teleportPoints[targetIndex].position;

        if (teleportEffectPrefab != null)
        {
            GameObject effect1 = Instantiate(teleportEffectPrefab, fromPos, Quaternion.identity);
            GameObject effect2 = Instantiate(teleportEffectPrefab, toPos, Quaternion.identity);
            Destroy(effect1, teleportEffectDuration);
            Destroy(effect2, teleportEffectDuration);
        }

        yield return new WaitForSeconds(teleportEffectDuration);

        transform.position = toPos;
        currentTeleportIndex = targetIndex;

        isTeleporting = false;
    }
}
