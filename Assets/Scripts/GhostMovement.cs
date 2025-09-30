using UnityEngine;

public class GhostMovement : MonoBehaviour
{
    public Transform target;
    public float speed = 2f;
    public float stopDistance = 2f;
    public float rotationSpeed = 5f;

    [Header("Separation Settings")]
    public float separationDistance = 0.3f;   // how far ghosts try to stay apart
    public float separationStrength = 0.25f;   // lower = gentler push
    public float separationSmoothing = 5f;    // higher = smoother blending

    private static readonly string GhostTag = "Ghost"; // assign this tag to your ghost prefab
    private Vector3 smoothedSeparation = Vector3.zero;

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }

    void Update()
    {
        if (target == null) return;

        // --- Step 1: Base direction toward player ---
        Vector3 direction = (target.position - transform.position).normalized;

        // --- Step 2: Calculate separation (smoothed) ---
        Vector3 rawSeparation = Vector3.zero;
        GameObject[] allGhosts = GameObject.FindGameObjectsWithTag(GhostTag);

        foreach (var ghost in allGhosts)
        {
            if (ghost == gameObject) continue;

            float dist = Vector3.Distance(transform.position, ghost.transform.position);
            if (dist < separationDistance && dist > 0.001f)
            {
                float strength = Mathf.Lerp(separationStrength, 0f, dist / separationDistance);
                Vector3 away = (transform.position - ghost.transform.position).normalized * strength;
                rawSeparation += away;
            }
        }

        // Smooth separation force over time to prevent jitter/bouncing
        smoothedSeparation = Vector3.Lerp(smoothedSeparation, rawSeparation, Time.deltaTime * separationSmoothing);

        // Combine
        if (smoothedSeparation != Vector3.zero)
            direction = (direction + smoothedSeparation).normalized;

        // --- Step 3: Rotate smoothly ---
        if (direction != Vector3.zero)
        {
            Quaternion lookRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                lookRotation,
                rotationSpeed * Time.deltaTime
            );
        }

        // --- Step 4: Move toward player until stop distance ---
        float distanceToTarget = Vector3.Distance(transform.position, target.position);
        if (distanceToTarget > stopDistance)
        {
            transform.position += direction * speed * Time.deltaTime;
        }
    }
}
