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

    // --- Pushback system ---
    private Vector3 pushVelocity = Vector3.zero;          // current push velocity (units/sec)
    public float pushDecay = 2f;                          // higher = decays faster
    public float pushForwardReductionThreshold = 0.2f;    // when pushVelocity.magnitude > this, reduce forward movement
    [Range(0f,1f)] public float forwardReductionWhilePushed = 0.5f; // fraction of forward speed while pushed

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
        // Expect `push` to be a velocity impulse (e.g. direction * magnitude in units/sec).
        // If you want `push` to be an instant displacement, multiply it here by some factor,
        // or change how GhostHealth passes it in.
        pushVelocity += push;
        // Optional debug:
        // Debug.Log($"ApplyPushback: added {push}, new pushVelocity = {pushVelocity}");
    }

    void Update()
    {
        if (target == null) return;

        // --- Step 1: Base direction toward player ---
        Vector3 toTarget = target.position - transform.position;
        float distanceToTarget = toTarget.magnitude;
        Vector3 direction = toTarget.normalized;

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
        // Determine forward movement; reduce it while push is strong so push is visible.
        Vector3 forwardMove = Vector3.zero;
        if (distanceToTarget > stopDistance)
        {
            float forwardFactor = 1f;
            if (pushVelocity.magnitude > pushForwardReductionThreshold)
                forwardFactor = forwardReductionWhilePushed;

            forwardMove = direction * speed * speedMultiplier * forwardFactor * Time.deltaTime;
        }

        // Apply push every frame (even if within stopDistance)
        Vector3 pushDelta = pushVelocity * Time.deltaTime;

        transform.position += forwardMove + pushDelta;

        // Decay push velocity smoothly
        pushVelocity = Vector3.Lerp(pushVelocity, Vector3.zero, Time.deltaTime * pushDecay);

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
