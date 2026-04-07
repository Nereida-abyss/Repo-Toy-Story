using UnityEngine;
using UnityEngine.AI;

[DisallowMultipleComponent]
[RequireComponent(typeof(MovementScript))]
[RequireComponent(typeof(NavMeshAgent))]
public class EnemyController : MonoBehaviour
{
    private enum AIState { Patrol, Investigate, Combat }
    private enum CombatMovementMode { Advance, StrafeLeft, StrafeRight, Retreat }

    private const float DefaultDetectionRange = 10f;
    private const float DefaultLoseSightGraceTime = 0.6f;
    private const float DefaultEyeHeight = 1.4f;
    private const float DefaultAlertHeightOffset = 1.8f;
    private const float DefaultStoppingDistance = 4.5f;
    private const float DefaultAttackRange = 7f;
    private const float DefaultNavMeshSnapDistance = 1f;
    private const float DefaultPatrolPointReachThreshold = 0.25f;
    private const float DefaultPatrolRetargetDelay = 0.35f;
    private const float DefaultPatrolSearchRadius = 6f;
    private const float DefaultPatrolMinTravelDistance = 2f;
    private const int PatrolPointSearchAttempts = 8;

    [Header("Target")]
    [SerializeField] private Transform target;
    [SerializeField] private float targetAimHeight = 0.15f;
    [SerializeField] private float detectionRange = DefaultDetectionRange;
    [SerializeField] private float loseSightGraceTime = DefaultLoseSightGraceTime;
    [SerializeField] private Transform eyeOrigin;
    [SerializeField] private float eyeHeight = DefaultEyeHeight;
    [SerializeField] private LayerMask visionBlockLayers;
    [SerializeField] private Transform alertAnchor;
    [SerializeField] private float alertHeightOffset = DefaultAlertHeightOffset;

    [Header("Movement")]
    [SerializeField] private float stoppingDistance = DefaultStoppingDistance;
    [SerializeField] private float attackRange = DefaultAttackRange;
    [SerializeField] private float turnSpeed = 360f;
    [SerializeField] private float alertSpeedMultiplier = 2f;
    [SerializeField] private float damageSpeedMultiplier = 1.7f;
    [SerializeField] private float damageSpeedBoostDuration = 3f;

    [Header("Patrol")]
    [SerializeField] private float patrolPointReachThreshold = DefaultPatrolPointReachThreshold;
    [SerializeField] private float patrolRetargetDelay = DefaultPatrolRetargetDelay;
    [SerializeField] private float patrolSearchRadius = DefaultPatrolSearchRadius;
    [SerializeField] private float patrolMinTravelDistance = DefaultPatrolMinTravelDistance;

    [Header("Combat")]
    [SerializeField] private float attackWarmup = 0.65f;
    [Range(0f, 1f)] [SerializeField] private float attackAimDotThreshold = 0.92f;

    [Header("Tactics")]
    [SerializeField] private float allyAlertRadius = 2f;
    [SerializeField] private float investigationDuration = 4f;
    [SerializeField] private float slotRefreshInterval = 0.35f;
    [SerializeField] private float slotInnerRadius = 0.95f;
    [SerializeField] private float slotOuterRadius = 1.35f;
    [SerializeField] private int slotInnerCount = 4;
    [SerializeField] private int slotOuterCount = 6;
    [SerializeField] private float stuckCheckInterval = 0.25f;
    [SerializeField] private float stuckProgressThreshold = 0.03f;
    [SerializeField] private float stuckTimeout = 0.75f;
    [SerializeField] private float wallProbeDistance = 0.18f;
    [SerializeField] private int avoidancePriorityMin = 35;
    [SerializeField] private int avoidancePriorityMax = 65;

    [Header("Animation")]
    [SerializeField] private float animationSpeedReference = 0.2f;
    [Range(0f, 1f)] [SerializeField] private float minimumMoveBlend = 0f;
    [SerializeField] private float animationMoveThreshold = 0.01f;

