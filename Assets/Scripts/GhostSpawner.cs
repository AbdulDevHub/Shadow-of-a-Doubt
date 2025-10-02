using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class GhostSpawnerMaster : MonoBehaviour
{
    [System.Serializable]
    public class GhostEntry
    {
        public GameObject ghostPrefab;
        public int amount;
    }

    [System.Serializable]
    public class Wave
    {
        public List<GhostEntry> ghostsInWave = new List<GhostEntry>();
        public float minSpawnInterval = 1f;
        public float maxSpawnInterval = 3f;
    }

    public List<Wave> waves = new List<Wave>();
    public Transform playerTransform;

    private List<Transform> spawnPoints = new List<Transform>();

    void Awake()
    {
        // Collect all child transforms that are spawners
        foreach (Transform child in transform)
        {
            spawnPoints.Add(child);
        }
    }

    void Start()
    {
        StartCoroutine(HandleWaves());
    }

    IEnumerator HandleWaves()
    {
        foreach (var wave in waves)
        {
            yield return StartCoroutine(SpawnWave(wave));
        }
    }

    IEnumerator SpawnWave(Wave wave)
    {
        foreach (var ghostEntry in wave.ghostsInWave)
        {
            for (int i = 0; i < ghostEntry.amount; i++)
            {
                SpawnGhost(ghostEntry.ghostPrefab);

                float delay = Random.Range(wave.minSpawnInterval, wave.maxSpawnInterval);
                yield return new WaitForSeconds(delay);
            }
        }
    }

    void SpawnGhost(GameObject ghostPrefab)
    {
        if (spawnPoints.Count == 0) return;

        Transform randomPoint = spawnPoints[Random.Range(0, spawnPoints.Count)];

        GameObject ghost = Instantiate(
            ghostPrefab,
            randomPoint.position,
            Quaternion.identity
        );

        GhostMovement gm = ghost.GetComponent<GhostMovement>();
        if (gm != null && playerTransform != null)
        {
            gm.SetTarget(playerTransform);
        }
    }
}
