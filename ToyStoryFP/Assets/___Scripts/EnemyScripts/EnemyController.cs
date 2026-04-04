using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(MovementScript))]
public class EnemyController : MonoBehaviour
{
    private const float DefaultDetectionRange = 10f;
    private const float DefaultLoseSightGraceTime = 0.6f;
    private const float DefaultEyeHeight = 1.4f;
    private const float DefaultAlertHeightOffset = 1.8f;

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
    [SerializeField] private float stoppingDistance = 4.5f;
    [SerializeField] private float attackRange = 7f;
    [SerializeField] private float turnSpeed = 360f;

    [Header("Combat")]
    [SerializeField] private float attackWarmup = 0.65f;
    [Range(0f, 1f)] [SerializeField] private float attackAimDotThreshold = 0.92f;

    private MovementScript movementScript;
    private WeaponScript weaponScript;
    private EnemyAlertIndicator alertIndicator;
    private EnemyAudioController enemyAudio;
    private PlayerHealthScript cachedTargetHealth;
    private float attackWarmupTimer;
    private float loseSightTimer;
    private bool isAlerted;

    void Awake()
    {
        movementScript = GetComponent<MovementScript>();
        weaponScript = GetComponentInChildren<WeaponScript>(true);
        enemyAudio = GetComponent<EnemyAudioController>();
        CacheTargetHealth();
        ResolveAlertIndicator();
        ResetAttackWarmup();
        SetAlerted(false, false);
    }

    void Update()
    {
        if (!TryResolveTarget())
        {
            movementScript.SetMoveInput(Vector2.zero);
            SetAlerted(false, false);
            ResetAttackWarmup();
            return;
        }

        Vector3 toTarget = target.position - transform.position;
        Vector3 flatDirection = Vector3.ProjectOnPlane(toTarget, Vector3.up);
        float flatDistance = flatDirection.magnitude;

        UpdateDetectionState(flatDistance);

        if (!isAlerted)
        {
            movementScript.SetMoveInput(Vector2.zero);
            ResetAttackWarmup();
            return;
        }

        movementScript.FaceDirection(flatDirection, turnSpeed);

        Vector2 moveInput = flatDistance > stoppingDistance
            ? Vector2.up
            : Vector2.zero;

        movementScript.SetMoveInput(moveInput);

        bool isInAttackRange = flatDistance <= attackRange;
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

    private void UpdateDetectionState(float flatDistance)
    {
        bool canSeeTargetNow = flatDistance <= GetDetectionRange() && HasLineOfSight();

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

    private bool TryResolveTarget()
    {
        if (target == null)
        {
            if (PlayerController.Instance != null)
            {
                target = PlayerController.Instance.transform;
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

        if (PlayerController.Instance != null && target == PlayerController.Instance.transform)
        {
            cachedTargetHealth = PlayerController.Instance.Health;
            return;
        }

        cachedTargetHealth = target.GetComponent<PlayerHealthScript>();
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

        if (!alerted)
        {
            loseSightTimer = 0f;
            ResetAttackWarmup();
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

    private void ResetAttackWarmup()
    {
        attackWarmupTimer = attackWarmup;
    }

    private Vector3 GetTargetAimPoint()
    {
        return target.position + Vector3.up * targetAimHeight;
    }
}
