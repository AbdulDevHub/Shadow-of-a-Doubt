using UnityEngine;

public class Collectible : MonoBehaviour
{
    public string message = "Cube Collected!";
    public AudioClip collectSound;
    public GameObject vanishEffectPrefab;

    public void Collect()
    {
        // play sound
        if (collectSound != null)
            AudioSource.PlayClipAtPoint(collectSound, transform.position);

        // spawn vanish effect
        if (vanishEffectPrefab != null)
        {
            GameObject effect = Instantiate(vanishEffectPrefab, transform.position, Quaternion.identity);
            Destroy(effect, 2f);
        }

        // show UI message
        UIManager.Instance.ShowMessage(message);

        // destroy cube
        Destroy(gameObject);
    }
}
