using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(MovementScript))]
public class EnemyController : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform target;
    [SerializeField] private float targetAimHeight = 0.15f;

    [Header("Movement")]
    [SerializeField] private float stoppingDistance = 4.5f;
    [SerializeField] private float attackRange = 7f;
    [SerializeField] private float turnSpeed = 360f;

    [Header("Combat")]
    [SerializeField] private float attackWarmup = 0.65f;
    [Range(0f, 1f)] [SerializeField] private float attackAimDotThreshold = 0.92f;

    private MovementScript movementScript;
    private WeaponScript weaponScript;
    private PlayerHealthScript cachedTargetHealth;
    private float attackWarmupTimer;

    void Awake()
    {
        movementScript = GetComponent<MovementScript>();
        weaponScript = GetComponentInChildren<WeaponScript>(true);
        CacheTargetHealth();
        ResetAttackWarmup();
    }

    void Update()
    {
        if (!TryResolveTarget())
        {
            movementScript.SetMoveInput(Vector2.zero);
            ResetAttackWarmup();
            return;
        }

        Vector3 toTarget = target.position - transform.position;
        Vector3 flatDirection = Vector3.ProjectOnPlane(toTarget, Vector3.up);
        float flatDistance = flatDirection.magnitude;

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

    private bool TryResolveTarget()
    {
        if (target == null)
        {
            GameObject playerObject = GameObject.Find("PlayerController");

            if (playerObject != null)
            {
                target = playerObject.transform;
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
        cachedTargetHealth = target != null ? target.GetComponent<PlayerHealthScript>() : null;
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
