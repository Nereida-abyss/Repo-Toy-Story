using UnityEngine;

public partial class EnemyController
{
    private void WarnIfMissingBehaviorProfile()
    {
        if (behaviorProfile != null)
        {
            hasLoggedMissingBehaviorProfile = false;
            return;
        }

        if (hasLoggedMissingBehaviorProfile)
        {
            return;
        }

        hasLoggedMissingBehaviorProfile = true;
        GameDebug.Advertencia("IA", "EnemyController no tiene EnemyBehaviorProfile asignado. Se usaran los valores locales del prefab.", this);
    }

    private float GetTargetAimHeight() => behaviorProfile != null ? behaviorProfile.TargetAimHeight : targetAimHeight;
    private float GetDetectionRange() => GetPositiveOrDefault(behaviorProfile != null ? behaviorProfile.DetectionRange : detectionRange, DefaultDetectionRange);
    private float GetLoseSightGraceTime() => GetPositiveOrDefault(behaviorProfile != null ? behaviorProfile.LoseSightGraceTime : loseSightGraceTime, DefaultLoseSightGraceTime);
    private float GetEyeHeight() => GetPositiveOrDefault(behaviorProfile != null ? behaviorProfile.EyeHeight : eyeHeight, DefaultEyeHeight);
    private float GetAlertHeightOffset() => GetPositiveOrDefault(behaviorProfile != null ? behaviorProfile.AlertHeightOffset : alertHeightOffset, DefaultAlertHeightOffset);
    private float GetStoppingDistance() => GetPositiveOrDefault(behaviorProfile != null ? behaviorProfile.StoppingDistance : stoppingDistance, DefaultStoppingDistance);
    private float GetAttackRange() => GetPositiveOrDefault(behaviorProfile != null ? behaviorProfile.AttackRange : attackRange, DefaultAttackRange);
    private float GetTurnSpeed() => GetPositiveOrDefault(behaviorProfile != null ? behaviorProfile.TurnSpeed : turnSpeed, 360f);
    private float GetAlertSpeedMultiplier() => Mathf.Max(1f, behaviorProfile != null ? behaviorProfile.AlertSpeedMultiplier : alertSpeedMultiplier);
    private float GetDamageSpeedMultiplier() => Mathf.Max(1f, behaviorProfile != null ? behaviorProfile.DamageSpeedMultiplier : damageSpeedMultiplier);
    private float GetDamageSpeedBoostDuration() => Mathf.Max(0f, behaviorProfile != null ? behaviorProfile.DamageSpeedBoostDuration : damageSpeedBoostDuration);
    private float GetPatrolPointReachThreshold() => GetPositiveOrDefault(behaviorProfile != null ? behaviorProfile.PatrolPointReachThreshold : patrolPointReachThreshold, DefaultPatrolPointReachThreshold);
    private float GetPatrolRetargetDelay() => GetPositiveOrDefault(behaviorProfile != null ? behaviorProfile.PatrolRetargetDelay : patrolRetargetDelay, DefaultPatrolRetargetDelay);
    private float GetPatrolSearchRadius() => GetPositiveOrDefault(behaviorProfile != null ? behaviorProfile.PatrolSearchRadius : patrolSearchRadius, DefaultPatrolSearchRadius);
    private float GetPatrolMinTravelDistance() => GetPositiveOrDefault(behaviorProfile != null ? behaviorProfile.PatrolMinTravelDistance : patrolMinTravelDistance, DefaultPatrolMinTravelDistance);
    private float GetAttackWarmup() => Mathf.Max(0f, behaviorProfile != null ? behaviorProfile.AttackWarmup : attackWarmup);
    private float GetAttackAimDotThreshold() => Mathf.Clamp01(behaviorProfile != null ? behaviorProfile.AttackAimDotThreshold : attackAimDotThreshold);
    private float GetAllyAlertRadius() => Mathf.Max(0f, behaviorProfile != null ? behaviorProfile.AllyAlertRadius : allyAlertRadius);
    private float GetInvestigationDuration() => Mathf.Max(0f, behaviorProfile != null ? behaviorProfile.InvestigationDuration : investigationDuration);
    private float GetSlotRefreshInterval() => Mathf.Max(0.05f, behaviorProfile != null ? behaviorProfile.SlotRefreshInterval : slotRefreshInterval);
    private float GetSlotInnerRadius() => Mathf.Max(0.05f, behaviorProfile != null ? behaviorProfile.SlotInnerRadius : slotInnerRadius);
    private float GetSlotOuterRadius() => Mathf.Max(GetSlotInnerRadius(), behaviorProfile != null ? behaviorProfile.SlotOuterRadius : slotOuterRadius);
    private int GetSlotInnerCount() => Mathf.Max(1, behaviorProfile != null ? behaviorProfile.SlotInnerCount : slotInnerCount);
    private int GetSlotOuterCount() => Mathf.Max(1, behaviorProfile != null ? behaviorProfile.SlotOuterCount : slotOuterCount);
    private float GetStuckCheckInterval() => Mathf.Max(0.05f, behaviorProfile != null ? behaviorProfile.StuckCheckInterval : stuckCheckInterval);
    private float GetStuckProgressThreshold() => Mathf.Max(0.001f, behaviorProfile != null ? behaviorProfile.StuckProgressThreshold : stuckProgressThreshold);
    private float GetStuckTimeout() => Mathf.Max(0.1f, behaviorProfile != null ? behaviorProfile.StuckTimeout : stuckTimeout);
    private float GetWallProbeDistance() => Mathf.Max(0.05f, behaviorProfile != null ? behaviorProfile.WallProbeDistance : wallProbeDistance);
    private int GetAvoidancePriorityMin() => Mathf.Clamp(behaviorProfile != null ? behaviorProfile.AvoidancePriorityMin : avoidancePriorityMin, 0, 99);
    private int GetAvoidancePriorityMax() => Mathf.Clamp(behaviorProfile != null ? behaviorProfile.AvoidancePriorityMax : avoidancePriorityMax, 0, 99);
    private float GetAnimationSpeedReference() => Mathf.Max(0.01f, behaviorProfile != null ? behaviorProfile.AnimationSpeedReference : animationSpeedReference);
    private float GetMinimumMoveBlend() => Mathf.Clamp01(behaviorProfile != null ? behaviorProfile.MinimumMoveBlend : minimumMoveBlend);
    private float GetAnimationMoveThreshold() => Mathf.Max(0f, behaviorProfile != null ? behaviorProfile.AnimationMoveThreshold : animationMoveThreshold);
    private float GetCombatDecisionInterval() => Mathf.Max(0.1f, behaviorProfile != null ? behaviorProfile.CombatDecisionInterval : combatDecisionInterval);
    private float GetCombatMovePointRefreshInterval() => Mathf.Max(0.05f, behaviorProfile != null ? behaviorProfile.CombatMovePointRefreshInterval : combatMovePointRefreshInterval);
    private float GetCombatStrafeDistance() => Mathf.Max(0.05f, behaviorProfile != null ? behaviorProfile.CombatStrafeDistance : combatStrafeDistance);
    private float GetCombatAdvanceDistance() => Mathf.Max(0.05f, behaviorProfile != null ? behaviorProfile.CombatAdvanceDistance : combatAdvanceDistance);
    private float GetCombatRetreatDistance() => Mathf.Max(0.05f, behaviorProfile != null ? behaviorProfile.CombatRetreatDistance : combatRetreatDistance);
    private float GetPreferredCombatDistance() => Mathf.Max(0.05f, behaviorProfile != null ? behaviorProfile.PreferredCombatDistance : preferredCombatDistance);
    private float GetPreferredDistanceTolerance() => Mathf.Max(0.01f, behaviorProfile != null ? behaviorProfile.PreferredDistanceTolerance : preferredDistanceTolerance);
    private float GetCombatMovementJitter() => Mathf.Max(0f, behaviorProfile != null ? behaviorProfile.CombatMovementJitter : combatMovementJitter);
    private float GetCombatLateralWeight() => Mathf.Max(0f, behaviorProfile != null ? behaviorProfile.CombatLateralWeight : combatLateralWeight);
    private float GetCombatAdvanceWeight() => Mathf.Max(0f, behaviorProfile != null ? behaviorProfile.CombatAdvanceWeight : combatAdvanceWeight);
    private float GetCombatRetreatWeight() => Mathf.Max(0f, behaviorProfile != null ? behaviorProfile.CombatRetreatWeight : combatRetreatWeight);
    private float GetMovementFacingDeadzone() => Mathf.Max(0.001f, behaviorProfile != null ? behaviorProfile.MovementFacingDeadzone : movementFacingDeadzone);
    private float GetEffectiveStoppingDistance() => Mathf.Max(0.05f, Mathf.Min(GetStoppingDistance(), GetAttackRange()));

    private static float GetPositiveOrDefault(float value, float fallback)
    {
        return value > 0f ? value : fallback;
    }
}
