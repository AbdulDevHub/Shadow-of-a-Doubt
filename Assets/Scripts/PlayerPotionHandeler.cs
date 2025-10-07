using UnityEngine;
using TMPro;
using System.Collections;
using UnityEngine.InputSystem;

public class PlayerPotionHandler : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TextMeshProUGUI interactText;
    [SerializeField] private float interactDistance = 4f;

    [Header("Camera Reference")]
    [SerializeField] private Camera playerCamera;

    [Header("Potion Prefabs (for identification)")]
    public GameObject healthPotion30Prefab;
    public GameObject ultimateHealthPotionPrefab;
    public GameObject manaPotion30Prefab;
    public GameObject ultimateManaPotionPrefab;

    [Header("References")]
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private WandShooter wandShooter;

    [Header("Lock Images (for ultimate potions)")]
    [SerializeField] private GameObject healthLockImage;
    [SerializeField] private GameObject manaLockImage;

    private GameObject lookedAtPotion;

    private void Start()
    {
        if (playerHealth == null)
            Debug.LogError("PlayerHealth reference not set in PlayerPotionHandler!");

        if (interactText != null) interactText.gameObject.SetActive(false);
        if (healthLockImage != null) healthLockImage.SetActive(false);
        if (manaLockImage != null) manaLockImage.SetActive(false);
    }

    private void Update()
    {
        HandlePotionLook();
    }

    private void HandlePotionLook()
    {
        lookedAtPotion = FindClosestVisiblePotion();

        if (lookedAtPotion != null)
        {
            if (interactText != null)
            {
                Vector3 worldPos = lookedAtPotion.transform.position + Vector3.up * 0.2f;
                Vector3 screenPos = playerCamera.WorldToScreenPoint(worldPos);

                if (screenPos.z > 0)
                {
                    interactText.transform.position = screenPos;
                    interactText.gameObject.SetActive(true);
                    interactText.text = "Press E to Drink";
                }
                else
                {
                    interactText.gameObject.SetActive(false);
                    return;
                }
            }

            if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
            {
                DrinkPotion(lookedAtPotion);
            }
        }
        else
        {
            if (interactText != null)
                interactText.gameObject.SetActive(false);
        }
    }

    private GameObject FindClosestVisiblePotion()
    {
        GameObject[] potions = CombineArrays(
            GameObject.FindGameObjectsWithTag("HealthPotion30"),
            GameObject.FindGameObjectsWithTag("UltimateHealthPotion"),
            GameObject.FindGameObjectsWithTag("ManaPotion30"),
            GameObject.FindGameObjectsWithTag("UltimateManaPotion")
        );

        GameObject closest = null;
        float closestDist = Mathf.Infinity;

        foreach (var potion in potions)
        {
            float dist = Vector3.Distance(transform.position, potion.transform.position);
            if (dist > interactDistance) continue;

            Renderer rend = potion.GetComponentInChildren<Renderer>();
            if (rend == null || !rend.isVisible) continue;

            if (dist < closestDist)
            {
                closest = potion;
                closestDist = dist;
            }
        }

        return closest;
    }

    private GameObject[] CombineArrays(params GameObject[][] arrays)
    {
        int total = 0;
        foreach (var arr in arrays) total += arr.Length;

        GameObject[] result = new GameObject[total];
        int index = 0;
        foreach (var arr in arrays)
            foreach (var item in arr)
                result[index++] = item;

        return result;
    }

    private void DrinkPotion(GameObject potion)
    {
        if (potion.CompareTag("HealthPotion30"))
            ApplyHealthPotion(0.3f);
        else if (potion.CompareTag("UltimateHealthPotion"))
            StartCoroutine(ApplyUltimateHealthPotion());
        else if (potion.CompareTag("ManaPotion30"))
            ApplyManaPotion(0.3f);
        else if (potion.CompareTag("UltimateManaPotion"))
            StartCoroutine(ApplyUltimateManaPotion());

        Destroy(potion);

        if (interactText != null)
            interactText.gameObject.SetActive(false);
    }

    private void ApplyHealthPotion(float percentage)
    {
        if (playerHealth == null || playerHealth.isHealthLocked) return;

        float healAmount = playerHealth.maxHealth * percentage;
        playerHealth.Heal(healAmount); // Health UI updates automatically
    }

    private IEnumerator ApplyUltimateHealthPotion()
    {
        if (playerHealth == null) yield break;

        playerHealth.isHealthLocked = true;
        playerHealth.Heal(playerHealth.maxHealth);

        if (healthLockImage != null) healthLockImage.SetActive(true);

        yield return new WaitForSeconds(10f);

        playerHealth.isHealthLocked = false;
        if (healthLockImage != null) healthLockImage.SetActive(false);
    }

    private void ApplyManaPotion(float percentage)
    {
        if (wandShooter == null) return;

        float manaToAdd = wandShooter.maxMana * percentage;
        wandShooter.AddMana(manaToAdd);
    }

    private IEnumerator ApplyUltimateManaPotion()
    {
        if (wandShooter == null) yield break;

        wandShooter.isManaLocked = true;
        wandShooter.AddMana(wandShooter.maxMana);

        if (manaLockImage != null) manaLockImage.SetActive(true);

        yield return new WaitForSeconds(3f);

        wandShooter.isManaLocked = false;
        if (manaLockImage != null) manaLockImage.SetActive(false);
    }
}
