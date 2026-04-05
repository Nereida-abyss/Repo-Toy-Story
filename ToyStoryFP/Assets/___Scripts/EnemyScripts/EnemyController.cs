using UnityEngine;
using UnityEngine.AI;

[DisallowMultipleComponent]
[RequireComponent(typeof(MovementScript))]
[RequireComponent(typeof(NavMeshAgent))]
public class EnemyController : MonoBehaviour
{
    private enum AIState
    {
        Patrol,
        Alert
    }

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

    [Header("Patrol")]
    [SerializeField] private float patrolPointReachThreshold = DefaultPatrolPointReachThreshold;
    [SerializeField] private float patrolRetargetDelay = DefaultPatrolRetargetDelay;
    [SerializeField] private float patrolSearchRadius = DefaultPatrolSearchRadius;
    [SerializeField] private float patrolMinTravelDistance = DefaultPatrolMinTravelDistance;

    [Header("Combat")]
    [SerializeField] private float attackWarmup = 0.65f;
    [Range(0f, 1f)] [SerializeField] private float attackAimDotThreshold = 0.92f;

    private MovementScript movementScript;
    private NavMeshAgent navMeshAgent;
    private WeaponScript weaponScript;
    private PlayerHealthScript healthScript;
    private EnemyAlertIndicator alertIndicator;
    private EnemyAudioController enemyAudio;
    private PlayerHealthScript cachedTargetHealth;
    private PlayerController cachedPlayerController;
    private float attackWarmupTimer;
    private float loseSightTimer;
    private float patrolRetargetTimer;
    private int baseMaxHealth;
    private int baseDamagePerShot;
    private bool baseScalingStatsCached;
    private bool isAlerted;
    private bool hasPatrolDestination;
    private AIState currentState = AIState.Patrol;

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
        CacheBaseScalingStats();
        ConfigureNavigation();
        CacheTargetHealth();
        ResolveAlertIndicator();
        ResetAttackWarmup();
        SetAlerted(false, false);
    }

    public void ApplyRoundScaling(float healthMultiplier, float damageMultiplier)
    {
        CacheBaseScalingStats();

        if (healthScript != null)
        {
            int scaledMaxHealth = Mathf.Max(
                baseMaxHealth,
                Mathf.RoundToInt(baseMaxHealth * Mathf.Max(1f, healthMultiplier)));
            healthScript.SetMaxHealth(scaledMaxHealth, true);
        }

        if (weaponScript != null)
        {
            int scaledDamage = Mathf.Max(
                baseDamagePerShot,
                Mathf.RoundToInt(baseDamagePerShot * Mathf.Max(1f, damageMultiplier)));
            weaponScript.SetDamagePerShot(scaledDamage);
        }
    }

    private void CacheBaseScalingStats()
    {
        if (baseScalingStatsCached)
        {
            return;
        }

        baseMaxHealth = healthScript != null ? Mathf.Max(1, healthScript.MaxHealth) : 1;
        baseDamagePerShot = weaponScript != null ? Mathf.Max(1, weaponScript.DamagePerShot) : 1;
        baseScalingStatsCached = true;
    }

    void Update()
    {
        bool hasTarget = TryResolveTarget();
        Vector3 flatDirection = Vector3.zero;
        float flatDistance = float.PositiveInfinity;

        if (hasTarget)
        {
            Vector3 toTarget = target.position - transform.position;
            flatDirection = Vector3.ProjectOnPlane(toTarget, Vector3.up);
            flatDistance = flatDirection.magnitude;
        }

        UpdateDetectionState(hasTarget, flatDistance);

        if (currentState == AIState.Alert && hasTarget)
        {
            HandleAlert(flatDirection, flatDistance);
            return;
        }

        HandlePatrol();
    }

    private void UpdateDetectionState(bool hasTarget, float flatDistance)
    {
        bool canSeeTargetNow = hasTarget &&
            flatDistance <= GetDetectionRange() &&
            HasLineOfSight();

        if (canSeeTargetNow)
        {
            loseSightTimer = GetLoseSightGraceTime();

            if (!isAlerted)
            {
                SetAlerted(true, true);
            }

            return;
        }

        if (!isAlerted)
        {
            return;
        }

        if (loseSightTimer > 0f)
        {
            loseSightTimer -= Time.deltaTime;
            return;
        }

        SetAlerted(false, false);
    }

    private void HandlePatrol()
    {
        ResetAttackWarmup();
        UpdatePatrolNavigation();
        movementScript.SetMoveInput(Vector2.zero);
        movementScript.SetExternalMovementState(GetPlanarAgentVelocity(), IsNavigationAvailable());
        movementScript.FaceDirection(GetFacingDirection(transform.forward), turnSpeed);
    }

    private void HandleAlert(Vector3 flatDirection, float flatDistance)
    {
        hasPatrolDestination = false;
        patrolRetargetTimer = 0f;

        float desiredStoppingDistance = GetEffectiveStoppingDistance();
        bool shouldChase = flatDistance > desiredStoppingDistance;
        bool isInAttackRange = flatDistance <= GetAttackRange();

        if (shouldChase)
        {
            SetAgentDestination(target.position, desiredStoppingDistance);
        }
        else
        {
            StopNavigation();
        }

        movementScript.SetMoveInput(Vector2.zero);
        movementScript.SetExternalMovementState(GetPlanarAgentVelocity(), IsNavigationAvailable());

        Vector3 facingDirection = shouldChase
            ? GetFacingDirection(flatDirection)
            : flatDirection;

        movementScript.FaceDirection(facingDirection, turnSpeed);

        bool hasValidAim = flatDirection.sqrMagnitude > 0.0001f &&
            Vector3.Dot(transform.forward, flatDirection.normalized) >= attackAimDotThreshold;

        if (!isInAttackRange || !hasValidAim)
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

        if (target == null)
        {
            return false;
        }

        if (cachedTargetHealth == null)
        {
            CacheTargetHealth();
        }

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
        if (navMeshAgent == null)
        {
            return;
        }

        navMeshAgent.updateRotation = false;
        navMeshAgent.stoppingDistance = GetEffectiveStoppingDistance();

        Rigidbody rigidbody = GetComponent<Rigidbody>();

        if (rigidbody != null)
        {
            rigidbody.isKinematic = true;
        }

        EnsureAgentOnNavMesh();
    }

    private bool HasLineOfSight()
    {
        if (target == null)
        {
            return false;
        }

        Vector3 origin = GetEyeOrigin();
        Vector3 destination = GetTargetAimPoint();
        Vector3 direction = destination - origin;
        float distance = direction.magnitude;

        if (distance <= 0.001f)
        {
            return true;
        }

        int mask = visionBlockLayers.value != 0 ? visionBlockLayers.value : Physics.DefaultRaycastLayers;

        if (Physics.Raycast(origin, direction.normalized, out RaycastHit hit, distance, mask, QueryTriggerInteraction.Ignore))
        {
            return hit.transform == target || hit.transform.IsChildOf(target);
        }

        return true;
    }

    private Vector3 GetEyeOrigin()
    {
        if (eyeOrigin != null)
        {
            return eyeOrigin.position;
        }

        return transform.position + Vector3.up * GetEyeHeight();
    }

    private void SetAlerted(bool alerted, bool playPulse)
    {
        bool wasAlerted = isAlerted;
        isAlerted = alerted;
        currentState = alerted ? AIState.Alert : AIState.Patrol;

        if (!alerted)
        {
            StopNavigation();
            loseSightTimer = 0f;
            ResetAttackWarmup();
            hasPatrolDestination = false;
            patrolRetargetTimer = 0f;
        }
        else if (!wasAlerted)
        {
            enemyAudio?.PlayAlert();
        }

        if (alertIndicator != null)
        {
            alertIndicator.SetVisible(alerted, playPulse && alerted);
        }
    }

    private float GetDetectionRange()
    {
        return detectionRange > 0f ? detectionRange : DefaultDetectionRange;
    }

    private float GetLoseSightGraceTime()
    {
        return loseSightGraceTime > 0f ? loseSightGraceTime : DefaultLoseSightGraceTime;
    }

    private float GetEyeHeight()
    {
        return eyeHeight > 0f ? eyeHeight : DefaultEyeHeight;
    }

    private float GetAlertHeightOffset()
    {
        return alertHeightOffset > 0f ? alertHeightOffset : DefaultAlertHeightOffset;
    }

    private float GetStoppingDistance()
    {
        return stoppingDistance > 0f ? stoppingDistance : DefaultStoppingDistance;
    }

    private float GetAttackRange()
    {
        return attackRange > 0f ? attackRange : DefaultAttackRange;
    }

    private float GetPatrolPointReachThreshold()
    {
        return patrolPointReachThreshold > 0f ? patrolPointReachThreshold : DefaultPatrolPointReachThreshold;
    }

    private float GetPatrolRetargetDelay()
    {
        return patrolRetargetDelay > 0f ? patrolRetargetDelay : DefaultPatrolRetargetDelay;
    }

    private float GetPatrolSearchRadius()
    {
        return patrolSearchRadius > 0f ? patrolSearchRadius : DefaultPatrolSearchRadius;
    }

    private float GetPatrolMinTravelDistance()
    {
        return patrolMinTravelDistance > 0f ? patrolMinTravelDistance : DefaultPatrolMinTravelDistance;
    }

    private float GetEffectiveStoppingDistance()
    {
        return Mathf.Max(0.05f, Mathf.Min(GetStoppingDistance(), GetAttackRange()));
    }

    private void ResetAttackWarmup()
    {
        attackWarmupTimer = attackWarmup;
    }

    private Vector3 GetTargetAimPoint()
    {
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

        if (hasPatrolDestination)
        {
            return;
        }

        if (patrolRetargetTimer > 0f)
        {
            patrolRetargetTimer -= Time.deltaTime;
            return;
        }

        if (!TrySetNextPatrolDestination())
        {
            patrolRetargetTimer = GetPatrolRetargetDelay();
        }
    }

    private bool TrySetNextPatrolDestination()
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

            if (!SetAgentDestination(hit.position, GetPatrolPointReachThreshold()))
            {
                continue;
            }

            hasPatrolDestination = true;
            return true;
        }

        return false;
    }

    private bool SetAgentDestination(Vector3 destination, float desiredStoppingDistance)
    {
        if (!EnsureAgentOnNavMesh())
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

    private bool HasReachedDestination(float reachThreshold)
    {
        if (!IsNavigationAvailable() || navMeshAgent.pathPending || !navMeshAgent.hasPath)
        {
            return false;
        }

        return navMeshAgent.remainingDistance <= Mathf.Max(0.01f, reachThreshold);
    }

    private void StopNavigation()
    {
        if (navMeshAgent == null || !navMeshAgent.isOnNavMesh)
        {
            return;
        }

        navMeshAgent.isStopped = true;
        navMeshAgent.ResetPath();
    }

    private bool EnsureAgentOnNavMesh()
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

    private bool IsNavigationAvailable()
    {
        return navMeshAgent != null && navMeshAgent.isOnNavMesh;
    }

    private Vector3 GetPlanarAgentVelocity()
    {
        if (!IsNavigationAvailable())
        {
            return Vector3.zero;
        }

        return Vector3.ProjectOnPlane(navMeshAgent.velocity, Vector3.up);
    }

    private Vector3 GetFacingDirection(Vector3 fallbackDirection)
    {
        Vector3 desiredVelocity = navMeshAgent != null
            ? Vector3.ProjectOnPlane(navMeshAgent.desiredVelocity, Vector3.up)
            : Vector3.zero;

        if (desiredVelocity.sqrMagnitude > 0.0001f)
        {
            return desiredVelocity;
        }

        Vector3 velocity = GetPlanarAgentVelocity();

        if (velocity.sqrMagnitude > 0.0001f)
        {
            return velocity;
        }

        return fallbackDirection;
    }
}
