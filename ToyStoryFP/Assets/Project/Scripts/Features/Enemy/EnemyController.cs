using UnityEngine;
using UnityEngine.AI;

[DisallowMultipleComponent]
[RequireComponent(typeof(MovementScript))]
[RequireComponent(typeof(NavMeshAgent))]
public partial class EnemyController : MonoBehaviour
{
    private enum AIState { Patrol, Investigate, Combat }
    private enum CombatMovementMode { Advance, StrafeLeft, StrafeRight, Retreat }

    private const float DefaultDetectionRange = 1f;
    private const float DefaultLoseSightGraceTime = 0.6f;
    private const float DefaultEyeHeight = 1.4f;
    private const float DefaultAlertHeightOffset = 0.45f;
    private const float DefaultStoppingDistance = 0.75f;
    private const float DefaultAttackRange = 0.8f;
    private const float DefaultNavMeshSnapDistance = 1f;
    private const float DefaultPatrolPointReachThreshold = 0.25f;
    private const float DefaultPatrolRetargetDelay = 0.2f;
    private const float DefaultPatrolSearchRadius = 5f;
    private const float DefaultPatrolMinTravelDistance = 2.5f;
    private const int PatrolPointSearchAttempts = 8;

    [Header("Target")]
    [SerializeField] private Transform target;
    [SerializeField] private EnemyBehaviorProfile behaviorProfile;
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
    [SerializeField] private EnemyAlertIndicator alertIndicator;
    private EnemyAudioController enemyAudio;
    [SerializeField] private EnemyTacticsCoordinator tacticsCoordinator;
    private PlayerHealthScript cachedTargetHealth;
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
    private bool hasLoggedMissingTarget;
    private bool hasLoggedMissingAlertIndicator;
    private bool hasLoggedMissingBehaviorProfile;
    private AIState currentState = AIState.Patrol;
    private CombatMovementMode currentCombatMovementMode = CombatMovementMode.StrafeRight;

    void Awake()
    {
        movementScript = GetComponent<MovementScript>();
        navMeshAgent = GetComponent<NavMeshAgent>();
        weaponScript = GetComponentInChildren<WeaponScript>(true);
        healthScript = GetComponent<PlayerHealthScript>();

        if (healthScript == null)
        {
            healthScript = GetComponentInChildren<PlayerHealthScript>(true);
        }

        enemyAudio = GetComponent<EnemyAudioController>();

        if (weaponScript != null)
        {
            weaponScript.SetPlayerOwned(false);
            weaponScript.ConfigureAudioControllers(null, enemyAudio);
        }

        CacheBaseScalingStats();
        WarnIfMissingBehaviorProfile();
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
        ResetMeasuredMotion();
        ResetCombatMovementState();
        RegisterWithTacticsCoordinator();
    }

    void OnDisable() => tacticsCoordinator?.UnregisterEnemy(this);
    void OnDestroy() => tacticsCoordinator?.UnregisterEnemy(this);

    public void ConfigureRuntimeContext(Transform runtimeTarget, EnemyTacticsCoordinator coordinator)
    {
        if (runtimeTarget != null)
        {
            target = runtimeTarget;
            hasLoggedMissingTarget = false;
            CacheTargetHealth();
        }

        if (tacticsCoordinator != coordinator)
        {
            tacticsCoordinator?.UnregisterEnemy(this);
            tacticsCoordinator = coordinator;
        }

        RegisterWithTacticsCoordinator();
    }

    public void SetAvoidancePriority(int priority)
    {
        if (navMeshAgent != null)
        {
            navMeshAgent.avoidancePriority = Mathf.Clamp(priority, 0, 99);
        }
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

            if (canSeeTarget)
            {
                RememberPlayerPosition(target.position);
            }
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
}
