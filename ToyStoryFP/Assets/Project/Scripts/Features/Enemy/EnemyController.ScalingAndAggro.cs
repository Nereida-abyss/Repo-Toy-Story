using UnityEngine;

public partial class EnemyController
{
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
            playerTarget = target;
        }

        if (playerTarget == null)
        {
            RememberPlayerPosition(hitPoint);
            return;
        }

        damageSpeedBoostTimer = Mathf.Max(damageSpeedBoostTimer, Mathf.Max(0f, GetDamageSpeedBoostDuration()));
        EnterEnragedState(playerTarget);
        UpdateAgentSpeedByState();
        tacticsCoordinator?.BroadcastAggro(this, lastKnownPlayerPosition, GetAllyAlertRadius(), aggressor);
    }

    public void NotifyAllyAlert(Vector3 playerPosition, Transform aggressor)
    {
        if (aggressor != null)
        {
            target = aggressor;
            CacheTargetHealth();
        }

        RememberPlayerPosition(playerPosition);
        investigationTimer = Mathf.Max(investigationTimer, Mathf.Max(0.1f, GetInvestigationDuration()));
        ForceDestinationRefresh();

        if (currentState != AIState.Combat)
        {
            SetState(AIState.Investigate, true);
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
        if (!isEnraged || target == null)
        {
            return false;
        }

        CacheTargetHealth();
        return cachedTargetHealth == null || cachedTargetHealth.IsAlive;
    }
}
