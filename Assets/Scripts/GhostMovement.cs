using UnityEngine;

public class GhostMovement : MonoBehaviour
{
    public Transform target;
    public float speed = 2f;
    public float stopDistance = 2f;
    public float rotationSpeed = 5f;

    [Header("Separation Settings")]
    public float separationDistance = 0.3f;
    public float separationStrength = 0.25f;
    public float separationSmoothing = 5f;

    [Header("Height Settings")]
    public float minHeightOffset = -0.5f;
    public float heightAdjustSpeed = 3f;

    private static readonly string GhostTag = "Ghost";
    private Vector3 smoothedSeparation = Vector3.zero;

    private float speedMultiplier = 1f;
    private Vector3 pushVelocity = Vector3.zero;
    private float pushDecay = 2f;

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }

    public void SetSpeedMultiplier(float multiplier)
    {
        speedMultiplier = multiplier;
    }

    public void ApplyPushback(Vector3 push)
    {
        pushVelocity += push; // Includes vertical component
    }

    void Update()
    {
        if (target == null) return;

        // --- Step 1: Base direction toward player ---
        Vector3 direction = (target.position - transform.position).normalized;

        // --- Step 2: Separation between ghosts ---
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

        smoothedSeparation = Vector3.Lerp(smoothedSeparation, rawSeparation, Time.deltaTime * separationSmoothing);

        if (smoothedSeparation != Vector3.zero)
            direction = (direction + smoothedSeparation).normalized;

        // --- Step 3: Rotate to face player ---
        if (direction != Vector3.zero)
        {
            Quaternion lookRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, rotationSpeed * Time.deltaTime);
        }

        // --- Step 4: Move toward player + apply pushback ---
        float distanceToTarget = Vector3.Distance(transform.position, target.position);
        if (distanceToTarget > stopDistance)
        {
            // Keep push magnitude instead of normalizing
            Vector3 move = direction * speed * speedMultiplier * Time.deltaTime;
            Vector3 push = pushVelocity * Time.deltaTime;
            transform.position += move + push;

            // Decay push velocity
            pushVelocity = Vector3.Lerp(pushVelocity, Vector3.zero, Time.deltaTime * pushDecay);
        }

        // --- Step 5: Clamp height ---
        float minAllowedHeight = target.position.y + minHeightOffset;
        if (transform.position.y < minAllowedHeight)
        {
            Vector3 corrected = transform.position;
            corrected.y = Mathf.Lerp(corrected.y, minAllowedHeight, Time.deltaTime * heightAdjustSpeed);
            transform.position = corrected;
        }
    }
}
