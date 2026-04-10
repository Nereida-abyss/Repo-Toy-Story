using UnityEngine;

public partial class EnemyController
{
    // Intenta resolver objetivo.
    private bool TryResolveTarget()
    {
        if (target == null)
        {
            if (!hasLoggedMissingTarget)
            {
                hasLoggedMissingTarget = true;
                GameDebug.Advertencia("IA", "EnemyController no tiene target asignado. WaveSpawner debe inyectar el objetivo del jugador al generar enemigos.", this);
            }

            cachedTargetHealth = null;
            return false;
        }

        hasLoggedMissingTarget = false;
        if (cachedTargetHealth == null) CacheTargetHealth();
        return cachedTargetHealth == null || cachedTargetHealth.IsAlive;
    }

    // Guarda en cache objetivo vida.
    private void CacheTargetHealth()
    {
        if (target == null)
        {
            cachedTargetHealth = null;
            return;
        }

        cachedTargetHealth = target.GetComponent<PlayerHealthScript>();
    }

    // Resuelve alerta indicator.
    private void ResolveAlertIndicator()
    {
        if (alertIndicator == null)
        {
            alertIndicator = GetComponentInChildren<EnemyAlertIndicator>(true);
        }

        if (alertIndicator == null)
        {
            if (!hasLoggedMissingAlertIndicator)
            {
                hasLoggedMissingAlertIndicator = true;
                GameDebug.Advertencia("IA", "EnemyController no tiene EnemyAlertIndicator configurado en el prefab enemigo.", this);
            }

            return;
        }

        hasLoggedMissingAlertIndicator = false;
        Transform anchor = alertAnchor != null ? alertAnchor : transform;
        alertIndicator.Configure(anchor, GetAlertHeightOffset());
    }

    private void RegisterWithTacticsCoordinator()
    {
        if (!isActiveAndEnabled || tacticsCoordinator == null)
        {
            return;
        }

        tacticsCoordinator.RegisterEnemy(this, GetAvoidancePriorityMin(), GetAvoidancePriorityMax());
    }

    // Comprueba si hay visión limpia entre los ojos del enemigo y el punto de apuntado del objetivo.
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

    // Atajo para saber si el objetivo actual está vivo, cerca y sin obstáculos delante.
    private bool CanSeeCurrentTarget()
    {
        if (!TryResolveTarget() || target == null)
        {
            return false;
        }

        Vector3 flatDirection = Vector3.ProjectOnPlane(target.position - transform.position, Vector3.up);
        return flatDirection.magnitude <= GetDetectionRange() && HasLineOfSight();
    }

    // Define desde dónde "mira" el enemigo al lanzar raycasts de visión.
    private Vector3 GetEyeOrigin()
    {
        return eyeOrigin != null ? eyeOrigin.position : transform.position + Vector3.up * GetEyeHeight();
    }

    // El enemigo no apunta al suelo del objetivo, sino a una altura útil para que el tiro se vea lógico.
    private Vector3 GetTargetAimPoint()
    {
        if (target == null)
        {
            return transform.position + (transform.forward * Mathf.Max(0.1f, GetAttackRange()));
        }

        return target.position + Vector3.up * GetTargetAimHeight();
    }
}
