using UnityEngine;
using UnityEngine.AI;

public partial class EnemyController
{
    private void HandleCombat(bool canSeeTarget, Vector3 flatDirection, float flatDistance)
    {
        if (isEnraged)
        {
            HandleEnragedCombat(canSeeTarget, flatDirection, flatDistance);
            return;
        }

        if (canSeeTarget && target != null)
        {
            UpdateCombatMovementDecision(flatDistance);

            if (ShouldRefreshTacticalDestination() || HasReachedDestination(GetPatrolPointReachThreshold()))
            {
                RefreshVisibleCombatDestination(flatDirection, flatDistance);
            }

            if (hasTacticalDestination)
            {
                SetAgentDestination(tacticalDestination, GetPatrolPointReachThreshold());
            }
        }
        else
        {
            bool shouldChase = flatDistance > GetEffectiveStoppingDistance();

            if (shouldChase)
            {
                if (ShouldRefreshTacticalDestination())
                {
                    RefreshCombatDestination();
                }

                Vector3 combatDestination = hasTacticalDestination
                    ? tacticalDestination
                    : (hasLastKnownPlayerPosition ? lastKnownPlayerPosition : transform.position);
                SetAgentDestination(combatDestination, GetEffectiveStoppingDistance());
            }
            else
            {
                StopNavigation();
                ResetNavigationRecoveryState();
            }
        }

        MonitorNavigationRecovery(canSeeTarget);
        UpdateMovementPresentation(GetCombatFacingDirection(canSeeTarget ? flatDirection : Vector3.zero));

        bool canAttack = canSeeTarget &&
            flatDistance <= GetAttackRange() &&
            flatDirection.sqrMagnitude > 0.0001f &&
            Vector3.Dot(transform.forward, flatDirection.normalized) >= GetAttackAimDotThreshold();

        if (!canAttack)
        {
            ResetAttackWarmup();
            return;
        }

        if (attackWarmupTimer > 0f)
        {
            attackWarmupTimer -= Time.deltaTime;
            return;
        }

        if (weaponScript != null)
        {
            weaponScript.TryFire(GetTargetAimPoint());
        }
    }

    private void HandleEnragedCombat(bool canSeeTarget, Vector3 flatDirection, float flatDistance)
    {
        if (!IsEnragedTargetValid())
        {
            ExitEnragedState();
            SetState(AIState.Patrol, false);
            return;
        }

        if (target != null)
        {
            RememberPlayerPosition(target.position);
        }

        bool shouldPushForward = !canSeeTarget || flatDistance > Mathf.Max(0.05f, GetAttackRange() * 0.9f);

        if (shouldPushForward)
        {
            if (ShouldRefreshTacticalDestination() || HasReachedDestination(GetPatrolPointReachThreshold()))
            {
                RefreshEnragedDestination();
            }

            if (hasTacticalDestination)
            {
                SetAgentDestination(tacticalDestination, Mathf.Max(0.05f, GetAttackRange() * 0.9f));
            }
        }
        else
        {
            StopNavigation();
            ResetNavigationRecoveryState();
        }

        MonitorNavigationRecovery(canSeeTarget);
        UpdateMovementPresentation(GetCombatFacingDirection(canSeeTarget ? flatDirection : GetDirectionToLastKnownPlayerPosition()));

        bool canAttack = canSeeTarget &&
            flatDistance <= GetAttackRange() &&
            flatDirection.sqrMagnitude > 0.0001f &&
            Vector3.Dot(transform.forward, flatDirection.normalized) >= GetAttackAimDotThreshold();

        if (!canAttack)
        {
            ResetAttackWarmup();
            return;
        }

        if (attackWarmupTimer > 0f)
        {
            attackWarmupTimer -= Time.deltaTime;
            return;
        }

        if (weaponScript != null)
        {
            weaponScript.TryFire(GetTargetAimPoint());
        }
    }

    private void RefreshVisibleCombatDestination(Vector3 flatDirection, float flatDistance)
    {
        if (TryResolveCombatMovePoint(flatDirection, flatDistance, out Vector3 destination))
        {
            SetTacticalDestination(destination, GetCombatMovePointRefreshInterval());
            return;
        }

        RefreshCombatDestination();
    }

    private void RefreshEnragedDestination()
    {
        if (target == null && !hasLastKnownPlayerPosition)
        {
            return;
        }

        Vector3 center = target != null ? target.position : lastKnownPlayerPosition;

        if (TryResolveFallbackDestination(center, Mathf.Max(0.35f, GetCombatAdvanceDistance()), out Vector3 destination))
        {
            SetTacticalDestination(destination, GetCombatMovePointRefreshInterval());
            return;
        }

        SetTacticalDestination(center, GetCombatMovePointRefreshInterval());
    }

