using UnityEngine;
using UnityEngine.AI;

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
    [SerializeField] private float navMeshSampleDistance = 2f;

    private bool hasLoggedMissingSpawnPoints;
    private bool hasLoggedMissingEnemyPrefab;
    private bool hasLoggedInvalidSpawnPosition;
    private bool hasLoggedMissingTarget;

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

        int index = Random.Range(0, spawnPoints.Length);
        return spawnPoints[index];
    }
}