    [Header("Combat Movement")]
    [SerializeField] private float combatDecisionInterval = 0.6f;
    [SerializeField] private float combatMovePointRefreshInterval = 0.2f;
    [SerializeField] private float combatStrafeDistance = 0.55f;
    [SerializeField] private float combatAdvanceDistance = 0.35f;
    [SerializeField] private float combatRetreatDistance = 0.45f;
    [SerializeField] private float preferredCombatDistance = 0.72f;
    [SerializeField] private float preferredDistanceTolerance = 0.12f;
    [SerializeField] private float combatMovementJitter = 0.08f;
    [SerializeField] private float combatLateralWeight = 0.6f;
    [SerializeField] private float combatAdvanceWeight = 0.2f;
    [SerializeField] private float combatRetreatWeight = 0.2f;
    [SerializeField] private float movementFacingDeadzone = 0.02f;

    private MovementScript movementScript;
    private NavMeshAgent navMeshAgent;
    private WeaponScript weaponScript;
    private PlayerHealthScript healthScript;
    private EnemyAlertIndicator alertIndicator;
    private EnemyAudioController enemyAudio;
    private EnemyTacticsCoordinator tacticsCoordinator;
    private PlayerHealthScript cachedTargetHealth;
    private PlayerController cachedPlayerController;
    private Vector3 lastKnownPlayerPosition;
    private Vector3 lastMeasuredPosition;
    private Vector3 measuredPlanarVelocity;
    private Vector3 tacticalDestination;
    private Vector3 lastMovementDirection = Vector3.forward;
    private float attackWarmupTimer;
    private float loseSightTimer;
    private float patrolRetargetTimer;
    private float combatDecisionTimer;
    private float combatSidePreferenceTimer;
    private float investigationTimer;
    private float damageAggroTimer;
    private float destinationRefreshTimer;
    private float stuckCheckTimer;
    private float timeWithoutProgress;
    private float previousRemainingDistance = float.PositiveInfinity;
    private float baseNavSpeed = 0.1f;
    private float damageSpeedBoostTimer;
    private int preferredCombatSideSign = 1;
    private int baseMaxHealth;
    private int baseDamagePerShot;
    private bool baseScalingStatsCached;
    private bool isEnraged;
    private bool hasPatrolDestination;
    private bool hasLastKnownPlayerPosition;
    private bool hasTacticalDestination;
    private bool forceDestinationRefresh;
    private AIState currentState = AIState.Patrol;
    private CombatMovementMode currentCombatMovementMode = CombatMovementMode.StrafeRight;

    void Awake()
    {
        movementScript = GetComponent<MovementScript>();
        navMeshAgent = GetComponent<NavMeshAgent>();
        weaponScript = GetComponentInChildren<WeaponScript>(true);
        healthScript = GetComponent<PlayerHealthScript>();
        if (healthScript == null) healthScript = GetComponentInChildren<PlayerHealthScript>(true);
        enemyAudio = GetComponent<EnemyAudioController>();
        ResolveCoordinator();
        CacheBaseScalingStats();
        ConfigureNavigation();
        CacheTargetHealth();
        ResolveAlertIndicator();
        ResetAttackWarmup();
        ResetMeasuredMotion();
        ResetCombatMovementState();
        SetState(AIState.Patrol, false);
    }

    void OnEnable()
    {
        ResolveCoordinator();
        ResetMeasuredMotion();
        ResetCombatMovementState();
        tacticsCoordinator?.RegisterEnemy(this, avoidancePriorityMin, avoidancePriorityMax);
    }

    void OnDisable() => tacticsCoordinator?.UnregisterEnemy(this);
    void OnDestroy() => tacticsCoordinator?.UnregisterEnemy(this);

