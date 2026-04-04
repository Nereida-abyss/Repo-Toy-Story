using UnityEngine;

public class WeaponScript : MonoBehaviour
{
    [Header("Weapon Setup")]
    public Camera _camera;
    [SerializeField] private Transform fireOrigin;
    [SerializeField] private float maxRange = 100f;

    [Header("Weapon Stats")]
    public float fireRate = 10f;
    public int damagePerShot = 20;


    [Header("Weapon Effects")]
    public GameObject muzzleFlashPrefab;
    public AudioClip fireSound;

    private float nextAllowedShotTime;

    public bool TryFire()
    {
        if (!CanFire())
        {
            return false;
        }

        Transform shotTransform = ResolveShotTransform();
        return FireRay(shotTransform.position, shotTransform.forward);
    }

    public bool TryFire(Vector3 targetPoint)
    {
        if (!CanFire())
        {
            return false;
        }

        Transform shotTransform = ResolveShotTransform();
        Vector3 direction = (targetPoint - shotTransform.position).normalized;

        if (direction.sqrMagnitude <= 0.0001f)
        {
            direction = shotTransform.forward;
        }

        return FireRay(shotTransform.position, direction);
    }

    private bool CanFire()
    {
        return Time.time >= nextAllowedShotTime;
    }

    private Transform ResolveShotTransform()
    {
        if (_camera != null)
        {
            return _camera.transform;
        }

        return fireOrigin != null ? fireOrigin : transform;
    }

    private bool FireRay(Vector3 origin, Vector3 direction)
    {
        nextAllowedShotTime = Time.time + (1f / fireRate);

        Vector3 normalizedDirection = direction.normalized;
        Vector3 rayOrigin = origin + normalizedDirection * 0.05f;

        if (Physics.Raycast(rayOrigin, normalizedDirection, out RaycastHit hit, maxRange))
        {
            if (muzzleFlashPrefab != null)
            {
                Instantiate(muzzleFlashPrefab, hit.point, Quaternion.identity);
            }

            IDamageable damageable = hit.transform.GetComponentInParent<IDamageable>();

            if (damageable != null)
            {
                damageable.TakeDamage(damagePerShot);
            }
        }

        return true;
    }
}
