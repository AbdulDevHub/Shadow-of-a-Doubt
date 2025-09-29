using UnityEngine;
using UnityEngine.InputSystem; // required for new Input System

public class WandShooter : MonoBehaviour
{
    public float range = 50f;

    void Update()
    {
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            ShootRay();
        }
    }

    void ShootRay()
    {
        Ray ray = new Ray(transform.position, transform.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, range))
        {
            Collectible collectible = hit.collider.GetComponent<Collectible>();
            if (collectible != null)
            {
                collectible.Collect();
            }
        }
    }
}
