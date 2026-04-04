using System;
using UnityEngine;

public class WeaponScript : MonoBehaviour
{
    [Header("Weapon Setup")]
    public Camera _camera;
    [SerializeField] private Transform fireOrigin;
    [SerializeField] private float maxRange = 100f;
    [SerializeField] private bool playerOwnedWeapon;

    [Header("Weapon Stats")]
    public float fireRate = 10f;
    public int damagePerShot = 20;
    [SerializeField] private int magazineSize = 30;
    [SerializeField] private int reserveAmmo = 90;
    [SerializeField] private bool infiniteReserve = true;
    [SerializeField] private float reloadDuration = 1.2f;
    [SerializeField] private float dryFireCooldown = 0.2f;

    [Header("Weapon Effects")]
    public GameObject muzzleFlashPrefab;
    public AudioClip fireSound;

    [Header("Camera Recoil")]
    [SerializeField] private float cameraRecoilPitch = 1.1f;
    [SerializeField] private float cameraRecoilYaw = 0.2f;

    [Header("Weapon Recoil")]
    [SerializeField] private Vector3 weaponRecoilPosition = new Vector3(0f, 0.01f, -0.05f);
    [SerializeField] private Vector3 weaponRecoilRotation = new Vector3(-6f, 1.25f, 0.5f);
    [SerializeField] private float weaponRecoilReturnTime = 0.08f;

    private float nextAllowedShotTime;
    private float reloadTimer;
    private float nextAllowedDryFireTime;
    private int currentAmmoInMagazine;
    private bool isReloading;
    private bool ammoInitialized;
    private Vector3 baseLocalPosition;
    private Vector3 baseLocalEulerAngles;
    private Vector3 recoilPositionOffset;
    private Vector3 recoilRotationOffset;
    private Vector3 recoilPositionVelocity;
    private Vector3 recoilRotationVelocity;

    public event Action<WeaponScript> StateChanged;

    public int CurrentAmmoInMagazine => playerOwnedWeapon ? currentAmmoInMagazine : 0;
    public int ReserveAmmo => reserveAmmo;
    public bool HasInfiniteReserve => infiniteReserve;
    public bool IsReloading => playerOwnedWeapon && isReloading;
    public bool IsPlayerOwnedWeapon => playerOwnedWeapon;

    void Awake()
    {
        CacheBasePose();
        TryInitializeAmmo();
    }

    void OnEnable()
    {
        CacheBasePose();
        ResetVisualRecoil();

        TryInitializeAmmo();
    }

    void Update()
    {
        UpdateReload();
        UpdateVisualRecoil();
    }

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

    public bool TryReload()
    {
        if (!playerOwnedWeapon || isReloading || magazineSize <= 0)
        {
            return false;
        }

        if (currentAmmoInMagazine >= magazineSize)
        {
            return false;
        }

        if (!infiniteReserve && reserveAmmo <= 0)
        {
            return false;
        }

        isReloading = true;
        reloadTimer = reloadDuration;
        NotifyStateChanged();
        return true;
    }

    public void CancelReload()
    {
        if (!isReloading)
        {
            return;
        }

        isReloading = false;
        reloadTimer = 0f;
        NotifyStateChanged();
    }

    public void SetPlayerOwned(bool enabled)
    {
        playerOwnedWeapon = enabled;
        TryInitializeAmmo();
        NotifyStateChanged();
    }

    public void NotifyEquipped()
    {
        CancelReload();
        ResetVisualRecoil();
        NotifyStateChanged();
    }

    private bool CanFire()
    {
        if (Time.time < nextAllowedShotTime)
        {
            return false;
        }

        if (!playerOwnedWeapon)
        {
            return true;
        }

        if (isReloading)
        {
            return false;
        }

        if (currentAmmoInMagazine > 0)
        {
            return true;
        }

        if (Time.time < nextAllowedDryFireTime)
        {
            return false;
        }

        nextAllowedDryFireTime = Time.time + dryFireCooldown;
        nextAllowedShotTime = nextAllowedDryFireTime;
        NotifyStateChanged();
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

        if (playerOwnedWeapon)
        {
            currentAmmoInMagazine = Mathf.Max(0, currentAmmoInMagazine - 1);
        }

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

        ApplyCameraRecoil();
        ApplyWeaponRecoil();
        NotifyStateChanged();
        return true;
    }

    private void TryInitializeAmmo()
    {
        if (!playerOwnedWeapon || ammoInitialized)
        {
            return;
        }

        magazineSize = Mathf.Max(1, magazineSize);
        currentAmmoInMagazine = magazineSize;
        reserveAmmo = Mathf.Max(reserveAmmo, magazineSize);
        ammoInitialized = true;
    }

    private void UpdateReload()
    {
        if (!isReloading)
        {
            return;
        }

        reloadTimer -= Time.deltaTime;

        if (reloadTimer > 0f)
        {
            return;
        }

        isReloading = false;
        reloadTimer = 0f;

        if (infiniteReserve)
        {
            currentAmmoInMagazine = magazineSize;
        }
        else
        {
            int neededAmmo = magazineSize - currentAmmoInMagazine;
            int transferredAmmo = Mathf.Min(neededAmmo, reserveAmmo);
            currentAmmoInMagazine += transferredAmmo;
            reserveAmmo -= transferredAmmo;
        }

        NotifyStateChanged();
    }

    private void CacheBasePose()
    {
        baseLocalPosition = transform.localPosition;
        baseLocalEulerAngles = transform.localEulerAngles;
    }

    private void ResetVisualRecoil()
    {
        recoilPositionOffset = Vector3.zero;
        recoilRotationOffset = Vector3.zero;
        recoilPositionVelocity = Vector3.zero;
        recoilRotationVelocity = Vector3.zero;
        transform.localPosition = baseLocalPosition;
        transform.localRotation = Quaternion.Euler(baseLocalEulerAngles);
    }

    private void UpdateVisualRecoil()
    {
        recoilPositionOffset = Vector3.SmoothDamp(
            recoilPositionOffset,
            Vector3.zero,
            ref recoilPositionVelocity,
            Mathf.Max(0.01f, weaponRecoilReturnTime));

        recoilRotationOffset = Vector3.SmoothDamp(
            recoilRotationOffset,
            Vector3.zero,
            ref recoilRotationVelocity,
            Mathf.Max(0.01f, weaponRecoilReturnTime));

        transform.localPosition = baseLocalPosition + recoilPositionOffset;
        transform.localRotation = Quaternion.Euler(baseLocalEulerAngles + recoilRotationOffset);
    }

    private void ApplyCameraRecoil()
    {
        if (!playerOwnedWeapon || _camera == null)
        {
            return;
        }

        MouseLookScript mouseLook = _camera.GetComponent<MouseLookScript>();

        if (mouseLook == null)
        {
            return;
        }

        float yawKick = UnityEngine.Random.Range(-cameraRecoilYaw, cameraRecoilYaw);
        mouseLook.ApplyRecoil(cameraRecoilPitch, yawKick);
    }

    private void ApplyWeaponRecoil()
    {
        if (!playerOwnedWeapon)
        {
            return;
        }

        recoilPositionOffset += weaponRecoilPosition;
        recoilRotationOffset += new Vector3(
            weaponRecoilRotation.x,
            UnityEngine.Random.Range(-weaponRecoilRotation.y, weaponRecoilRotation.y),
            UnityEngine.Random.Range(-weaponRecoilRotation.z, weaponRecoilRotation.z));
    }

    private void NotifyStateChanged()
    {
        StateChanged?.Invoke(this);
    }
}
