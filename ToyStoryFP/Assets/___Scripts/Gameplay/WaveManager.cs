using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[DisallowMultipleComponent]
public class WaveManager : MonoBehaviour
{
    [System.Serializable]
    private struct EnemyRoundScalingSettings
    {
        [Min(1f)] public float healthMultiplierPerRound;
        [Min(1f)] public float damageMultiplierPerRound;
        [Min(1f)] public float maxHealthMultiplier;
        [Min(1f)] public float maxDamageMultiplier;

        public static EnemyRoundScalingSettings Default => new EnemyRoundScalingSettings
        {
            healthMultiplierPerRound = 1.03f,
            damageMultiplierPerRound = 1.02f,
            maxHealthMultiplier = 1.75f,
            maxDamageMultiplier = 1.35f
        };
    }

    private readonly struct EnemyRoundScalingSnapshot
    {
        public EnemyRoundScalingSnapshot(float healthMultiplier, float damageMultiplier)
        {
            HealthMultiplier = healthMultiplier;
            DamageMultiplier = damageMultiplier;
        }

        public float HealthMultiplier { get; }
        public float DamageMultiplier { get; }
    }

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
    [SerializeField] private float waveAnnouncementDuration = 2.5f;
    [SerializeField] private int baseEnemyCount = 10;
    [SerializeField] private int additionalEnemiesPerWave = 2;
    [SerializeField] private float navMeshSampleDistance = 2f;

    [Header("Enemy Round Scaling")]
    [SerializeField] private EnemyRoundScalingSettings enemyRoundScaling = EnemyRoundScalingSettings.Default;

    private readonly HashSet<PlayerHealthScript> aliveEnemies = new HashSet<PlayerHealthScript>();
    private EnemySpawnPoint[] spawnPoints = System.Array.Empty<EnemySpawnPoint>();
    private WaveAnnouncementUI waveAnnouncementUi;
    private WaveIntermissionUI waveIntermissionUi;
    private WaveTimersUI waveTimersUi;
    private PlayerController cachedPlayerController;
    private int currentWaveIndex;
    private int spawnAttemptsCompletedThisWave;
    private int enemiesToSpawnThisWave;
    private float roundElapsedTime;
    private float remainingIntermissionTime;
    private float remainingWaveAnnouncementTime;
    private bool hasLoggedMissingSpawnPoints;
    private bool hasLoggedMissingEnemyPrefab;
    private bool hasLoggedMissingPlayerUi;
    private bool isSpawningCurrentWave;
    private WaveRuntimeState currentState = WaveRuntimeState.InitialDelay;

    public WaveRuntimeState CurrentState => currentState;
    public int CurrentWaveIndex => currentWaveIndex;
    public float RoundElapsedTime => roundElapsedTime;
    public float RemainingIntermissionTime => Mathf.Max(0f, remainingIntermissionTime);

    void Start()
    {
        ResetRuntimeState();
        RefreshSceneReferences();
        HideTransientUi(true);
        StartCoroutine(WaveLoop());
    }

