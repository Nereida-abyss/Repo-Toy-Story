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

    // Secuencia de oleada bucle.
    private IEnumerator WaveLoop()
    {
        currentState = WaveRuntimeState.InitialDelay;
        RefreshTimersUi();
        yield return new WaitForSeconds(Mathf.Max(0f, initialWaveDelay));
        StartNextWave();
    }

    // Inicia siguiente oleada.
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
        RunStatsStore.UpdateWave(currentWaveIndex);
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

    // Genera oleada currutina.
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

    // Genera enemigo.
    private void SpawnEnemy(int waveIndex)
    {
        if (!TryResolveSpawnPosition(out Vector3 spawnPosition, out EnemySpawnPoint spawnPoint))
        {
            GameDebug.Advertencia("Oleadas", "No se encontró una posición valida en NavMesh para generar enemigo.", this);
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
            GameDebug.Advertencia("Oleadas", $"Se genero un enemigo desde {enemyPrefab.name} sin PlayerHealthScript.", spawnedEnemy);
            return;
        }

        enemyHealth.Died += HandleEnemyDied;
        aliveEnemies.Add(enemyHealth);
    }

    // Gestiona enemigo muerto.
    private void HandleEnemyDied(PlayerHealthScript enemyHealth)
    {
        if (enemyHealth == null)
        {
            return;
        }

        enemyHealth.Died -= HandleEnemyDied;
        aliveEnemies.Remove(enemyHealth);
    }

    // Refresca escena referencias.
    private void RefreshSceneReferences()
    {
        spawnPoints = FindObjectsByType<EnemySpawnPoint>(FindObjectsSortMode.None);
        cachedPlayerController = ResolvePlayerController();
        ResolveAnnouncementUi();
    }

    // Resuelve anuncio UI.
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

    // Comprueba si hay puntos de spawn.
    private bool HasSpawnPoints()
    {
        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            if (!hasLoggedMissingSpawnPoints)
            {
                GameDebug.Error("Oleadas", "WaveManager necesita al menos un EnemySpawnPoint activo en la escena.", this);
                hasLoggedMissingSpawnPoints = true;
            }

            return false;
        }

        return true;
    }

    // Comprueba si valido enemigo prefab.
    private bool HasValidEnemyPrefab()
    {
        if (enemyPrefab == null)
        {
            if (!hasLoggedMissingEnemyPrefab)
            {
                GameDebug.Error("Oleadas", "WaveManager no tiene asignado el prefab de enemigo.", this);
                hasLoggedMissingEnemyPrefab = true;
            }

            return false;
        }

        return true;
    }

    // Obtiene un punto de spawn aleatorio.
    private EnemySpawnPoint GetRandomSpawnPoint()
    {
        if (!HasSpawnPoints())
        {
            return null;
        }

        int index = Random.Range(0, spawnPoints.Length);
        return spawnPoints[index];
    }

    // Intenta resolver spawn posición.
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

    // Obtiene enemigo conteo para oleada.
    private int GetEnemyCountForWave(int waveIndex)
    {
        return Mathf.Max(1, baseEnemyCount + ((waveIndex - 1) * additionalEnemiesPerWave));
    }

    // Aplica ronda escalado a enemigo.
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

    // Obtiene una captura del escalado por ronda.
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

    // Obtiene effective ronda multiplier.
    private static float GetEffectiveRoundMultiplier(float multiplierPerRound, float maxMultiplier, int waveOffset)
    {
        float sanitizedPerRound = Mathf.Max(1f, multiplierPerRound);
        float sanitizedMax = Mathf.Max(1f, maxMultiplier);
        float compoundedMultiplier = Mathf.Pow(sanitizedPerRound, Mathf.Max(0, waveOffset));
        return Mathf.Min(sanitizedMax, compoundedMultiplier);
    }

    // Comprueba si oleada finished.
    private bool HasWaveFinished()
    {
        PruneDeadEnemies();
        return !isSpawningCurrentWave
            && spawnAttemptsCompletedThisWave >= enemiesToSpawnThisWave
            && aliveEnemies.Count == 0;
    }

    // Inicia intermedio.
    private void BeginIntermission()
    {
        currentState = WaveRuntimeState.Intermission;
        remainingIntermissionTime = Mathf.Max(0f, intermissionDuration);
        ResolveAnnouncementUi();
        HideWaveAnnouncement();
        if (waveIntermissionUi != null)
        {
            waveIntermissionUi.ShowPrompt();
        }
        RefreshTimersUi();
    }

    // Oculta intermedio prompt.
    private void HideIntermissionPrompt()
    {
        ResolveAnnouncementUi();
        if (waveIntermissionUi != null)
        {
            waveIntermissionUi.HidePrompt();
        }
    }

    // Muestra oleada anuncio.
    private void ShowWaveAnnouncement(int waveNumber)
    {
        ResolveAnnouncementUi();
        remainingWaveAnnouncementTime = Mathf.Max(0.01f, waveAnnouncementDuration);
        if (waveAnnouncementUi != null)
        {
            waveAnnouncementUi.ShowWave(waveNumber);
        }
    }

    // Oculta oleada anuncio.
    private void HideWaveAnnouncement()
    {
        remainingWaveAnnouncementTime = 0f;
        ResolveAnnouncementUi();
        if (waveAnnouncementUi != null)
        {
            waveAnnouncementUi.HideWave();
        }
    }

    // Actualiza oleada anuncio temporizador.
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

    // Refresca temporizadores UI.
    private void RefreshTimersUi()
    {
        ResolveAnnouncementUi();
        if (waveTimersUi != null)
        {
            waveTimersUi.Refresh(currentState, roundElapsedTime, RemainingIntermissionTime);
        }
    }

    // Reinicia runtime estado.
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
        HideTransientUi(false);
    }

    // Oculta transient UI.
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
        if (waveAnnouncementUi != null)
        {
            waveAnnouncementUi.HideWave();
        }

        if (waveIntermissionUi != null)
        {
            waveIntermissionUi.HidePrompt();
        }

        if (waveTimersUi != null)
        {
            waveTimersUi.Refresh(WaveRuntimeState.InitialDelay, 0f, 0f);
        }
    }

    // Gestiona prune dead enemigos.
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

    // Resuelve jugador controller.
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

    // Gestiona registro faltante jugador UI warning.
    private void LogMissingPlayerUiWarning(string message)
    {
        if (hasLoggedMissingPlayerUi)
        {
            return;
        }

        hasLoggedMissingPlayerUi = true;
        GameDebug.Advertencia("Oleadas", message, this);
    }
}
