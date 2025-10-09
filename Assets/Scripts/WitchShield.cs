using UnityEngine;

public class WitchShield : MonoBehaviour
{
    public enum ShieldElement { Fire, Ice , Poison }

    [Header("Shield Settings")]
    public MeshRenderer shieldRenderer;     // assign a material/mesh with transparent shader
    public float shieldDuration = 3f;       // active time
    public Color fireColor = new Color(1f, 0f, 0f, 0.4f);   // orange
    public Color iceColor = new Color(0f, 0.3f, 1f, 0.4f); // light blue
    public Color poisonColor = new Color(0f, 1f, 0f, 0.4f);    // dark green

    private ShieldElement currentElement;
    private bool isActive;

    private void Awake()
    {
        if (shieldRenderer != null)
            shieldRenderer.enabled = false; // hide initially
    }

    /// <summary>
    /// Activates the shield with a random element.
    /// </summary>
    public void ActivateShield()
    {
        if (isActive) return;
        isActive = true;

        // Pick random element
        currentElement = (ShieldElement)Random.Range(0, 3);

        if (shieldRenderer != null)
        {
            shieldRenderer.enabled = true;
            Material matInstance = shieldRenderer.material; // instance so alpha works
            switch (currentElement)
            {
                case ShieldElement.Fire: matInstance.color = fireColor; break;
                case ShieldElement.Ice: matInstance.color = iceColor; break;
                case ShieldElement.Poison: matInstance.color = poisonColor; break;
            }
        }

        // Automatically deactivate after duration
        Invoke(nameof(DeactivateShield), shieldDuration);
    }

    private void DeactivateShield()
    {
        isActive = false;
        if (shieldRenderer != null)
            shieldRenderer.enabled = false;
    }

    /// <summary>
    /// Checks if the attack matches the current shield element.
    /// Returns true if the shield allows damage.
    /// </summary>
    public bool CanTakeDamage(int spellIndex)
    {
        if (!isActive) return true; // no shield → all attacks work

        // Map spell index to shield element
        // 0 = Fire, 1 = Ice, 2 = Poison
        switch (currentElement)
        {
            case ShieldElement.Fire: return spellIndex == 1;
            case ShieldElement.Ice: return spellIndex == 0;
            case ShieldElement.Poison: return spellIndex == 2;
        }
        return true;
    }
}
