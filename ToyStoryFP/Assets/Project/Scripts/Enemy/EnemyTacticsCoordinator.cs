using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[DisallowMultipleComponent]
public class EnemyTacticsCoordinator : MonoBehaviour
{
    private static EnemyTacticsCoordinator cachedInstance;

    private readonly HashSet<EnemyController> registeredEnemies = new HashSet<EnemyController>();
    private readonly Dictionary<EnemyController, int> reservedSlotIndices = new Dictionary<EnemyController, int>();
    private readonly Dictionary<EnemyController, int> reservedCombatSideSigns = new Dictionary<EnemyController, int>();

    // Gestiona resolver.
    public static EnemyTacticsCoordinator Resolve()
    {
        if (cachedInstance != null)
        {
            return cachedInstance;
        }

        cachedInstance = FindFirstObjectByType<EnemyTacticsCoordinator>();
        return cachedInstance;
    }

    void Awake()
    {
        if (cachedInstance != null && cachedInstance != this)
        {
            GameDebug.Advertencia("IA", "EnemyTacticsCoordinator detecto multiples instancias activas. Se desactiva el duplicado.", this);
            enabled = false;
            return;
        }

        cachedInstance = this;
    }

    void OnDestroy()
    {
        if (cachedInstance == this)
        {
            cachedInstance = null;
        }
    }

    // Gestiona register enemigo.
    public void RegisterEnemy(EnemyController enemy, int avoidancePriorityMin, int avoidancePriorityMax)
    {
        if (enemy == null)
        {
            return;
        }

        CleanupRegistry();
        registeredEnemies.Add(enemy);

        int clampedMin = Mathf.Clamp(Mathf.Min(avoidancePriorityMin, avoidancePriorityMax), 0, 99);
        int clampedMax = Mathf.Clamp(Mathf.Max(avoidancePriorityMin, avoidancePriorityMax), 0, 99);
        enemy.SetAvoidancePriority(Random.Range(clampedMin, clampedMax + 1));
    }

    // Gestiona unregister enemigo.
    public void UnregisterEnemy(EnemyController enemy)
    {
        if (enemy == null)
        {
            return;
        }

        registeredEnemies.Remove(enemy);
        reservedSlotIndices.Remove(enemy);
        reservedCombatSideSigns.Remove(enemy);
    }

    // Gestiona broadcast aggro.
    public void BroadcastAggro(EnemyController source, Vector3 playerPosition, float radius, Transform aggressor)
    {
        CleanupRegistry();
        float maxDistanceSqr = Mathf.Max(0f, radius) * Mathf.Max(0f, radius);

        foreach (EnemyController enemy in registeredEnemies)
        {
            if (enemy == null || enemy == source)
            {
                continue;
            }

            if ((enemy.transform.position - source.transform.position).sqrMagnitude > maxDistanceSqr)
            {
                continue;
            }

            enemy.NotifyAllyAlert(playerPosition, aggressor);
        }
    }

    // Gestiona solicitud combate ranura.
    public bool RequestCombatSlot(
        EnemyController requester,
        Vector3 center,
        float innerRadius,
        float outerRadius,
        int innerCount,
        int outerCount,
        int areaMask,
        out Vector3 slotPosition)
    {
        return RequestSlot(requester, center, innerRadius, outerRadius, innerCount, outerCount, areaMask, out slotPosition);
    }

    // Gestiona solicitud investigación punto.
    public bool RequestInvestigatePoint(
        EnemyController requester,
        Vector3 center,
        float innerRadius,
        float outerRadius,
        int innerCount,
        int outerCount,
        int areaMask,
        out Vector3 slotPosition)
    {
        return RequestSlot(requester, center, innerRadius, outerRadius, innerCount, outerCount, areaMask, out slotPosition);
    }