    public void ApplyRoundScaling(float healthMultiplier, float damageMultiplier)
    {
        CacheBaseScalingStats();
        if (healthScript != null)
        {
            int scaledMaxHealth = Mathf.Max(baseMaxHealth, Mathf.RoundToInt(baseMaxHealth * Mathf.Max(1f, healthMultiplier)));
            healthScript.SetMaxHealth(scaledMaxHealth, true);
        }

        if (weaponScript != null)
        {
            int scaledDamage = Mathf.Max(baseDamagePerShot, Mathf.RoundToInt(baseDamagePerShot * Mathf.Max(1f, damageMultiplier)));
            weaponScript.SetDamagePerShot(scaledDamage);
        }
    }

    public void NotifyDamagedByPlayer(Transform aggressor, Vector3 hitPoint)
    {
        if (healthScript != null && !healthScript.IsAlive)
        {
            return;
        }

        Transform playerTarget = aggressor;

        if (playerTarget == null)
        {
            PlayerController playerController = ResolvePlayerController();
            playerTarget = playerController != null ? playerController.transform : null;
        }

        if (playerTarget == null)
        {
            RememberPlayerPosition(hitPoint);
            return;
        }

        damageSpeedBoostTimer = Mathf.Max(damageSpeedBoostTimer, Mathf.Max(0f, damageSpeedBoostDuration));
        EnterEnragedState(playerTarget);
        UpdateAgentSpeedByState();
        ResolveCoordinator();
        tacticsCoordinator?.BroadcastAggro(this, lastKnownPlayerPosition, allyAlertRadius, aggressor);
    }

    public void NotifyAllyAlert(Vector3 playerPosition, Transform aggressor)
    {
        if (aggressor != null)
        {
            target = aggressor;
            CacheTargetHealth();
        }

        RememberPlayerPosition(playerPosition);
        investigationTimer = Mathf.Max(investigationTimer, Mathf.Max(0.1f, investigationDuration));
        ForceDestinationRefresh();
        if (currentState != AIState.Combat) SetState(AIState.Investigate, true);
    }

    public void SetAvoidancePriority(int priority)
    {
        if (navMeshAgent != null) navMeshAgent.avoidancePriority = Mathf.Clamp(priority, 0, 99);
    }

