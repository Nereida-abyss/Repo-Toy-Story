using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class WaveManager : MonoBehaviour
{
    [Header("Dialogue")]
    [SerializeField] private RoundDialogueController dialogueController;

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

    [Header("Wave Setup")]
    [SerializeField] private WaveSpawner waveSpawner;
    [SerializeField] private WaveBalanceProfile balanceProfile;
    [SerializeField] private float initialWaveDelay = 2f;
    [SerializeField] private float intermissionDuration = 180f;
    [SerializeField] private float spawnInterval = 0.5f;
    [SerializeField] private float waveAnnouncementDuration = 2.5f;
    [SerializeField] private int baseEnemyCount = 10;
    [SerializeField] private int additionalEnemiesPerWave = 2;

    [Header("Enemy Round Scaling")]
    [SerializeField] [Min(1f)] private float healthMultiplierPerRound = 1.03f;
    [SerializeField] [Min(1f)] private float damageMultiplierPerRound = 1.02f;
    [SerializeField] [Min(1f)] private float maxHealthMultiplier = 1.75f;
    [SerializeField] [Min(1f)] private float maxDamageMultiplier = 1.35f;

    private readonly HashSet<PlayerHealthScript> aliveEnemies = new HashSet<PlayerHealthScript>();
    private int currentWaveIndex;
    private int spawnAttemptsCompletedThisWave;
    private int enemiesToSpawnThisWave;
    private float roundElapsedTime;
    private float remainingIntermissionTime;
    private bool hasLoggedMissingSpawner;
    private bool hasLoggedMissingBalanceProfile;
    private bool isSpawningCurrentWave;
    private WaveRuntimeState currentState = WaveRuntimeState.InitialDelay;

    public event Action<int> WaveStarted;
    public event Action IntermissionStarted;

    public WaveRuntimeState CurrentState => currentState;
    public int CurrentWaveIndex => currentWaveIndex;
    public float RoundElapsedTime => roundElapsedTime;
    public float RemainingIntermissionTime => Mathf.Max(0f, remainingIntermissionTime);
    public float WaveAnnouncementDuration => Mathf.Max(0.01f, GetWaveAnnouncementDuration());

    void Start()
    {
        ResetRuntimeState();
        ValidateConfiguration();
        StartCoroutine(WaveLoop());
    }

    void Update()
    {
        bool isPaused = UIManager.IsGamePaused;
        PruneDeadEnemies();

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

            return;
        }

        if (currentState != WaveRuntimeState.Intermission || isPaused)
        {
            return;
        }

        if (!PlayerShopController.IsInputBlocked && ProjectInput.WasNextWavePressed())
        {
            StartNextWave();
            return;
        }

        remainingIntermissionTime -= Time.deltaTime;

        if (remainingIntermissionTime <= 0f)
        {
            StartNextWave();
        }
    }

    void OnDestroy()
    {
        StopAllCoroutines();

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
        yield return new WaitForSeconds(Mathf.Max(0f, GetInitialWaveDelay()));
        StartNextWave();
    }

    private void StartNextWave()
    {
        if (currentState == WaveRuntimeState.WaveInProgress || !HasValidSpawner())
        {
            return;
        }

        if (currentWaveIndex > 0)
        {
            StartCoroutine(ShowDialogueBeforeWave());
        }

        PruneDeadEnemies();
        currentWaveIndex++;
        RunStatsStore.UpdateWave(currentWaveIndex);
        currentState = WaveRuntimeState.WaveInProgress;
        roundElapsedTime = 0f;
        remainingIntermissionTime = 0f;
        enemiesToSpawnThisWave = GetEnemyCountForWave(currentWaveIndex);
        spawnAttemptsCompletedThisWave = 0;
        isSpawningCurrentWave = true;
        WaveStarted?.Invoke(currentWaveIndex);
        StartCoroutine(SpawnWaveCoroutine(currentWaveIndex));
    }

    private IEnumerator SpawnWaveCoroutine(int waveIndex)
    {
        int spawnCount = enemiesToSpawnThisWave;

        for (int i = 0; i < spawnCount; i++)
        {
            spawnAttemptsCompletedThisWave++;

            if (waveSpawner != null && waveSpawner.TrySpawnEnemy(out GameObject spawnedEnemy))
            {
                RegisterSpawnedEnemy(spawnedEnemy, waveIndex);
            }

            if (i < spawnCount - 1)
            {
                yield return new WaitForSeconds(Mathf.Max(0.01f, GetSpawnInterval()));
            }
        }

        isSpawningCurrentWave = false;
    }

    private void RegisterSpawnedEnemy(GameObject spawnedEnemy, int waveIndex)
    {
        if (spawnedEnemy == null)
        {
            return;
        }

        ApplyRoundScalingToEnemy(spawnedEnemy, waveIndex);

        PlayerHealthScript enemyHealth = spawnedEnemy.GetComponent<PlayerHealthScript>();

        if (enemyHealth == null)
        {
            enemyHealth = spawnedEnemy.GetComponentInChildren<PlayerHealthScript>();
        }

        if (enemyHealth == null)
        {
            GameDebug.Advertencia("Oleadas", $"Se genero un enemigo desde {spawnedEnemy.name} sin PlayerHealthScript.", spawnedEnemy);
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

    private bool HasValidSpawner()
    {
        if (waveSpawner != null)
        {
            return true;
        }

        if (!hasLoggedMissingSpawner)
        {
            hasLoggedMissingSpawner = true;
            GameDebug.Error("Oleadas", "WaveManager necesita una referencia explicita a WaveSpawner.", this);
        }

        return false;
    }

    private int GetEnemyCountForWave(int waveIndex)
    {
        return Mathf.Max(1, GetBaseEnemyCount() + ((waveIndex - 1) * GetAdditionalEnemiesPerWave()));
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
            GetHealthMultiplierPerRound(),
            GetMaxHealthMultiplier(),
            waveOffset);
        float damageMultiplier = GetEffectiveRoundMultiplier(
            GetDamageMultiplierPerRound(),
            GetMaxDamageMultiplier(),
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
        remainingIntermissionTime = Mathf.Max(0f, GetIntermissionDuration());
        IntermissionStarted?.Invoke();

        RoundDialogueManager.Instance.AdvanceToNextRound();
    }

    private void ResetRuntimeState()
    {
        StopAllCoroutines();
        currentWaveIndex = 0;
        spawnAttemptsCompletedThisWave = 0;
        enemiesToSpawnThisWave = 0;
        roundElapsedTime = 0f;
        remainingIntermissionTime = 0f;
        isSpawningCurrentWave = false;
        currentState = WaveRuntimeState.InitialDelay;
        aliveEnemies.Clear();
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

    private void ValidateConfiguration()
    {
        HasValidSpawner();
        WarnIfMissingBalanceProfile();
    }

    private void WarnIfMissingBalanceProfile()
    {
        if (balanceProfile != null)
        {
            hasLoggedMissingBalanceProfile = false;
            return;
        }

        if (hasLoggedMissingBalanceProfile)
        {
            return;
        }

        hasLoggedMissingBalanceProfile = true;
        GameDebug.Advertencia("Oleadas", "WaveManager no tiene WaveBalanceProfile asignado. Se usaran los valores locales del componente.", this);
    }

    private IEnumerator ShowDialogueBeforeWave()
    {
        isSpawningCurrentWave = false;

        if (dialogueController != null)
        {
            yield return dialogueController.ShowDialogueAndWait();
        }

        isSpawningCurrentWave = true;
    }

    private float GetInitialWaveDelay() => balanceProfile != null ? balanceProfile.InitialWaveDelay : initialWaveDelay;
    private float GetIntermissionDuration() => balanceProfile != null ? balanceProfile.IntermissionDuration : intermissionDuration;
    private float GetSpawnInterval() => balanceProfile != null ? balanceProfile.SpawnInterval : spawnInterval;
    private float GetWaveAnnouncementDuration() => balanceProfile != null ? balanceProfile.WaveAnnouncementDuration : waveAnnouncementDuration;
    private int GetBaseEnemyCount() => balanceProfile != null ? balanceProfile.BaseEnemyCount : baseEnemyCount;
    private int GetAdditionalEnemiesPerWave() => balanceProfile != null ? balanceProfile.AdditionalEnemiesPerWave : additionalEnemiesPerWave;
    private float GetHealthMultiplierPerRound() => balanceProfile != null ? balanceProfile.HealthMultiplierPerRound : healthMultiplierPerRound;
    private float GetDamageMultiplierPerRound() => balanceProfile != null ? balanceProfile.DamageMultiplierPerRound : damageMultiplierPerRound;
    private float GetMaxHealthMultiplier() => balanceProfile != null ? balanceProfile.MaxHealthMultiplier : maxHealthMultiplier;
    private float GetMaxDamageMultiplier() => balanceProfile != null ? balanceProfile.MaxDamageMultiplier : maxDamageMultiplier;
}