    // Gestiona solicitud combate movimiento punto.
    public bool RequestCombatMovePoint(
        EnemyController requester,
        Vector3 playerPosition,
        Vector3 playerDirection,
        float desiredDistanceFromPlayer,
        float lateralDistance,
        float jitterRadius,
        int preferredSideSign,
        int areaMask,
        out Vector3 slotPosition)
    {
        slotPosition = playerPosition;

        if (requester == null)
        {
            return false;
        }

        CleanupRegistry();

        Vector3 flatPlayerDirection = Vector3.ProjectOnPlane(playerDirection, Vector3.up);
        if (flatPlayerDirection.sqrMagnitude <= 0.0001f)
        {
            return false;
        }

        Vector3 forward = flatPlayerDirection.normalized;
        Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
        int stablePreferredSide = preferredSideSign < 0 ? -1 : 1;
        int[] sideCandidates = { stablePreferredSide, -stablePreferredSide, 0 };
        float bestScore = float.PositiveInfinity;
        int bestSide = 0;
        Vector3 bestPosition = playerPosition;

        for (int i = 0; i < sideCandidates.Length; i++)
        {
            int sideSign = sideCandidates[i];
            float deterministicJitter = GetDeterministicJitter(requester, jitterRadius);
            Vector3 candidatePosition =
                playerPosition -
                (forward * Mathf.Max(0.05f, desiredDistanceFromPlayer)) +
                (right * (Mathf.Max(0f, lateralDistance) * sideSign));

            if (sideSign != 0)
            {
                candidatePosition += right * deterministicJitter;
                candidatePosition += forward * (deterministicJitter * 0.35f);
            }

            if (!TrySampleSlot(candidatePosition, areaMask, out Vector3 sampledPosition))
            {
                continue;
            }

            float score =
                Vector3.Distance(requester.transform.position, sampledPosition) +
                GetCombatMovePenalty(requester, sideSign, sampledPosition);

            if (sideSign == stablePreferredSide)
            {
                score -= 0.25f;
            }

            if (score >= bestScore)
            {
                continue;
            }

            bestScore = score;
            bestSide = sideSign;
            bestPosition = sampledPosition;
        }

        if (float.IsInfinity(bestScore))
        {
            return false;
        }

        reservedCombatSideSigns[requester] = bestSide;
        slotPosition = bestPosition;
        return true;
    }

    // Gestiona solicitud ranura.
    private bool RequestSlot(
        EnemyController requester,
        Vector3 center,
        float innerRadius,
        float outerRadius,
        int innerCount,
        int outerCount,
        int areaMask,
        out Vector3 slotPosition)
    {
        slotPosition = center;

        if (requester == null)
        {
            return false;
        }

        CleanupRegistry();

        int sanitizedInnerCount = Mathf.Max(1, innerCount);
        int sanitizedOuterCount = Mathf.Max(1, outerCount);
        float sanitizedInnerRadius = Mathf.Max(0.05f, innerRadius);
        float sanitizedOuterRadius = Mathf.Max(sanitizedInnerRadius + 0.05f, outerRadius);
        int totalSlots = sanitizedInnerCount + sanitizedOuterCount;
        int preferredSlotIndex = reservedSlotIndices.TryGetValue(requester, out int existingSlotIndex)
            ? existingSlotIndex
            : -1;

        float bestScore = float.PositiveInfinity;
        int bestSlotIndex = -1;
        Vector3 bestSlotPosition = center;

        for (int slotIndex = 0; slotIndex < totalSlots; slotIndex++)
        {
            GetSlotDefinition(
                slotIndex,
                sanitizedInnerCount,
                sanitizedOuterCount,
                sanitizedInnerRadius,
                sanitizedOuterRadius,
                out float slotRadius,
                out float slotAngleDegrees);

            Vector3 candidatePosition = center +
                Quaternion.Euler(0f, slotAngleDegrees, 0f) * (Vector3.forward * slotRadius);

            if (!TrySampleSlot(candidatePosition, areaMask, out Vector3 sampledPosition))
            {
                continue;
            }

            float score = Vector3.Distance(requester.transform.position, sampledPosition) +
                GetReservationPenalty(requester, slotIndex, sampledPosition);

            if (slotIndex == preferredSlotIndex)
            {
                score -= 0.35f;
            }

            if (score >= bestScore)
            {
                continue;
            }

            bestScore = score;
            bestSlotIndex = slotIndex;
            bestSlotPosition = sampledPosition;
        }

        if (bestSlotIndex < 0)
        {
            return false;
        }

        reservedSlotIndices[requester] = bestSlotIndex;
        slotPosition = bestSlotPosition;
        return true;
    }

