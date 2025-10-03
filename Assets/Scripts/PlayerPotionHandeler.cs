using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class PlayerPotionHandler : MonoBehaviour
{
    [Header("Sliders")]
    [SerializeField] private Slider healthSlider;
    [SerializeField] private Slider manaSlider;

    [Header("Potion Prefabs (assign in Inspector)")]
    public GameObject healthPotion30Prefab;
    public GameObject ultimateHealthPotionPrefab;
    public GameObject manaPotion30Prefab;
    public GameObject ultimateManaPotionPrefab;

    [Header("Stats")]
    public float maxHealth = 100f;
    public float maxMana = 100f;

    private float currentHealth;
    private float currentMana;

    private bool isHealthLocked = false;
    private bool isManaLocked = false;

    private void Start()
    {
        currentHealth = maxHealth;
        currentMana = maxMana;
        UpdateSliders();
    }

    private void UpdateSliders()
    {
        if (healthSlider != null)
            healthSlider.value = currentHealth / maxHealth;

        if (manaSlider != null)
            manaSlider.value = currentMana / maxMana;
    }

    // --- Public API ---
    public void UsePotion(GameObject potionPrefab)
    {
        if (potionPrefab == null) return;

        if (potionPrefab == healthPotion30Prefab)
        {
            ApplyHealthPotion(0.3f);
        }
        else if (potionPrefab == ultimateHealthPotionPrefab)
        {
            StartCoroutine(ApplyUltimateHealthPotion());
        }
        else if (potionPrefab == manaPotion30Prefab)
        {
            ApplyManaPotion(0.3f);
        }
        else if (potionPrefab == ultimateManaPotionPrefab)
        {
            StartCoroutine(ApplyUltimateManaPotion());
        }
    }

    private void ApplyHealthPotion(float percentage)
    {
        if (isHealthLocked) return;

        float amount = maxHealth * percentage;
        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        UpdateSliders();
    }

    private IEnumerator ApplyUltimateHealthPotion()
    {
        isHealthLocked = true;
        currentHealth = maxHealth;
        UpdateSliders();

        yield return new WaitForSeconds(3f);

        isHealthLocked = false;
    }

    private void ApplyManaPotion(float percentage)
    {
        if (isManaLocked) return;

        float amount = maxMana * percentage;
        currentMana = Mathf.Min(maxMana, currentMana + amount);
        UpdateSliders();
    }

    private IEnumerator ApplyUltimateManaPotion()
    {
        isManaLocked = true;
        currentMana = maxMana;
        UpdateSliders();

        yield return new WaitForSeconds(3f);

        isManaLocked = false;
    }

    // Example: If potions are physical pickups with collider triggers
    private void OnTriggerEnter(Collider other)
    {
        // Match prefab by tag or custom component
        if (other.CompareTag("HealthPotion30"))
        {
            UsePotion(healthPotion30Prefab);
            Destroy(other.gameObject);
        }
        else if (other.CompareTag("UltimateHealthPotion"))
        {
            UsePotion(ultimateHealthPotionPrefab);
            Destroy(other.gameObject);
        }
        else if (other.CompareTag("ManaPotion30"))
        {
            UsePotion(manaPotion30Prefab);
            Destroy(other.gameObject);
        }
        else if (other.CompareTag("UltimateManaPotion"))
        {
            UsePotion(ultimateManaPotionPrefab);
            Destroy(other.gameObject);
        }
    }
}
