using UnityEngine;

public partial class EnemyController
{
    private void UpdateTimers(bool canSeeTarget)
    {
        if (!canSeeTarget && loseSightTimer > 0f) loseSightTimer -= Time.deltaTime;
        if (combatDecisionTimer > 0f) combatDecisionTimer -= Time.deltaTime;
        if (combatSidePreferenceTimer > 0f) combatSidePreferenceTimer -= Time.deltaTime;
        if (damageAggroTimer > 0f) damageAggroTimer -= Time.deltaTime;
        if (damageSpeedBoostTimer > 0f) damageSpeedBoostTimer -= Time.deltaTime;
        if (currentState == AIState.Investigate && investigationTimer > 0f) investigationTimer -= Time.deltaTime;
        if (destinationRefreshTimer > 0f) destinationRefreshTimer -= Time.deltaTime;
        if (stuckCheckTimer > 0f) stuckCheckTimer -= Time.deltaTime;
    }

    private void UpdateAwarenessState(bool hasTarget, bool canSeeTarget)
    {
        if (isEnraged)
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

            if (canSeeTarget)
            {
                loseSightTimer = GetLoseSightGraceTime();
            }

            SetState(AIState.Combat, canSeeTarget);
            return;
        }

        if (canSeeTarget)
        {
            loseSightTimer = GetLoseSightGraceTime();
            investigationTimer = Mathf.Max(investigationTimer, Mathf.Max(0.1f, GetInvestigationDuration()));
            SetState(AIState.Combat, true);
            return;
        }

        if (currentState == AIState.Combat)
        {
            if (loseSightTimer > 0f)
            {
                return;
            }

            if (hasLastKnownPlayerPosition || damageAggroTimer > 0f || investigationTimer > 0f)
            {
                investigationTimer = Mathf.Max(investigationTimer, Mathf.Max(0.1f, GetInvestigationDuration()));
                SetState(AIState.Investigate, false);
                return;
            }

            SetState(AIState.Patrol, false);
            return;
        }

        if ((damageAggroTimer > 0f || investigationTimer > 0f) && (hasLastKnownPlayerPosition || hasTarget))
        {
            if (!hasLastKnownPlayerPosition && hasTarget && target != null)
            {
                RememberPlayerPosition(target.position);
            }

            SetState(AIState.Investigate, false);
            return;
        }

        if (currentState == AIState.Investigate && investigationTimer <= 0f && damageAggroTimer <= 0f)
        {
            ClearLastKnownPlayerPosition();
            SetState(AIState.Patrol, false);
        }
    }

    private void HandlePatrol()
    {
        HandlePatrolState();
    }

    private void HandleInvestigate()
    {
        HandleInvestigateState();
    }

    private void SetState(AIState nextState, bool playPulse)
    {
        bool changedState = currentState != nextState;
        bool wasAlerted = currentState != AIState.Patrol;
        currentState = nextState;

        if (nextState == AIState.Patrol)
        {
            StopNavigation();
            hasTacticalDestination = false;
            forceDestinationRefresh = false;
            hasPatrolDestination = false;
            ResetCombatMovementState();
            ResetNavigationRecoveryState();
            loseSightTimer = 0f;
            ResetAttackWarmup();
        }
        else if (changedState && !wasAlerted)
        {
            hasPatrolDestination = false;
            patrolRetargetTimer = 0f;
            ResetCombatMovementState();
            enemyAudio?.PlayAlert();
        }
        else if (nextState != AIState.Patrol)
        {
            hasPatrolDestination = false;
        }

        if (alertIndicator != null)
        {
            alertIndicator.SetVisible(nextState != AIState.Patrol, playPulse && changedState && nextState != AIState.Patrol);
        }

        UpdateAgentSpeedByState();
    }

    private void RememberPlayerPosition(Vector3 worldPosition)
    {
        lastKnownPlayerPosition = worldPosition;
        hasLastKnownPlayerPosition = true;
    }

    private void ClearLastKnownPlayerPosition()
    {
        hasLastKnownPlayerPosition = false;
    }

    private void ResetCombatMovementState()
    {
        combatDecisionTimer = 0f;
        combatSidePreferenceTimer = 0f;
        currentCombatMovementMode = preferredCombatSideSign < 0
            ? CombatMovementMode.StrafeLeft
            : CombatMovementMode.StrafeRight;
    }

    private void ResetAttackWarmup()
    {
        attackWarmupTimer = GetAttackWarmup();
    }

    private void UpdateMeasuredPlanarVelocity()
    {
        Vector3 currentPosition = transform.position;
        Vector3 frameDelta = Vector3.ProjectOnPlane(currentPosition - lastMeasuredPosition, Vector3.up);
        float deltaTime = Mathf.Max(Time.deltaTime, 0.0001f);
        measuredPlanarVelocity = frameDelta / deltaTime;
        lastMeasuredPosition = currentPosition;
    }

    private void ResetMeasuredMotion()
    {
        lastMeasuredPosition = transform.position;
        measuredPlanarVelocity = Vector3.zero;
    }

    private void UpdateMovementPresentation(Vector3 facingDirection)
    {
        movementScript.SetMoveInput(Vector2.zero);

        if (facingDirection.sqrMagnitude > 0.0001f)
        {
            movementScript.FaceDirection(facingDirection, GetTurnSpeed());
            lastMovementDirection = facingDirection.normalized;
        }

        movementScript.SetExternalMovementAnimation(
            measuredPlanarVelocity,
            IsNavigationAvailable(),
            GetAnimationSpeedReference(),
            GetMinimumMoveBlend(),
            GetAnimationMoveThreshold());
    }

    private void UpdateAgentSpeedByState()
    {
        if (navMeshAgent == null)
        {
            return;
        }

        if (baseNavSpeed <= 0f)
        {
            baseNavSpeed = Mathf.Max(0.01f, navMeshAgent.speed);
        }

        bool isAlerted = currentState != AIState.Patrol;
        float appliedMultiplier = 1f;

        if (isAlerted)
        {
            appliedMultiplier *= Mathf.Max(1f, GetAlertSpeedMultiplier());

            if (damageSpeedBoostTimer > 0f)
            {
                appliedMultiplier *= Mathf.Max(1f, GetDamageSpeedMultiplier());
            }
        }

        navMeshAgent.speed = Mathf.Max(0.01f, baseNavSpeed * appliedMultiplier);
    }
}