    private void RefreshCombatDestination()
    {
        Vector3 center = target != null ? target.position : (hasLastKnownPlayerPosition ? lastKnownPlayerPosition : transform.position);

        if (!TryResolveCombatDestination(center, out Vector3 destination))
        {
            destination = center;
        }

        SetTacticalDestination(destination);
    }

    private void RefreshInvestigateDestination()
    {
        Vector3 center = hasLastKnownPlayerPosition ? lastKnownPlayerPosition : transform.position;

        if (!TryResolveInvestigateDestination(center, out Vector3 destination))
        {
            destination = center;
        }

        SetTacticalDestination(destination);
    }

    private bool TryResolveCombatDestination(Vector3 center, out Vector3 destination)
    {
        if (tacticsCoordinator != null &&
            tacticsCoordinator.RequestCombatSlot(this, center, GetSlotInnerRadius(), GetSlotOuterRadius(), GetSlotInnerCount(), GetSlotOuterCount(), GetAreaMask(), out destination))
        {
            return true;
        }

        return TryResolveFallbackDestination(center, GetSlotOuterRadius(), out destination);
    }

    private bool TryResolveInvestigateDestination(Vector3 center, out Vector3 destination)
    {
        if (tacticsCoordinator != null &&
            tacticsCoordinator.RequestInvestigatePoint(this, center, GetSlotInnerRadius(), GetSlotOuterRadius(), GetSlotInnerCount(), GetSlotOuterCount(), GetAreaMask(), out destination))
        {
            return true;
        }

        return TryResolveFallbackDestination(center, GetSlotOuterRadius(), out destination);
    }

    private bool TryResolveFallbackDestination(Vector3 center, float radius, out Vector3 destination)
    {
        float searchRadius = Mathf.Max(0.35f, radius);

        for (int attempt = 0; attempt < PatrolPointSearchAttempts; attempt++)
        {
            Vector2 offset2D = Random.insideUnitCircle * searchRadius;
            Vector3 candidate = center + new Vector3(offset2D.x, 0f, offset2D.y);

            if (NavMesh.SamplePosition(candidate + (Vector3.up * 0.15f), out NavMeshHit hit, searchRadius, GetAreaMask()))
            {
                destination = hit.position;
                return true;
            }
        }

        destination = center;
        return false;
    }

    private void SetTacticalDestination(Vector3 destination)
    {
        SetTacticalDestination(destination, GetSlotRefreshInterval());
    }

    private void SetTacticalDestination(Vector3 destination, float refreshInterval)
    {
        tacticalDestination = destination;
        hasTacticalDestination = true;
        forceDestinationRefresh = false;
        destinationRefreshTimer = Mathf.Max(0.05f, refreshInterval);
        ResetNavigationRecoveryState();
    }

    private bool ShouldRefreshTacticalDestination()
    {
        return forceDestinationRefresh || !hasTacticalDestination || destinationRefreshTimer <= 0f;
    }

    private void ForceDestinationRefresh()
    {
        forceDestinationRefresh = true;
        destinationRefreshTimer = 0f;
        ResetNavigationRecoveryState();
    }

    private void MonitorNavigationRecovery(bool canSeeTarget)
    {
        MonitorNavigationRecoveryState(canSeeTarget);
    }

    private void ResetNavigationRecoveryState()
    {
        ResetNavigationRecoveryStateInternal();
    }

    private void UpdateCombatMovementDecision(float flatDistance)
    {
        if (combatDecisionTimer > 0f)
        {
            return;
        }

        float preferredDistance = Mathf.Max(0.05f, GetPreferredCombatDistance());
        float tolerance = Mathf.Max(0.01f, GetPreferredDistanceTolerance());

        if (flatDistance > preferredDistance + tolerance)
        {
            currentCombatMovementMode = CombatMovementMode.Advance;
        }
        else if (flatDistance < preferredDistance - tolerance)
        {
            currentCombatMovementMode = CombatMovementMode.Retreat;
        }
        else
        {
            if (combatSidePreferenceTimer <= 0f)
            {
                preferredCombatSideSign = -preferredCombatSideSign;

                if (preferredCombatSideSign == 0)
                {
                    preferredCombatSideSign = 1;
                }

                combatSidePreferenceTimer = Mathf.Max(0.8f, GetCombatDecisionInterval() * 2f);
            }

            currentCombatMovementMode = preferredCombatSideSign < 0
                ? CombatMovementMode.StrafeLeft
                : CombatMovementMode.StrafeRight;
        }

        combatDecisionTimer = Mathf.Max(0.1f, GetCombatDecisionInterval());
    }

