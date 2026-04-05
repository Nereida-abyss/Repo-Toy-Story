using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[DisallowMultipleComponent]
public class WaveManager : MonoBehaviour
{
    public enum WaveRuntimeState
    {
        InitialDelay,
        WaveInProgress,
        Intermission
    }

    private const int MaxSpawnPositionAttempts = 12;
    private const float MinimumNavMeshSampleDistance = 8f;
    private const float SpawnSampleHeightOffset = 0.5f;

    [Header("Wave Setup")]
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private float initialWaveDelay = 2f;
    [SerializeField] private float intermissionDuration = 180f;
    [SerializeField] private float spawnInterval = 0.5f;
    [SerializeField] private int baseEnemyCount = 10;
    [SerializeField] private int additionalEnemiesPerWave = 2;
    [SerializeField] private float navMeshSampleDistance = 2f;

    private readonly HashSet<PlayerHealthScript> aliveEnemies = new HashSet<PlayerHealthScript>();
    private EnemySpawnPoint[] spawnPoints = System.Array.Empty<EnemySpawnPoint>();
    private WaveAnnouncementUI waveAnnouncementUi;
    private WaveIntermissionUI waveIntermissionUi;
    private WaveTimersUI waveTimersUi;
    private int currentWaveIndex;
    private int spawnAttemptsCompletedThisWave;
    private int enemiesToSpawnThisWave;
    private float roundElapsedTime;
    private float remainingIntermissionTime;
    private bool hasLoggedMissingSpawnPoints;
    private bool hasLoggedMissingEnemyPrefab;
    private bool isSpawningCurrentWave;
    private WaveRuntimeState currentState = WaveRuntimeState.InitialDelay;

    public WaveRuntimeState CurrentState => currentState;
    public int CurrentWaveIndex => currentWaveIndex;
    public float RoundElapsedTime => roundElapsedTime;
    public float RemainingIntermissionTime => Mathf.Max(0f, remainingIntermissionTime);

    void Start()
    {
        RefreshSceneReferences();
        HideIntermissionPrompt();
        RefreshTimersUi();
        StartCoroutine(WaveLoop());
    }

    void Update()
    {
        if (currentState == WaveRuntimeState.WaveInProgress)
        {
            roundElapsedTime += Time.deltaTime;

            if (HasWaveFinished())
            {
                BeginIntermission();
            }

            RefreshTimersUi();
            return;
        }

        if (currentState != WaveRuntimeState.Intermission)
        {
            RefreshTimersUi();
            return;
        }

        if (UIManager.Instance != null && UIManager.Instance.IsPaused)
        {
            RefreshTimersUi();
            return;
        }

        if (Input.GetKeyDown(KeyCode.T))
        {
            StartNextWave();
            return;
        }

        remainingIntermissionTime -= Time.deltaTime;

        if (remainingIntermissionTime <= 0f)
        {
            StartNextWave();
            return;
        }

        RefreshTimersUi();
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
        currentState = WaveRuntimeState.InitialDelay;
        RefreshTimersUi();
        yield return new WaitForSeconds(Mathf.Max(0f, initialWaveDelay));
        StartNextWave();
    }

    private void StartNextWave()
    {
        if (!HasValidEnemyPrefab() || !HasSpawnPoints())
        {
            return;
        }

        currentWaveIndex++;
        currentState = WaveRuntimeState.WaveInProgress;
        roundElapsedTime = 0f;
        remainingIntermissionTime = 0f;
        enemiesToSpawnThisWave = GetEnemyCountForWave(currentWaveIndex);
        spawnAttemptsCompletedThisWave = 0;
        isSpawningCurrentWave = true;
        ResolveAnnouncementUi();
        HideIntermissionPrompt();
        waveAnnouncementUi?.ShowWave(currentWaveIndex);
        RefreshTimersUi();
        StartCoroutine(SpawnWaveCoroutine(currentWaveIndex));
    }

    private IEnumerator SpawnWaveCoroutine(int waveIndex)
    {
        int enemiesToSpawn = GetEnemyCountForWave(waveIndex);

        for (int i = 0; i < enemiesToSpawn; i++)
        {
            SpawnEnemy();
            spawnAttemptsCompletedThisWave++;

            if (i < enemiesToSpawn - 1)
            {
                yield return new WaitForSeconds(Mathf.Max(0.01f, spawnInterval));
            }
        }

        isSpawningCurrentWave = false;
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
        if (waveAnnouncementUi != null && waveIntermissionUi != null && waveTimersUi != null)
        {
            return;
        }

        if (PlayerController.Instance == null)
        {
            return;
        }

        waveAnnouncementUi = PlayerController.Instance.GetComponentInChildren<WaveAnnouncementUI>(true);
        waveIntermissionUi = PlayerController.Instance.GetComponentInChildren<WaveIntermissionUI>(true);
        waveTimersUi = PlayerController.Instance.GetComponentInChildren<WaveTimersUI>(true);
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

    private bool HasWaveFinished()
    {
        return !isSpawningCurrentWave
            && spawnAttemptsCompletedThisWave >= enemiesToSpawnThisWave
            && aliveEnemies.Count == 0;
    }

    private void BeginIntermission()
    {
        currentState = WaveRuntimeState.Intermission;
        remainingIntermissionTime = Mathf.Max(0f, intermissionDuration);
        ResolveAnnouncementUi();
        waveIntermissionUi?.ShowPrompt();
        RefreshTimersUi();
    }

    private void HideIntermissionPrompt()
    {
        ResolveAnnouncementUi();
        waveIntermissionUi?.HidePrompt();
    }

    private void RefreshTimersUi()
    {
        ResolveAnnouncementUi();
        waveTimersUi?.Refresh(currentState, roundElapsedTime, RemainingIntermissionTime);
    }
}
