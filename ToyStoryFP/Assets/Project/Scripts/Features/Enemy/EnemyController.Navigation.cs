using UnityEngine;
using UnityEngine.AI;

public partial class EnemyController
{
    private void HandlePatrolState()
    {
        ResetAttackWarmup();
        UpdatePatrolNavigationState();
        UpdateMovementPresentation(GetMovementFacingDirection());
    }

    private void HandleInvestigateState()
    {
        ResetAttackWarmup();
        if (!hasLastKnownPlayerPosition)
        {
            SetState(AIState.Patrol, false);
            HandlePatrolState();
            return;
        }

        if (ShouldRefreshTacticalDestination() || HasReachedDestinationInternal(GetPatrolPointReachThreshold()))
        {
            RefreshInvestigateDestination();
        }

        if (hasTacticalDestination)
        {
            SetAgentDestinationInternal(tacticalDestination, GetPatrolPointReachThreshold());
        }

        MonitorNavigationRecoveryState(false);
        UpdateMovementPresentation(GetMovementFacingDirection());

        if (HasReachedDestinationInternal(GetPatrolPointReachThreshold()) && investigationTimer <= 0f && damageAggroTimer <= 0f)
        {
            ClearLastKnownPlayerPosition();
            SetState(AIState.Patrol, false);
        }
    }

    private void MonitorNavigationRecoveryState(bool canSeeTarget)
    {
        if (!hasTacticalDestination || !IsNavigationAvailableInternal())
        {
            ResetNavigationRecoveryStateInternal();
            return;
        }

        if (navMeshAgent.pathPending)
        {
            return;
        }

        if (!navMeshAgent.hasPath || navMeshAgent.pathStatus != NavMeshPathStatus.PathComplete)
        {
            ForceDestinationRefresh();
            return;
        }

        if (stuckCheckTimer > 0f)
        {
            return;
        }

        float checkInterval = Mathf.Max(0.05f, GetStuckCheckInterval());
        stuckCheckTimer = checkInterval;
        float remainingDistance = navMeshAgent.remainingDistance;

        if (float.IsInfinity(previousRemainingDistance) ||
            remainingDistance < previousRemainingDistance - Mathf.Max(0.001f, GetStuckProgressThreshold()))
        {
            timeWithoutProgress = 0f;
        }
        else
        {
            timeWithoutProgress += checkInterval;
        }

        previousRemainingDistance = remainingDistance;
        bool wallAhead = !canSeeTarget && ProbeWallAheadInternal();

        if (wallAhead || timeWithoutProgress >= Mathf.Max(0.1f, GetStuckTimeout()))
        {
            ForceDestinationRefresh();
        }
    }

    private bool ProbeWallAheadInternal()
    {
        Vector3 probeDirection = GetDirectionToCurrentDestination();
        if (probeDirection.sqrMagnitude <= 0.0001f)
        {
            probeDirection = lastMovementDirection.sqrMagnitude > 0.0001f ? lastMovementDirection : transform.forward;
        }

        Vector3 flattenedDirection = Vector3.ProjectOnPlane(probeDirection, Vector3.up);
        if (flattenedDirection.sqrMagnitude <= 0.0001f)
        {
            return false;
        }

        Vector3 rayOrigin = transform.position + (Vector3.up * Mathf.Max(0.05f, GetEyeHeight() * 0.5f));
        int mask = visionBlockLayers.value != 0 ? visionBlockLayers.value : Physics.DefaultRaycastLayers;

        if (!Physics.Raycast(rayOrigin, flattenedDirection.normalized, out RaycastHit hit, Mathf.Max(0.05f, GetWallProbeDistance()), mask, QueryTriggerInteraction.Ignore))
        {
            return false;
        }

        return target == null || (hit.transform != target && !hit.transform.IsChildOf(target));
    }

    private void ResetNavigationRecoveryStateInternal()
    {
        timeWithoutProgress = 0f;
        previousRemainingDistance = float.PositiveInfinity;
        stuckCheckTimer = 0f;
    }

