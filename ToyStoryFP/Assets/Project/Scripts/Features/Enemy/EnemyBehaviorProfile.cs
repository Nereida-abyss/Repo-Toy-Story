using UnityEngine;

[CreateAssetMenu(fileName = "DefaultEnemyBehaviorProfile", menuName = "Enemy/Enemy Behavior Profile")]
public class EnemyBehaviorProfile : ScriptableObject
{
    [Header("Target")]
    [SerializeField] private float targetAimHeight = 0.15f;
    [SerializeField] private float detectionRange = 1f;
    [SerializeField] private float loseSightGraceTime = 0.6f;
    [SerializeField] private float eyeHeight = 1.4f;
    [SerializeField] private float alertHeightOffset = 0.45f;

    [Header("Movement")]
    [SerializeField] private float stoppingDistance = 0.75f;
    [SerializeField] private float attackRange = 0.8f;
    [SerializeField] private float turnSpeed = 360f;
    [SerializeField] private float alertSpeedMultiplier = 2f;
    [SerializeField] private float damageSpeedMultiplier = 1.7f;
    [SerializeField] private float damageSpeedBoostDuration = 3f;

    [Header("Patrol")]
    [SerializeField] private float patrolPointReachThreshold = 0.25f;
    [SerializeField] private float patrolRetargetDelay = 0.2f;
    [SerializeField] private float patrolSearchRadius = 5f;
    [SerializeField] private float patrolMinTravelDistance = 2.5f;

    [Header("Combat")]
    [SerializeField] private float attackWarmup = 0.65f;
    [SerializeField, Range(0f, 1f)] private float attackAimDotThreshold = 0.877f;

    [Header("Tactics")]
    [SerializeField] private float allyAlertRadius = 0.6f;
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
    [SerializeField, Range(0f, 1f)] private float minimumMoveBlend = 0f;
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

    public float TargetAimHeight => targetAimHeight;
    public float DetectionRange => detectionRange;
    public float LoseSightGraceTime => loseSightGraceTime;
    public float EyeHeight => eyeHeight;
    public float AlertHeightOffset => alertHeightOffset;
    public float StoppingDistance => stoppingDistance;
    public float AttackRange => attackRange;
    public float TurnSpeed => turnSpeed;
    public float AlertSpeedMultiplier => alertSpeedMultiplier;
    public float DamageSpeedMultiplier => damageSpeedMultiplier;
    public float DamageSpeedBoostDuration => damageSpeedBoostDuration;
    public float PatrolPointReachThreshold => patrolPointReachThreshold;
    public float PatrolRetargetDelay => patrolRetargetDelay;
    public float PatrolSearchRadius => patrolSearchRadius;
    public float PatrolMinTravelDistance => patrolMinTravelDistance;
    public float AttackWarmup => attackWarmup;
    public float AttackAimDotThreshold => attackAimDotThreshold;
    public float AllyAlertRadius => allyAlertRadius;
    public float InvestigationDuration => investigationDuration;
    public float SlotRefreshInterval => slotRefreshInterval;
    public float SlotInnerRadius => slotInnerRadius;
    public float SlotOuterRadius => slotOuterRadius;
    public int SlotInnerCount => slotInnerCount;
    public int SlotOuterCount => slotOuterCount;
    public float StuckCheckInterval => stuckCheckInterval;
    public float StuckProgressThreshold => stuckProgressThreshold;
    public float StuckTimeout => stuckTimeout;
    public float WallProbeDistance => wallProbeDistance;
    public int AvoidancePriorityMin => avoidancePriorityMin;
    public int AvoidancePriorityMax => avoidancePriorityMax;
    public float AnimationSpeedReference => animationSpeedReference;
    public float MinimumMoveBlend => minimumMoveBlend;
    public float AnimationMoveThreshold => animationMoveThreshold;
    public float CombatDecisionInterval => combatDecisionInterval;
    public float CombatMovePointRefreshInterval => combatMovePointRefreshInterval;
    public float CombatStrafeDistance => combatStrafeDistance;
    public float CombatAdvanceDistance => combatAdvanceDistance;
    public float CombatRetreatDistance => combatRetreatDistance;
    public float PreferredCombatDistance => preferredCombatDistance;
    public float PreferredDistanceTolerance => preferredDistanceTolerance;
    public float CombatMovementJitter => combatMovementJitter;
    public float CombatLateralWeight => combatLateralWeight;
    public float CombatAdvanceWeight => combatAdvanceWeight;
    public float CombatRetreatWeight => combatRetreatWeight;
    public float MovementFacingDeadzone => movementFacingDeadzone;
}