    void Update()
    {
        bool isPaused = UIManager.IsGamePaused;
        PruneDeadEnemies();
        UpdateWaveAnnouncementTimer(isPaused);

        if (currentState == WaveRuntimeState.WaveInProgress)
        {
            if (!isPaused)
            {
                roundElapsedTime += Time.deltaTime;
            }

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

        if (isPaused)
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
        HideTransientUi(false);

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
        if (currentState == WaveRuntimeState.WaveInProgress)
        {
            return;
        }

        PruneDeadEnemies();

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
        ShowWaveAnnouncement(currentWaveIndex);
        RefreshTimersUi();
        StartCoroutine(SpawnWaveCoroutine(currentWaveIndex));
    }

    private IEnumerator SpawnWaveCoroutine(int waveIndex)
    {
        int enemiesToSpawn = GetEnemyCountForWave(waveIndex);

        for (int i = 0; i < enemiesToSpawn; i++)
        {
            SpawnEnemy(waveIndex);
            spawnAttemptsCompletedThisWave++;

            if (i < enemiesToSpawn - 1)
            {
                yield return new WaitForSeconds(Mathf.Max(0.01f, spawnInterval));
            }
        }

        isSpawningCurrentWave = false;
    }

    private void SpawnEnemy(int waveIndex)
    {
        if (!TryResolveSpawnPosition(out Vector3 spawnPosition, out EnemySpawnPoint spawnPoint))
        {
            Debug.LogWarning("WaveManager could not find a valid NavMesh position for an enemy spawn.", this);
            return;
        }

        GameObject spawnedEnemy = Instantiate(enemyPrefab, spawnPosition, spawnPoint.Rotation);
        ApplyRoundScalingToEnemy(spawnedEnemy, waveIndex);
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
        cachedPlayerController = ResolvePlayerController();
        ResolveAnnouncementUi();
    }

    private void ResolveAnnouncementUi()
    {
        if (waveAnnouncementUi != null && waveIntermissionUi != null && waveTimersUi != null)
        {
            return;
        }

        PlayerController playerController = ResolvePlayerController();

        if (playerController == null)
        {
            LogMissingPlayerUiWarning("WaveManager could not find an active PlayerController to bind wave UI.");
            return;
        }

        waveAnnouncementUi = playerController.GetComponentInChildren<WaveAnnouncementUI>(true);
        waveIntermissionUi = playerController.GetComponentInChildren<WaveIntermissionUI>(true);
        waveTimersUi = playerController.GetComponentInChildren<WaveTimersUI>(true);

        if (waveAnnouncementUi == null || waveIntermissionUi == null || waveTimersUi == null)
        {
            LogMissingPlayerUiWarning("WaveManager found the player, but one or more wave UI views are missing.");
            return;
        }

        hasLoggedMissingPlayerUi = false;
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

    private void ApplyRoundScalingToEnemy(GameObject spawnedEnemy, int waveIndex)
    {
        if (spawnedEnemy == null)
        {
            return;
        }

        EnemyController enemyController = spawnedEnemy.GetComponent<EnemyController>();

        if (enemyController == null)
        {
            enemyController = spawnedEnemy.GetComponentInChildren<EnemyController>();
        }

        if (enemyController == null)
        {
            return;
        }

        EnemyRoundScalingSnapshot roundScaling = GetRoundScalingSnapshot(waveIndex);
        enemyController.ApplyRoundScaling(roundScaling.HealthMultiplier, roundScaling.DamageMultiplier);
    }

    private EnemyRoundScalingSnapshot GetRoundScalingSnapshot(int waveIndex)
    {
        int waveOffset = Mathf.Max(0, waveIndex - 1);
        float healthMultiplier = GetEffectiveRoundMultiplier(
            enemyRoundScaling.healthMultiplierPerRound,
            enemyRoundScaling.maxHealthMultiplier,
            waveOffset);
        float damageMultiplier = GetEffectiveRoundMultiplier(
            enemyRoundScaling.damageMultiplierPerRound,
            enemyRoundScaling.maxDamageMultiplier,
            waveOffset);

        return new EnemyRoundScalingSnapshot(healthMultiplier, damageMultiplier);
    }

    private static float GetEffectiveRoundMultiplier(float multiplierPerRound, float maxMultiplier, int waveOffset)
    {
        float sanitizedPerRound = Mathf.Max(1f, multiplierPerRound);
        float sanitizedMax = Mathf.Max(1f, maxMultiplier);
        float compoundedMultiplier = Mathf.Pow(sanitizedPerRound, Mathf.Max(0, waveOffset));
        return Mathf.Min(sanitizedMax, compoundedMultiplier);
    }

    private bool HasWaveFinished()
    {
        PruneDeadEnemies();
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

    private void ShowWaveAnnouncement(int waveNumber)
    {
        ResolveAnnouncementUi();
        remainingWaveAnnouncementTime = Mathf.Max(0.01f, waveAnnouncementDuration);
        waveAnnouncementUi?.ShowWave(waveNumber);
    }

    private void HideWaveAnnouncement()
    {
        remainingWaveAnnouncementTime = 0f;
        ResolveAnnouncementUi();
        waveAnnouncementUi?.HideWave();
    }

    private void UpdateWaveAnnouncementTimer(bool isPaused)
    {
        if (remainingWaveAnnouncementTime <= 0f || isPaused)
        {
            return;
        }

        remainingWaveAnnouncementTime -= Time.deltaTime;

        if (remainingWaveAnnouncementTime <= 0f)
        {
            HideWaveAnnouncement();
        }
    }

    private void RefreshTimersUi()
    {
        ResolveAnnouncementUi();
        waveTimersUi?.Refresh(currentState, roundElapsedTime, RemainingIntermissionTime);
    }

    private void ResetRuntimeState()
    {
        StopAllCoroutines();
        currentWaveIndex = 0;
        spawnAttemptsCompletedThisWave = 0;
        enemiesToSpawnThisWave = 0;
        roundElapsedTime = 0f;
        remainingIntermissionTime = 0f;
        remainingWaveAnnouncementTime = 0f;
        isSpawningCurrentWave = false;
        currentState = WaveRuntimeState.InitialDelay;
        aliveEnemies.Clear();
    }

    private void HideTransientUi(bool resolveMissingReferences)
    {
        if (resolveMissingReferences)
        {
            HideWaveAnnouncement();
            HideIntermissionPrompt();
            RefreshTimersUi();
            return;
        }

        remainingWaveAnnouncementTime = 0f;
        waveAnnouncementUi?.HideWave();
        waveIntermissionUi?.HidePrompt();
    }

    private void PruneDeadEnemies()
    {
        if (aliveEnemies.Count == 0)
        {
            return;
        }

        List<PlayerHealthScript> deadEntries = null;

        foreach (PlayerHealthScript health in aliveEnemies)
        {
            if (health != null)
            {
                continue;
            }

            deadEntries ??= new List<PlayerHealthScript>();
            deadEntries.Add(health);
        }

        if (deadEntries == null)
        {
            return;
        }

        for (int i = 0; i < deadEntries.Count; i++)
        {
            aliveEnemies.Remove(deadEntries[i]);
        }
    }

    private PlayerController ResolvePlayerController()
    {
        if (cachedPlayerController != null)
        {
            return cachedPlayerController;
        }

        cachedPlayerController = PlayerController.Instance;

        if (cachedPlayerController == null)
        {
            cachedPlayerController = FindFirstObjectByType<PlayerController>();
        }

        return cachedPlayerController;
    }

    private void LogMissingPlayerUiWarning(string message)
    {
        if (hasLoggedMissingPlayerUi)
        {
            return;
        }

        hasLoggedMissingPlayerUi = true;
        Debug.LogWarning(message, this);
    }
}