    void Update()
    {
        bool hasTarget = TryResolveTarget();
        Vector3 flatDirection = Vector3.zero;
        float flatDistance = float.PositiveInfinity;
        bool canSeeTarget = false;

        if (hasTarget)
        {
            Vector3 toTarget = target.position - transform.position;
            flatDirection = Vector3.ProjectOnPlane(toTarget, Vector3.up);
            flatDistance = flatDirection.magnitude;
            canSeeTarget = flatDistance <= GetDetectionRange() && HasLineOfSight();
            if (canSeeTarget) RememberPlayerPosition(target.position);
        }

        UpdateMeasuredPlanarVelocity();
        UpdateTimers(canSeeTarget);
        UpdateAwarenessState(hasTarget, canSeeTarget);
        UpdateAgentSpeedByState();

        switch (currentState)
        {
            case AIState.Combat:
                HandleCombat(canSeeTarget, flatDirection, flatDistance);
                return;
            case AIState.Investigate:
                HandleInvestigate();
                return;
            default:
                HandlePatrol();
                return;
        }
    }

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
            investigationTimer = Mathf.Max(investigationTimer, Mathf.Max(0.1f, investigationDuration));
            SetState(AIState.Combat, true);
            return;
        }

        if (currentState == AIState.Combat)
        {
            if (loseSightTimer > 0f) return;
            if (hasLastKnownPlayerPosition || damageAggroTimer > 0f || investigationTimer > 0f)
            {
                investigationTimer = Mathf.Max(investigationTimer, Mathf.Max(0.1f, investigationDuration));
                SetState(AIState.Investigate, false);
                return;
            }

            SetState(AIState.Patrol, false);
            return;
        }

        if ((damageAggroTimer > 0f || investigationTimer > 0f) && (hasLastKnownPlayerPosition || hasTarget))
        {
            if (!hasLastKnownPlayerPosition && hasTarget && target != null) RememberPlayerPosition(target.position);
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
        ResetAttackWarmup();
        UpdatePatrolNavigation();
        UpdateMovementPresentation(GetMovementFacingDirection());
    }

    private void HandleInvestigate()
    {
        ResetAttackWarmup();
        if (!hasLastKnownPlayerPosition)
        {
            SetState(AIState.Patrol, false);
            HandlePatrol();
            return;
        }

        if (ShouldRefreshTacticalDestination() || HasReachedDestination(GetPatrolPointReachThreshold()))
        {
            RefreshInvestigateDestination();
        }

        if (hasTacticalDestination)
        {
            SetAgentDestination(tacticalDestination, GetPatrolPointReachThreshold());
        }

        MonitorNavigationRecovery(false);
        UpdateMovementPresentation(GetMovementFacingDirection());

        if (HasReachedDestination(GetPatrolPointReachThreshold()) && investigationTimer <= 0f && damageAggroTimer <= 0f)
        {
            ClearLastKnownPlayerPosition();
            SetState(AIState.Patrol, false);
        }
    }

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
                if (ShouldRefreshTacticalDestination()) RefreshCombatDestination();

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
            Vector3.Dot(transform.forward, flatDirection.normalized) >= attackAimDotThreshold;

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

        if (weaponScript != null) weaponScript.TryFire(GetTargetAimPoint());
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
            Vector3.Dot(transform.forward, flatDirection.normalized) >= attackAimDotThreshold;

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

        if (weaponScript != null) weaponScript.TryFire(GetTargetAimPoint());
    }

    private void RefreshVisibleCombatDestination(Vector3 flatDirection, float flatDistance)
    {
        if (TryResolveCombatMovePoint(flatDirection, flatDistance, out Vector3 destination))
        {
            SetTacticalDestination(destination, combatMovePointRefreshInterval);
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

        if (TryResolveFallbackDestination(center, Mathf.Max(0.35f, combatAdvanceDistance), out Vector3 destination))
        {
            SetTacticalDestination(destination, combatMovePointRefreshInterval);
            return;
        }

        SetTacticalDestination(center, combatMovePointRefreshInterval);
    }

    private void RefreshCombatDestination()
    {
        Vector3 center = target != null ? target.position : (hasLastKnownPlayerPosition ? lastKnownPlayerPosition : transform.position);
        if (!TryResolveCombatDestination(center, out Vector3 destination)) destination = center;
        SetTacticalDestination(destination);
    }

    private void RefreshInvestigateDestination()
    {
        Vector3 center = hasLastKnownPlayerPosition ? lastKnownPlayerPosition : transform.position;
        if (!TryResolveInvestigateDestination(center, out Vector3 destination)) destination = center;
        SetTacticalDestination(destination);
    }

    private bool TryResolveCombatDestination(Vector3 center, out Vector3 destination)
    {
        ResolveCoordinator();

        if (tacticsCoordinator != null &&
            tacticsCoordinator.RequestCombatSlot(this, center, slotInnerRadius, slotOuterRadius, slotInnerCount, slotOuterCount, GetAreaMask(), out destination))
        {
            return true;
        }

        return TryResolveFallbackDestination(center, slotOuterRadius, out destination);
    }

    private bool TryResolveInvestigateDestination(Vector3 center, out Vector3 destination)
    {
        ResolveCoordinator();

        if (tacticsCoordinator != null &&
            tacticsCoordinator.RequestInvestigatePoint(this, center, slotInnerRadius, slotOuterRadius, slotInnerCount, slotOuterCount, GetAreaMask(), out destination))
        {
            return true;
        }

        return TryResolveFallbackDestination(center, slotOuterRadius, out destination);
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
        SetTacticalDestination(destination, slotRefreshInterval);
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
        if (!hasTacticalDestination || !IsNavigationAvailable())
        {
            ResetNavigationRecoveryState();
            return;
        }

        if (navMeshAgent.pathPending) return;

        if (!navMeshAgent.hasPath || navMeshAgent.pathStatus != NavMeshPathStatus.PathComplete)
        {
            ForceDestinationRefresh();
            return;
        }

        if (stuckCheckTimer > 0f) return;

        float checkInterval = Mathf.Max(0.05f, stuckCheckInterval);
        stuckCheckTimer = checkInterval;
        float remainingDistance = navMeshAgent.remainingDistance;

        if (float.IsInfinity(previousRemainingDistance) ||
            remainingDistance < previousRemainingDistance - Mathf.Max(0.001f, stuckProgressThreshold))
        {
            timeWithoutProgress = 0f;
        }
        else
        {
            timeWithoutProgress += checkInterval;
        }

        previousRemainingDistance = remainingDistance;
        bool wallAhead = !canSeeTarget && ProbeWallAhead();

        if (wallAhead || timeWithoutProgress >= Mathf.Max(0.1f, stuckTimeout))
        {
            ForceDestinationRefresh();
        }
    }

    private bool ProbeWallAhead()
    {
        Vector3 probeDirection = GetDirectionToCurrentDestination();
        if (probeDirection.sqrMagnitude <= 0.0001f)
        {
            probeDirection = lastMovementDirection.sqrMagnitude > 0.0001f ? lastMovementDirection : transform.forward;
        }

        Vector3 flattenedDirection = Vector3.ProjectOnPlane(probeDirection, Vector3.up);
        if (flattenedDirection.sqrMagnitude <= 0.0001f) return false;

        Vector3 rayOrigin = transform.position + (Vector3.up * Mathf.Max(0.05f, GetEyeHeight() * 0.5f));
        int mask = visionBlockLayers.value != 0 ? visionBlockLayers.value : Physics.DefaultRaycastLayers;

        if (!Physics.Raycast(rayOrigin, flattenedDirection.normalized, out RaycastHit hit, Mathf.Max(0.05f, wallProbeDistance), mask, QueryTriggerInteraction.Ignore))
        {
            return false;
        }

        return target == null || (hit.transform != target && !hit.transform.IsChildOf(target));
    }

    private void ResetNavigationRecoveryState()
    {
        timeWithoutProgress = 0f;
        previousRemainingDistance = float.PositiveInfinity;
        stuckCheckTimer = 0f;
    }

    private void UpdateMovementPresentation(Vector3 facingDirection)
    {
        movementScript.SetMoveInput(Vector2.zero);

        if (facingDirection.sqrMagnitude > 0.0001f)
        {
            movementScript.FaceDirection(facingDirection, turnSpeed);
            lastMovementDirection = facingDirection.normalized;
        }

        movementScript.SetExternalMovementAnimation(
            measuredPlanarVelocity,
            IsNavigationAvailable(),
            animationSpeedReference,
            minimumMoveBlend,
            animationMoveThreshold);
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

    private void UpdateCombatMovementDecision(float flatDistance)
    {
        if (combatDecisionTimer > 0f)
        {
            return;
        }

        float preferredDistance = Mathf.Max(0.05f, preferredCombatDistance);
        float tolerance = Mathf.Max(0.01f, preferredDistanceTolerance);

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
                if (preferredCombatSideSign == 0) preferredCombatSideSign = 1;
                combatSidePreferenceTimer = Mathf.Max(0.8f, combatDecisionInterval * 2f);
            }

            currentCombatMovementMode = preferredCombatSideSign < 0
                ? CombatMovementMode.StrafeLeft
                : CombatMovementMode.StrafeRight;
        }

        combatDecisionTimer = Mathf.Max(0.1f, combatDecisionInterval);
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
        float jitter = Mathf.Max(0f, combatMovementJitter);

        switch (currentCombatMovementMode)
        {
            case CombatMovementMode.Advance:
                desiredDistanceFromPlayer = Mathf.Max(0.05f, flatDistance - Mathf.Max(0.05f, combatAdvanceDistance));
                lateralDistance = Mathf.Max(0f, combatMovementJitter * Mathf.Max(0.1f, combatAdvanceWeight));
                break;
            case CombatMovementMode.Retreat:
                desiredDistanceFromPlayer = flatDistance + Mathf.Max(0.05f, combatRetreatDistance);
                lateralDistance = Mathf.Max(0f, combatMovementJitter * Mathf.Max(0.1f, combatRetreatWeight));
                break;
            case CombatMovementMode.StrafeLeft:
            case CombatMovementMode.StrafeRight:
                desiredDistanceFromPlayer = Mathf.Max(0.05f, preferredCombatDistance);
                lateralDistance = Mathf.Max(0.05f, combatStrafeDistance * Mathf.Max(0.5f, combatLateralWeight));
                break;
        }

        ResolveCoordinator();
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

        return TryResolveFallbackDestination(target.position, slotOuterRadius, out destination);
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
        float deadzone = Mathf.Max(0.001f, movementFacingDeadzone);
        if (direction.sqrMagnitude > deadzone * deadzone)
        {
            return direction;
        }

        direction = GetDirectionToCurrentDestination();
        if (direction.sqrMagnitude > 0.0001f) return direction;

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

    private bool TryResolveTarget()
    {
        if (target == null)
        {
            PlayerController playerController = ResolvePlayerController();
            if (playerController != null)
            {
                target = playerController.transform;
                CacheTargetHealth();
            }
        }

        if (target == null) return false;
        if (cachedTargetHealth == null) CacheTargetHealth();
        return cachedTargetHealth == null || cachedTargetHealth.IsAlive;
    }

    private void CacheTargetHealth()
    {
        if (target == null)
        {
            cachedTargetHealth = null;
            return;
        }

        PlayerController playerController = ResolvePlayerController();
        if (playerController != null && target == playerController.transform)
        {
            cachedTargetHealth = playerController.Health;
            return;
        }

        cachedTargetHealth = target.GetComponent<PlayerHealthScript>();
    }

    private PlayerController ResolvePlayerController()
    {
        if (cachedPlayerController != null) return cachedPlayerController;
        cachedPlayerController = PlayerController.Instance;
        if (cachedPlayerController == null) cachedPlayerController = FindFirstObjectByType<PlayerController>();
        return cachedPlayerController;
    }

    private void ResolveCoordinator()
    {
        if (tacticsCoordinator == null) tacticsCoordinator = EnemyTacticsCoordinator.Resolve();
    }

    private void ResolveAlertIndicator()
    {
        alertIndicator = GetComponentInChildren<EnemyAlertIndicator>(true);
        if (alertIndicator == null)
        {
            GameObject indicatorObject = new GameObject("AlertIndicator");
            indicatorObject.transform.SetParent(transform, false);
            alertIndicator = indicatorObject.AddComponent<EnemyAlertIndicator>();
        }

        Transform anchor = alertAnchor != null ? alertAnchor : transform;
        alertIndicator.Configure(anchor, GetAlertHeightOffset());
    }

    private void ConfigureNavigation()
    {
        if (navMeshAgent == null) return;
        baseNavSpeed = Mathf.Max(0.01f, navMeshAgent.speed);
        navMeshAgent.updateRotation = false;
        navMeshAgent.stoppingDistance = GetEffectiveStoppingDistance();
        UpdateAgentSpeedByState();

        Rigidbody rigidbody = GetComponent<Rigidbody>();
        if (rigidbody != null) rigidbody.isKinematic = true;

        EnsureAgentOnNavMesh();
    }

    private bool HasLineOfSight()
    {
        if (target == null) return false;

        Vector3 origin = GetEyeOrigin();
        Vector3 destination = GetTargetAimPoint();
        Vector3 direction = destination - origin;
        float distance = direction.magnitude;
        if (distance <= 0.001f) return true;

        int mask = visionBlockLayers.value != 0 ? visionBlockLayers.value : Physics.DefaultRaycastLayers;
        if (Physics.Raycast(origin, direction.normalized, out RaycastHit hit, distance, mask, QueryTriggerInteraction.Ignore))
        {
            return hit.transform == target || hit.transform.IsChildOf(target);
        }

        return true;
    }

    private bool CanSeeCurrentTarget()
    {
        if (!TryResolveTarget() || target == null) return false;
        Vector3 flatDirection = Vector3.ProjectOnPlane(target.position - transform.position, Vector3.up);
        return flatDirection.magnitude <= GetDetectionRange() && HasLineOfSight();
    }

    private Vector3 GetEyeOrigin()
    {
        return eyeOrigin != null ? eyeOrigin.position : transform.position + Vector3.up * GetEyeHeight();
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

    private void EnterEnragedState(Transform playerTarget)
    {
        if (playerTarget == null)
        {
            return;
        }

        target = playerTarget;
        CacheTargetHealth();
        RememberPlayerPosition(playerTarget.position);
        isEnraged = true;
        damageAggroTimer = 0f;
        investigationTimer = 0f;
        loseSightTimer = 0f;
        ForceDestinationRefresh();
        ResetCombatMovementState();
        SetState(AIState.Combat, true);
    }

    private void ExitEnragedState()
    {
        isEnraged = false;
        damageAggroTimer = 0f;
        investigationTimer = 0f;
        loseSightTimer = 0f;
        target = null;
        cachedTargetHealth = null;
        hasTacticalDestination = false;
        forceDestinationRefresh = false;
        destinationRefreshTimer = 0f;
        ClearLastKnownPlayerPosition();
        ResetCombatMovementState();
        ResetNavigationRecoveryState();
        StopNavigation();
    }

    private bool IsEnragedTargetValid()
    {
        if (!isEnraged)
        {
            return false;
        }

        if (target == null)
        {
            return false;
        }

        CacheTargetHealth();
        return cachedTargetHealth == null || cachedTargetHealth.IsAlive;
    }

    private void ResetCombatMovementState()
    {
        combatDecisionTimer = 0f;
        combatSidePreferenceTimer = 0f;
        currentCombatMovementMode = preferredCombatSideSign < 0
            ? CombatMovementMode.StrafeLeft
            : CombatMovementMode.StrafeRight;
    }

    private float GetDetectionRange() => detectionRange > 0f ? detectionRange : DefaultDetectionRange;
    private float GetLoseSightGraceTime() => loseSightGraceTime > 0f ? loseSightGraceTime : DefaultLoseSightGraceTime;
    private float GetEyeHeight() => eyeHeight > 0f ? eyeHeight : DefaultEyeHeight;
    private float GetAlertHeightOffset() => alertHeightOffset > 0f ? alertHeightOffset : DefaultAlertHeightOffset;
    private float GetStoppingDistance() => stoppingDistance > 0f ? stoppingDistance : DefaultStoppingDistance;
    private float GetAttackRange() => attackRange > 0f ? attackRange : DefaultAttackRange;
    private float GetPatrolPointReachThreshold() => patrolPointReachThreshold > 0f ? patrolPointReachThreshold : DefaultPatrolPointReachThreshold;
    private float GetPatrolRetargetDelay() => patrolRetargetDelay > 0f ? patrolRetargetDelay : DefaultPatrolRetargetDelay;
    private float GetPatrolSearchRadius() => patrolSearchRadius > 0f ? patrolSearchRadius : DefaultPatrolSearchRadius;
    private float GetPatrolMinTravelDistance() => patrolMinTravelDistance > 0f ? patrolMinTravelDistance : DefaultPatrolMinTravelDistance;
    private float GetEffectiveStoppingDistance() => Mathf.Max(0.05f, Mathf.Min(GetStoppingDistance(), GetAttackRange()));

    private void ResetAttackWarmup()
    {
        attackWarmupTimer = attackWarmup;
    }

    private Vector3 GetTargetAimPoint()
    {
        if (target == null) return transform.position + (transform.forward * Mathf.Max(0.1f, GetAttackRange()));
        return target.position + Vector3.up * targetAimHeight;
    }

    private void UpdatePatrolNavigation()
    {
        if (!EnsureAgentOnNavMesh())
        {
            hasPatrolDestination = false;
            return;
        }

        if (hasPatrolDestination && HasReachedDestination(GetPatrolPointReachThreshold()))
        {
            StopNavigation();
            hasPatrolDestination = false;
            patrolRetargetTimer = GetPatrolRetargetDelay();
        }

        if (hasPatrolDestination) return;

        if (patrolRetargetTimer > 0f)
        {
            patrolRetargetTimer -= Time.deltaTime;
            return;
        }

        if (!TrySetNextPatrolDestination()) patrolRetargetTimer = GetPatrolRetargetDelay();
    }

    private bool TrySetNextPatrolDestination()
    {
        if (navMeshAgent == null) return false;

        float radius = GetPatrolSearchRadius();
        float minTravelDistance = GetPatrolMinTravelDistance();
        float minTravelDistanceSqr = minTravelDistance * minTravelDistance;

        for (int attempt = 0; attempt < PatrolPointSearchAttempts; attempt++)
        {
            Vector2 offset2D = Random.insideUnitCircle * radius;
            Vector3 candidate = transform.position + new Vector3(offset2D.x, 0f, offset2D.y);

            if ((candidate - transform.position).sqrMagnitude < minTravelDistanceSqr) continue;
            if (!NavMesh.SamplePosition(candidate, out NavMeshHit hit, radius, navMeshAgent.areaMask)) continue;

            Vector3 travel = Vector3.ProjectOnPlane(hit.position - transform.position, Vector3.up);
            if (travel.sqrMagnitude < minTravelDistanceSqr) continue;
            if (!SetAgentDestination(hit.position, GetPatrolPointReachThreshold())) continue;

            hasPatrolDestination = true;
            return true;
        }

        return false;
    }

    private bool SetAgentDestination(Vector3 destination, float desiredStoppingDistance)
    {
        if (!EnsureAgentOnNavMesh()) return false;
        navMeshAgent.stoppingDistance = Mathf.Max(0.01f, desiredStoppingDistance);
        if (navMeshAgent.isStopped) navMeshAgent.isStopped = false;
        return navMeshAgent.SetDestination(destination);
    }

    private bool HasReachedDestination(float reachThreshold)
    {
        if (!IsNavigationAvailable() || navMeshAgent.pathPending || !navMeshAgent.hasPath) return false;
        return navMeshAgent.remainingDistance <= Mathf.Max(0.01f, reachThreshold);
    }

    private void StopNavigation()
    {
        if (navMeshAgent == null || !navMeshAgent.isOnNavMesh) return;
        navMeshAgent.isStopped = true;
        navMeshAgent.ResetPath();
    }

    private bool EnsureAgentOnNavMesh()
    {
        if (navMeshAgent == null) return false;
        if (navMeshAgent.isOnNavMesh) return true;

        if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, DefaultNavMeshSnapDistance, navMeshAgent.areaMask))
        {
            navMeshAgent.Warp(hit.position);
            return navMeshAgent.isOnNavMesh;
        }

        return false;
    }

    private bool IsNavigationAvailable() => navMeshAgent != null && navMeshAgent.isOnNavMesh;
    private Vector3 GetPlanarAgentVelocity() => IsNavigationAvailable() ? Vector3.ProjectOnPlane(navMeshAgent.velocity, Vector3.up) : Vector3.zero;
    private int GetAreaMask() => navMeshAgent != null ? navMeshAgent.areaMask : NavMesh.AllAreas;

    private void CacheBaseScalingStats()
    {
        if (baseScalingStatsCached) return;
        baseMaxHealth = healthScript != null ? Mathf.Max(1, healthScript.MaxHealth) : 1;
        baseDamagePerShot = weaponScript != null ? Mathf.Max(1, weaponScript.DamagePerShot) : 1;
        baseScalingStatsCached = true;
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
            appliedMultiplier *= Mathf.Max(1f, alertSpeedMultiplier);

            if (damageSpeedBoostTimer > 0f)
            {
                appliedMultiplier *= Mathf.Max(1f, damageSpeedMultiplier);
            }
        }

        navMeshAgent.speed = Mathf.Max(0.01f, baseNavSpeed * appliedMultiplier);
    }
}