    private void ConfigureNavigationState()
    {
        if (navMeshAgent == null)
        {
            return;
        }

        baseNavSpeed = Mathf.Max(0.01f, navMeshAgent.speed);
        navMeshAgent.updateRotation = false;
        navMeshAgent.stoppingDistance = GetEffectiveStoppingDistance();
        UpdateAgentSpeedByState();

        Rigidbody rigidbody = GetComponent<Rigidbody>();
        if (rigidbody != null)
        {
            rigidbody.isKinematic = true;
        }

        EnsureAgentOnNavMeshInternal();
    }

    private void UpdatePatrolNavigationState()
    {
        if (!EnsureAgentOnNavMeshInternal())
        {
            hasPatrolDestination = false;
            return;
        }

        if (hasPatrolDestination && HasReachedDestinationInternal(GetPatrolPointReachThreshold()))
        {
            StopNavigationInternal();
            hasPatrolDestination = false;
            patrolRetargetTimer = GetPatrolRetargetDelay();
        }

        if (hasPatrolDestination)
        {
            return;
        }

        if (patrolRetargetTimer > 0f)
        {
            patrolRetargetTimer -= Time.deltaTime;
            return;
        }

        if (!TrySetNextPatrolDestinationState())
        {
            patrolRetargetTimer = GetPatrolRetargetDelay();
        }
    }

    private bool TrySetNextPatrolDestinationState()
    {
        if (navMeshAgent == null)
        {
            return false;
        }

        float radius = GetPatrolSearchRadius();
        float minTravelDistance = GetPatrolMinTravelDistance();
        float minTravelDistanceSqr = minTravelDistance * minTravelDistance;

        for (int attempt = 0; attempt < PatrolPointSearchAttempts; attempt++)
        {
            Vector2 offset2D = Random.insideUnitCircle * radius;
            Vector3 candidate = transform.position + new Vector3(offset2D.x, 0f, offset2D.y);

            if ((candidate - transform.position).sqrMagnitude < minTravelDistanceSqr)
            {
                continue;
            }

            if (!NavMesh.SamplePosition(candidate, out NavMeshHit hit, radius, navMeshAgent.areaMask))
            {
                continue;
            }

            Vector3 travel = Vector3.ProjectOnPlane(hit.position - transform.position, Vector3.up);
            if (travel.sqrMagnitude < minTravelDistanceSqr)
            {
                continue;
            }

            if (!SetAgentDestinationInternal(hit.position, GetPatrolPointReachThreshold()))
            {
                continue;
            }

            hasPatrolDestination = true;
            return true;
        }

        return false;
    }

    private bool SetAgentDestinationInternal(Vector3 destination, float desiredStoppingDistance)
    {
        if (!EnsureAgentOnNavMeshInternal())
        {
            return false;
        }

        navMeshAgent.stoppingDistance = Mathf.Max(0.01f, desiredStoppingDistance);
        if (navMeshAgent.isStopped)
        {
            navMeshAgent.isStopped = false;
        }

        return navMeshAgent.SetDestination(destination);
    }

    private bool HasReachedDestinationInternal(float reachThreshold)
    {
        if (!IsNavigationAvailableInternal() || navMeshAgent.pathPending || !navMeshAgent.hasPath)
        {
            return false;
        }

        return navMeshAgent.remainingDistance <= Mathf.Max(0.01f, reachThreshold);
    }

    private void StopNavigationInternal()
    {
        if (navMeshAgent == null || !navMeshAgent.isOnNavMesh)
        {
            return;
        }

        navMeshAgent.isStopped = true;
        navMeshAgent.ResetPath();
    }

    private bool EnsureAgentOnNavMeshInternal()
    {
        if (navMeshAgent == null)
        {
            return false;
        }

        if (navMeshAgent.isOnNavMesh)
        {
            return true;
        }

        if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, DefaultNavMeshSnapDistance, navMeshAgent.areaMask))
        {
            navMeshAgent.Warp(hit.position);
            return navMeshAgent.isOnNavMesh;
        }

        return false;
    }

    private bool IsNavigationAvailableInternal() => navMeshAgent != null && navMeshAgent.isOnNavMesh;
    private Vector3 GetPlanarAgentVelocityInternal() => IsNavigationAvailableInternal() ? Vector3.ProjectOnPlane(navMeshAgent.velocity, Vector3.up) : Vector3.zero;
    private int GetAreaMaskInternal() => navMeshAgent != null ? navMeshAgent.areaMask : NavMesh.AllAreas;
}