    private bool TryResolveCombatMovePoint(Vector3 flatDirection, float flatDistance, out Vector3 destination)
    {
        destination = transform.position;

        if (target == null)
        {
            return false;
        }

        Vector3 toPlayer = flatDirection.sqrMagnitude > 0.0001f
            ? flatDirection.normalized
            : GetCombatFacingDirection(Vector3.zero).normalized;

        if (toPlayer.sqrMagnitude <= 0.0001f)
        {
            toPlayer = transform.forward;
        }

        int preferredSideSign = GetCurrentCombatSideSign();
        float desiredDistanceFromPlayer = flatDistance;
        float lateralDistance = 0f;
        float jitter = Mathf.Max(0f, GetCombatMovementJitter());

        switch (currentCombatMovementMode)
        {
            case CombatMovementMode.Advance:
                desiredDistanceFromPlayer = Mathf.Max(0.05f, flatDistance - Mathf.Max(0.05f, GetCombatAdvanceDistance()));
                lateralDistance = Mathf.Max(0f, GetCombatMovementJitter() * Mathf.Max(0.1f, GetCombatAdvanceWeight()));
                break;
            case CombatMovementMode.Retreat:
                desiredDistanceFromPlayer = flatDistance + Mathf.Max(0.05f, GetCombatRetreatDistance());
                lateralDistance = Mathf.Max(0f, GetCombatMovementJitter() * Mathf.Max(0.1f, GetCombatRetreatWeight()));
                break;
            case CombatMovementMode.StrafeLeft:
            case CombatMovementMode.StrafeRight:
                desiredDistanceFromPlayer = Mathf.Max(0.05f, GetPreferredCombatDistance());
                lateralDistance = Mathf.Max(0.05f, GetCombatStrafeDistance() * Mathf.Max(0.5f, GetCombatLateralWeight()));
                break;
        }

        if (tacticsCoordinator != null &&
            tacticsCoordinator.RequestCombatMovePoint(
                this,
                target.position,
                toPlayer,
                desiredDistanceFromPlayer,
                lateralDistance,
                jitter,
                preferredSideSign,
                GetAreaMask(),
                out destination))
        {
            return true;
        }

        Vector3 right = Vector3.Cross(Vector3.up, toPlayer).normalized;
        Vector3 fallbackCandidate = target.position - (toPlayer * desiredDistanceFromPlayer);

        if (lateralDistance > 0f)
        {
            fallbackCandidate += right * (preferredSideSign * lateralDistance * 0.5f);
        }

        if (NavMesh.SamplePosition(
            fallbackCandidate + (Vector3.up * 0.15f),
            out NavMeshHit hit,
            Mathf.Max(0.35f, lateralDistance + jitter + 0.2f),
            GetAreaMask()))
        {
            destination = hit.position;
            return true;
        }

        if (TryResolveCombatDestination(target.position, out destination))
        {
            return true;
        }

        return TryResolveFallbackDestination(target.position, GetSlotOuterRadius(), out destination);
    }

    private Vector3 GetCombatFacingDirection(Vector3 visibleTargetDirection)
    {
        Vector3 direction = Vector3.ProjectOnPlane(visibleTargetDirection, Vector3.up);

        if (direction.sqrMagnitude > 0.0001f)
        {
            return direction;
        }

        return GetMovementFacingDirection();
    }

    private Vector3 GetMovementFacingDirection()
    {
        Vector3 direction = Vector3.ProjectOnPlane(measuredPlanarVelocity, Vector3.up);
        float deadzone = Mathf.Max(0.001f, GetMovementFacingDeadzone());

        if (direction.sqrMagnitude > deadzone * deadzone)
        {
            return direction;
        }

        direction = GetDirectionToCurrentDestination();

        if (direction.sqrMagnitude > 0.0001f)
        {
            return direction;
        }

        return lastMovementDirection.sqrMagnitude > 0.0001f ? lastMovementDirection : transform.forward;
    }

    private int GetCurrentCombatSideSign()
    {
        if (currentCombatMovementMode == CombatMovementMode.StrafeLeft) return -1;
        if (currentCombatMovementMode == CombatMovementMode.StrafeRight) return 1;
        return preferredCombatSideSign < 0 ? -1 : 1;
    }

    private Vector3 GetDirectionToCurrentDestination()
    {
        return hasTacticalDestination
            ? Vector3.ProjectOnPlane(tacticalDestination - transform.position, Vector3.up)
            : Vector3.zero;
    }

    private Vector3 GetDirectionToLastKnownPlayerPosition()
    {
        return hasLastKnownPlayerPosition
            ? Vector3.ProjectOnPlane(lastKnownPlayerPosition - transform.position, Vector3.up)
            : Vector3.zero;
    }

    private void ConfigureNavigation()
    {
        ConfigureNavigationState();
    }

    private bool SetAgentDestination(Vector3 destination, float desiredStoppingDistance)
    {
        return SetAgentDestinationInternal(destination, desiredStoppingDistance);
    }

    private bool HasReachedDestination(float reachThreshold)
    {
        return HasReachedDestinationInternal(reachThreshold);
    }

    private void StopNavigation()
    {
        StopNavigationInternal();
    }

    private bool IsNavigationAvailable() => IsNavigationAvailableInternal();
    private int GetAreaMask() => GetAreaMaskInternal();
}