    // Obtiene ranura definition.
    private void GetSlotDefinition(
        int slotIndex,
        int innerCount,
        int outerCount,
        float innerRadius,
        float outerRadius,
        out float radius,
        out float angleDegrees)
    {
        if (slotIndex < innerCount)
        {
            radius = innerRadius;
            angleDegrees = (360f / innerCount) * slotIndex;
            return;
        }

        int outerIndex = slotIndex - innerCount;
        radius = outerRadius;
        angleDegrees = ((360f / outerCount) * outerIndex) + (180f / outerCount);
    }

    // Intenta muestra ranura.
    private bool TrySampleSlot(Vector3 candidatePosition, int areaMask, out Vector3 sampledPosition)
    {
        sampledPosition = candidatePosition;

        if (NavMesh.SamplePosition(candidatePosition + (Vector3.up * 0.15f), out NavMeshHit hit, 0.75f, areaMask))
        {
            sampledPosition = hit.position;
            return true;
        }

        return false;
    }

    // Obtiene reservation penalty.
    private float GetReservationPenalty(EnemyController requester, int slotIndex, Vector3 candidatePosition)
    {
        float penalty = 0f;

        foreach (KeyValuePair<EnemyController, int> reservation in reservedSlotIndices)
        {
            EnemyController otherEnemy = reservation.Key;

            if (otherEnemy == null || otherEnemy == requester)
            {
                continue;
            }

            if (reservation.Value == slotIndex)
            {
                penalty += 2.5f;
            }

            if (Vector3.Distance(otherEnemy.transform.position, candidatePosition) < 0.45f)
            {
                penalty += 1.5f;
            }
        }

        return penalty;
    }

    // Obtiene combate movimiento penalty.
    private float GetCombatMovePenalty(EnemyController requester, int sideSign, Vector3 candidatePosition)
    {
        float penalty = 0f;

        foreach (EnemyController otherEnemy in registeredEnemies)
        {
            if (otherEnemy == null || otherEnemy == requester)
            {
                continue;
            }

            if (reservedCombatSideSigns.TryGetValue(otherEnemy, out int reservedSide) &&
                sideSign != 0 &&
                reservedSide == sideSign)
            {
                penalty += 0.75f;
            }

            if (Vector3.Distance(otherEnemy.transform.position, candidatePosition) < 0.4f)
            {
                penalty += 2f;
            }
        }

        return penalty;
    }

    // Obtiene deterministic jitter.
    private float GetDeterministicJitter(EnemyController requester, float jitterRadius)
    {
        if (requester == null || jitterRadius <= 0f)
        {
            return 0f;
        }

        float seed = Mathf.Abs(requester.GetInstanceID() * 0.173f);
        float normalized = Mathf.Repeat(seed, 1f);
        return Mathf.Lerp(-jitterRadius, jitterRadius, normalized);
    }

    // Gestiona limpieza registry.
    private void CleanupRegistry()
    {
        registeredEnemies.RemoveWhere(enemy => enemy == null);

        if (reservedSlotIndices.Count == 0)
        {
            CleanupCombatReservations();
            return;
        }

        List<EnemyController> missingEnemies = null;

        foreach (KeyValuePair<EnemyController, int> reservation in reservedSlotIndices)
        {
            if (reservation.Key != null)
            {
                continue;
            }

            missingEnemies ??= new List<EnemyController>();
            missingEnemies.Add(reservation.Key);
        }

        if (missingEnemies == null)
        {
            CleanupCombatReservations();
            return;
        }

        for (int i = 0; i < missingEnemies.Count; i++)
        {
            reservedSlotIndices.Remove(missingEnemies[i]);
        }

        CleanupCombatReservations();
    }

    // Gestiona limpieza combate reservations.
    private void CleanupCombatReservations()
    {
        if (reservedCombatSideSigns.Count == 0)
        {
            return;
        }

        List<EnemyController> missingEnemies = null;

        foreach (KeyValuePair<EnemyController, int> reservation in reservedCombatSideSigns)
        {
            if (reservation.Key != null)
            {
                continue;
            }

            missingEnemies ??= new List<EnemyController>();
            missingEnemies.Add(reservation.Key);
        }

        if (missingEnemies == null)
        {
            return;
        }

        for (int i = 0; i < missingEnemies.Count; i++)
        {
            reservedCombatSideSigns.Remove(missingEnemies[i]);
        }
    }
}
