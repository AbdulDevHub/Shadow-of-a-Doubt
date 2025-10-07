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

        [Header("Potion Drop Options")]
        public bool dropPotion = false;
        public GameObject potionPrefab;  // Different prefab per wave
    }

    [Header("Spawner Settings")]
    public List<Wave> waves = new List<Wave>();
    public Transform playerTransform;
    public bool startOnSceneLoad = true; // Toggle in Inspector

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
        if (startOnSceneLoad)
        {
            StartCoroutine(HandleWaves());
        }
    }

    public void StartSpawning()
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

    public IEnumerator SpawnWave(Wave wave)
    {
        var (minInterval, maxInterval) = DifficultyManager.Instance.GetSpawnIntervals();

        foreach (var ghostEntry in wave.ghostsInWave)
        {
            for (int i = 0; i < ghostEntry.amount; i++)
            {
                SpawnGhost(ghostEntry.ghostPrefab);

                float delay = Random.Range(minInterval, maxInterval);
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

        GhostMoveAndAttack gm = ghost.GetComponent<GhostMoveAndAttack>();
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
