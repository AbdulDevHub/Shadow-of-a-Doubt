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
        [Header("Ghost Setup")]
        public List<GhostEntry> ghostsInWave = new List<GhostEntry>();
        public float minSpawnInterval = 1f;
        public float maxSpawnInterval = 3f;

        [Header("Potion Drop Options")]
        public bool dropPotion = false;
        public GameObject potionPrefab;  // Different prefab per wave
    }

    public List<Wave> waves = new List<Wave>();
    public Transform playerTransform;

    private List<Transform> spawnPoints = new List<Transform>();

    // internal state
    private int currentWaveIndex = -1;
    private int aliveGhosts = 0;

    void Awake()
    {
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
        for (int i = 0; i < waves.Count; i++)
        {
            currentWaveIndex = i;
            aliveGhosts = 0;

            yield return StartCoroutine(SpawnWave(waves[i]));

            // Wait until all ghosts from this wave are dead
            yield return new WaitUntil(() => aliveGhosts <= 0);
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

        aliveGhosts++;

        // Hook into ghost death
        GhostHealth gh = ghost.GetComponent<GhostHealth>();
        if (gh != null)
        {
            gh.onGhostDied += OnGhostDied;
        }

        GhostMovement gm = ghost.GetComponent<GhostMovement>();
        if (gm != null && playerTransform != null)
        {
            gm.SetTarget(playerTransform);
        }
    }

    void OnGhostDied(GhostHealth ghost)
    {
        aliveGhosts--;

        // If last ghost of wave & potion drop is enabled, spawn potion
        if (aliveGhosts <= 0 && currentWaveIndex >= 0 && currentWaveIndex < waves.Count)
        {
            Wave wave = waves[currentWaveIndex];
            if (wave.dropPotion && wave.potionPrefab != null)
            {
                Instantiate(wave.potionPrefab, ghost.transform.position, Quaternion.identity);
            }
        }
    }
}
