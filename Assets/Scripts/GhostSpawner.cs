using UnityEngine;
using System.Collections;

public class GhostSpawner : MonoBehaviour
{
    public GameObject ghostPrefab;
    public Transform playerTransform;

    public int ghostCount = 5;
    public float minSpawnInterval = 1f;
    public float maxSpawnInterval = 3f;

    public float spawnRadius = 2f; // Offset radius so they don't overlap

    void Start()
    {
        StartCoroutine(SpawnGhosts());
    }

    IEnumerator SpawnGhosts()
    {
        for (int i = 0; i < ghostCount; i++)
        {
            SpawnOneGhost();
            float delay = Random.Range(minSpawnInterval, maxSpawnInterval);
            yield return new WaitForSeconds(delay);
        }
    }

    void SpawnOneGhost()
    {
        // Random offset so they don't all spawn at same position
        Vector2 offset2D = Random.insideUnitCircle * spawnRadius;
        Vector3 spawnPos = transform.position + new Vector3(offset2D.x, 0f, offset2D.y);

        GameObject ghost = Instantiate(ghostPrefab, spawnPos, Quaternion.identity);

        GhostMovement gm = ghost.GetComponent<GhostMovement>();
        if (gm != null)
        {
            gm.SetTarget(playerTransform);
        }
    }
}
