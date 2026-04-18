using UnityEngine;
using UnityEngine.AI;
using System;
using System.Collections;

[DisallowMultipleComponent]
public class WaveSpawner : MonoBehaviour
{
    private const int MaxSpawnPositionAttempts = 12;
    private const float MinimumNavMeshSampleDistance = 8f;
    private const float SpawnSampleHeightOffset = 0.5f;

    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private EnemySpawnPoint[] spawnPoints = System.Array.Empty<EnemySpawnPoint>();
    [SerializeField] private Transform target;
    [SerializeField] private EnemyTacticsCoordinator tacticsCoordinator;
    [SerializeField] private SpawnTelegraphProfile spawnTelegraphProfile;
    [SerializeField] private float navMeshSampleDistance = 2f;

    private bool hasLoggedMissingSpawnPoints;
    private bool hasLoggedMissingEnemyPrefab;
    private bool hasLoggedInvalidSpawnPosition;
    private bool hasLoggedMissingTarget;
    private bool hasLoggedInvalidTelegraphVfx;

    public bool TrySpawnEnemy(out GameObject spawnedEnemy)
    {
        spawnedEnemy = null;

        if (!HasValidEnemyPrefab() || !HasSpawnPoints())
        {
            return false;
        }

        if (!TryResolveSpawnPosition(out Vector3 spawnPosition, out EnemySpawnPoint spawnPoint))
        {
            if (!hasLoggedInvalidSpawnPosition)
            {
                hasLoggedInvalidSpawnPosition = true;
                GameDebug.Advertencia("Oleadas", "WaveSpawner no encontro una posicion valida en NavMesh para generar enemigo.", this);
            }

            return false;
        }

        hasLoggedInvalidSpawnPosition = false;
        spawnedEnemy = Instantiate(enemyPrefab, spawnPosition, spawnPoint.Rotation);
        ConfigureSpawnedEnemy(spawnedEnemy);
        return spawnedEnemy != null;
    }

    public IEnumerator SpawnEnemyWithTelegraph(Action<GameObject> onEnemySpawned)
    {
        GameObject spawnedEnemy = null;

        if (!HasValidEnemyPrefab() || !HasSpawnPoints())
        {
            onEnemySpawned?.Invoke(null);
            yield break;
        }

        if (!TryResolveSpawnPosition(out Vector3 spawnPosition, out EnemySpawnPoint spawnPoint))
        {
            if (!hasLoggedInvalidSpawnPosition)
            {
                hasLoggedInvalidSpawnPosition = true;
                GameDebug.Advertencia("Oleadas", "WaveSpawner no encontro una posicion valida en NavMesh para generar enemigo.", this);
            }

            onEnemySpawned?.Invoke(null);
            yield break;
        }

        hasLoggedInvalidSpawnPosition = false;
        PlaySpawnTelegraph(spawnPosition);
        yield return new WaitForSeconds(GetTelegraphDuration());
        spawnedEnemy = Instantiate(enemyPrefab, spawnPosition, spawnPoint.Rotation);
        ConfigureSpawnedEnemy(spawnedEnemy);
        onEnemySpawned?.Invoke(spawnedEnemy);
    }

    private bool HasSpawnPoints()
    {
        if (spawnPoints != null && spawnPoints.Length > 0)
        {
            return true;
        }

        if (!hasLoggedMissingSpawnPoints)
        {
            hasLoggedMissingSpawnPoints = true;
            GameDebug.Error("Oleadas", "WaveSpawner necesita al menos un EnemySpawnPoint asignado en inspector.", this);
        }

        return false;
    }

    private bool HasValidEnemyPrefab()
    {
        if (enemyPrefab != null)
        {
            return true;
        }

        if (!hasLoggedMissingEnemyPrefab)
        {
            hasLoggedMissingEnemyPrefab = true;
            GameDebug.Error("Oleadas", "WaveSpawner no tiene asignado el prefab de enemigo.", this);
        }

        return false;
    }

    private void ConfigureSpawnedEnemy(GameObject spawnedEnemy)
    {
        if (spawnedEnemy == null)
        {
            return;
        }

        EnemyController enemyController = spawnedEnemy.GetComponent<EnemyController>();

        if (enemyController == null)
        {
            enemyController = spawnedEnemy.GetComponentInChildren<EnemyController>(true);
        }

        if (enemyController == null)
        {
            return;
        }

        if (target == null && !hasLoggedMissingTarget)
        {
            hasLoggedMissingTarget = true;
            GameDebug.Advertencia("Oleadas", "WaveSpawner necesita el target del jugador asignado en inspector para configurar los enemigos.", this);
        }
        else if (target != null)
        {
            hasLoggedMissingTarget = false;
        }

        enemyController.ConfigureRuntimeContext(target, tacticsCoordinator);
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

    private EnemySpawnPoint GetRandomSpawnPoint()
    {
        if (!HasSpawnPoints())
        {
            return null;
        }

        int index = UnityEngine.Random.Range(0, spawnPoints.Length);
        return spawnPoints[index];
    }

    private void PlaySpawnTelegraph(Vector3 spawnPosition)
    {
        if (spawnTelegraphProfile == null)
        {
            return;
        }

        if (spawnTelegraphProfile.TelegraphVfxPrefab != null)
        {
            try
            {
                UnityEngine.Object instantiatedObject = Instantiate(
                    spawnTelegraphProfile.TelegraphVfxPrefab,
                    spawnPosition,
                    Quaternion.identity);

                if (instantiatedObject is GameObject telegraphVfx)
                {
                    hasLoggedInvalidTelegraphVfx = false;
                    Destroy(telegraphVfx, spawnTelegraphProfile.TelegraphLifetime);
                }
                else if (!hasLoggedInvalidTelegraphVfx)
                {
                    hasLoggedInvalidTelegraphVfx = true;
                    GameDebug.Advertencia("Oleadas", "El VFX de telegraph asignado no se pudo instanciar como GameObject. El spawn continuara sin efecto visual.", this);
                }
            }
            catch (Exception exception)
            {
                if (!hasLoggedInvalidTelegraphVfx)
                {
                    hasLoggedInvalidTelegraphVfx = true;
                    GameDebug.Advertencia("Oleadas", $"Fallo al instanciar el VFX de telegraph. El spawn continuara sin efecto visual. Detalle: {exception.Message}", this);
                }
            }
        }

        if (spawnTelegraphProfile.TelegraphSfx != null)
        {
            AudioSource.PlayClipAtPoint(
                spawnTelegraphProfile.TelegraphSfx,
                spawnPosition,
                spawnTelegraphProfile.TelegraphVolume);
        }
    }

    private float GetTelegraphDuration()
    {
        return spawnTelegraphProfile != null
            ? spawnTelegraphProfile.TelegraphDuration
            : 0.01f;
    }
}
