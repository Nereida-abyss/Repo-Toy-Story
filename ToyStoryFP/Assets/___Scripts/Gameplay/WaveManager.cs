using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[DisallowMultipleComponent]
public class WaveManager : MonoBehaviour
{
    private const int MaxSpawnPositionAttempts = 12;
    private const float MinimumNavMeshSampleDistance = 8f;
    private const float SpawnSampleHeightOffset = 0.5f;

    [Header("Wave Setup")]
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private float initialWaveDelay = 2f;
    [SerializeField] private float waveInterval = 60f;
    [SerializeField] private float spawnInterval = 0.5f;
    [SerializeField] private int baseEnemyCount = 10;
    [SerializeField] private int additionalEnemiesPerWave = 2;
    [SerializeField] private float navMeshSampleDistance = 2f;

    private readonly HashSet<PlayerHealthScript> aliveEnemies = new HashSet<PlayerHealthScript>();
    private EnemySpawnPoint[] spawnPoints = System.Array.Empty<EnemySpawnPoint>();
    private WaveAnnouncementUI waveAnnouncementUi;
    private int currentWaveIndex;
    private bool hasLoggedMissingSpawnPoints;
    private bool hasLoggedMissingEnemyPrefab;

    void Start()
    {
        RefreshSceneReferences();
        StartCoroutine(WaveLoop());
    }

    void OnDestroy()
    {
        foreach (PlayerHealthScript health in aliveEnemies)
        {
            if (health != null)
            {
                health.Died -= HandleEnemyDied;
            }
        }

        aliveEnemies.Clear();
    }

    private IEnumerator WaveLoop()
    {
        yield return new WaitForSeconds(Mathf.Max(0f, initialWaveDelay));

        while (true)
        {
            StartWave();
            yield return new WaitForSeconds(Mathf.Max(0f, waveInterval));
        }
    }

    private void StartWave()
    {
        if (!HasValidEnemyPrefab() || !HasSpawnPoints())
        {
            return;
        }

        currentWaveIndex++;
        ResolveAnnouncementUi();
        waveAnnouncementUi?.ShowWave(currentWaveIndex);
        StartCoroutine(SpawnWaveCoroutine(currentWaveIndex));
    }

    private IEnumerator SpawnWaveCoroutine(int waveIndex)
    {
        int enemiesToSpawn = GetEnemyCountForWave(waveIndex);

        for (int i = 0; i < enemiesToSpawn; i++)
        {
            SpawnEnemy();

            if (i < enemiesToSpawn - 1)
            {
                yield return new WaitForSeconds(Mathf.Max(0.01f, spawnInterval));
            }
        }
    }

    private void SpawnEnemy()
    {
        if (!TryResolveSpawnPosition(out Vector3 spawnPosition, out EnemySpawnPoint spawnPoint))
        {
            Debug.LogWarning("WaveManager could not find a valid NavMesh position for an enemy spawn.", this);
            return;
        }

        GameObject spawnedEnemy = Instantiate(enemyPrefab, spawnPosition, spawnPoint.Rotation);
        PlayerHealthScript enemyHealth = spawnedEnemy.GetComponent<PlayerHealthScript>();

        if (enemyHealth == null)
        {
            enemyHealth = spawnedEnemy.GetComponentInChildren<PlayerHealthScript>();
        }

        if (enemyHealth == null)
        {
            Debug.LogWarning($"Spawned enemy from {enemyPrefab.name} without PlayerHealthScript.", spawnedEnemy);
            return;
        }

        enemyHealth.Died += HandleEnemyDied;
        aliveEnemies.Add(enemyHealth);
    }

    private void HandleEnemyDied(PlayerHealthScript enemyHealth)
    {
        if (enemyHealth == null)
        {
            return;
        }

        enemyHealth.Died -= HandleEnemyDied;
        aliveEnemies.Remove(enemyHealth);
    }

    private void RefreshSceneReferences()
    {
        spawnPoints = FindObjectsByType<EnemySpawnPoint>(FindObjectsSortMode.None);
        ResolveAnnouncementUi();
    }

    private void ResolveAnnouncementUi()
    {
        if (waveAnnouncementUi != null)
        {
            return;
        }

        if (PlayerController.Instance == null)
        {
            return;
        }

        waveAnnouncementUi = PlayerController.Instance.GetComponentInChildren<WaveAnnouncementUI>(true);
    }

    private bool HasSpawnPoints()
    {
        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            if (!hasLoggedMissingSpawnPoints)
            {
                Debug.LogError("WaveManager requires at least one active EnemySpawnPoint in the scene.", this);
                hasLoggedMissingSpawnPoints = true;
            }

            return false;
        }

        return true;
    }

    private bool HasValidEnemyPrefab()
    {
        if (enemyPrefab == null)
        {
            if (!hasLoggedMissingEnemyPrefab)
            {
                Debug.LogError("WaveManager is missing the enemy prefab reference.", this);
                hasLoggedMissingEnemyPrefab = true;
            }

            return false;
        }

        return true;
    }

    private EnemySpawnPoint GetRandomSpawnPoint()
    {
        if (!HasSpawnPoints())
        {
            return null;
        }

        int index = Random.Range(0, spawnPoints.Length);
        return spawnPoints[index];
    }

    private bool TryResolveSpawnPosition(out Vector3 spawnPosition, out EnemySpawnPoint resolvedSpawnPoint)
    {
        spawnPosition = Vector3.zero;
        resolvedSpawnPoint = null;

        float searchDistance = Mathf.Max(MinimumNavMeshSampleDistance, navMeshSampleDistance);

        for (int attempt = 0; attempt < MaxSpawnPositionAttempts; attempt++)
        {
            EnemySpawnPoint candidate = GetRandomSpawnPoint();

            if (candidate == null)
            {
                return false;
            }

            Vector3 desiredPosition = candidate.Position + (Vector3.up * SpawnSampleHeightOffset);

            if (NavMesh.SamplePosition(desiredPosition, out NavMeshHit hit, searchDistance, NavMesh.AllAreas))
            {
                spawnPosition = hit.position;
                resolvedSpawnPoint = candidate;
                return true;
            }
        }

        return false;
    }

    private int GetEnemyCountForWave(int waveIndex)
    {
        return Mathf.Max(1, baseEnemyCount + ((waveIndex - 1) * additionalEnemiesPerWave));
    }
}
