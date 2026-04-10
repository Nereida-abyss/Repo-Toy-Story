using System;
using UnityEngine;

public class WeaponScript : MonoBehaviour
{
    [Header("Weapon Setup")]
    public Camera _camera;
    [SerializeField] private Transform fireOrigin;
    [SerializeField] private float maxRange = 100f;
    [SerializeField] private bool playerOwnedWeapon;
    [SerializeField] private PlayerAudioController playerAudio;
    [SerializeField] private EnemyAudioController enemyAudio;

    [Header("Weapon Stats")]
    public float fireRate = 10f;
    public int damagePerShot = 20;
    [SerializeField] private int magazineSize = 30;
    [SerializeField] private int reserveAmmo = 90;
    [SerializeField] private bool infiniteReserve = true;
    [SerializeField] private int reserveMagazineCapacity = 2;
    [SerializeField] private float reloadDuration = 1.2f;
    [SerializeField] private float dryFireCooldown = 0.2f;

    [Header("Weapon Effects")]
    public GameObject muzzleFlashPrefab;
    public AudioClip fireSound;
    [SerializeField] private AudioClip dryFireSound;
    [SerializeField] private AudioClip reloadSound;
    [SerializeField] [Range(0f, 1f)] private float fireVolume = 0.8f;
    [SerializeField] [Range(0f, 1f)] private float dryFireVolume = 0.35f;
    [SerializeField] [Range(0f, 1f)] private float reloadVolume = 0.5f;
    [SerializeField] private float firePitchRandomness = 0.02f;
    [SerializeField] private float dryFirePitchRandomness = 0.01f;
    [SerializeField] private float reloadPitchRandomness = 0.015f;

    [Header("Camera Recoil")]
    [SerializeField] private float cameraRecoilPitch = 1.1f;
    [SerializeField] private float cameraRecoilYaw = 0.2f;

    [Header("Weapon Recoil")]
    [SerializeField] private Vector3 weaponRecoilPosition = new Vector3(0f, 0.01f, -0.05f);
    [SerializeField] private Vector3 weaponRecoilRotation = new Vector3(-6f, 1.25f, 0.5f);
    [SerializeField] private float weaponRecoilReturnTime = 0.08f;

    [Header("Weapon Equip Animation")]
    [SerializeField] private Vector3 weaponEquipLowerPosition = new Vector3(0.08f, -0.22f, 0.06f);
    [SerializeField] private Vector3 weaponEquipLowerRotation = new Vector3(18f, -8f, 12f);

    private float nextAllowedShotTime;
    private float reloadTimer;
    private float nextAllowedDryFireTime;
    private int currentAmmoInMagazine;
    private int configuredReserveAmmo;
    private bool isReloading;
    private bool ammoInitialized;
    private bool basePoseCached;
    private bool configuredAmmoCached;
    private bool hasWarnedMissingPlayerAudio;
    private bool hasWarnedMissingEnemyAudio;
    private Vector3 baseLocalPosition;
    private Vector3 baseLocalEulerAngles;
    private Vector3 recoilPositionOffset;
    private Vector3 recoilRotationOffset;
    private Vector3 recoilPositionVelocity;
    private Vector3 recoilRotationVelocity;
    private Vector3 equipPositionOffset;
    private Vector3 equipRotationOffset;

    public event Action<WeaponScript> StateChanged;

    public int CurrentAmmoInMagazine => playerOwnedWeapon ? currentAmmoInMagazine : 0;
    public int ReserveAmmo => reserveAmmo;
    public int DamagePerShot => damagePerShot;
    public bool HasInfiniteReserve => infiniteReserve;
    public bool UsesFiniteReserve => !infiniteReserve;
    public bool IsReloading => playerOwnedWeapon && isReloading;
    public bool IsPlayerOwnedWeapon => playerOwnedWeapon;
    public int ReserveAmmoCapacity => infiniteReserve ? 0 : GetFiniteReserveCapacity();
    public int TotalAmmoCapacity => infiniteReserve ? 0 : Mathf.Max(1, magazineSize) + GetFiniteReserveCapacity();
    public int MissingTotalAmmo => infiniteReserve ? 0 : Mathf.Max(0, TotalAmmoCapacity - (currentAmmoInMagazine + reserveAmmo));
    public bool NeedsConfiguredAmmoRefill =>
        playerOwnedWeapon &&
        (currentAmmoInMagazine < magazineSize || reserveAmmo < configuredReserveAmmo || isReloading);

    void Awake()
    {
        CacheBasePose();
        CacheConfiguredAmmo();
        TryInitializeAmmo();
    }

    void OnEnable()
    {
        CacheBasePose();
        CacheConfiguredAmmo();
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
        PlayReloadAudio();
        NotifyStateChanged();
        return true;
    }

    public void CancelReload()
    {
        SetReloadState(false, true);
    }

    public void SetPlayerOwned(bool enabled)
    {
        playerOwnedWeapon = enabled;
        TryInitializeAmmo();
        NotifyStateChanged();
    }

    public void ConfigureAudioControllers(PlayerAudioController playerAudioController, EnemyAudioController enemyAudioController)
    {
        playerAudio = playerAudioController;
        enemyAudio = enemyAudioController;
    }

    public void NotifyEquipped()
    {
        SetReloadState(false, true);
        ResetVisualRecoil();
        NotifyStateChanged();
    }

    public void InitializeAsEquippedAtSpawn()
    {
        CacheBasePose();
        TryInitializeAmmo();
        SetReloadState(false, false);
        ResetVisualRecoil();
    }

    public void SetEquipAnimationProgress(float progress, bool lowering)
    {
        progress = Mathf.Clamp01(progress);

        if (lowering)
        {
            equipPositionOffset = Vector3.Lerp(Vector3.zero, weaponEquipLowerPosition, progress);
            equipRotationOffset = Vector3.Lerp(Vector3.zero, weaponEquipLowerRotation, progress);
        }
        else
        {
            equipPositionOffset = Vector3.Lerp(weaponEquipLowerPosition, Vector3.zero, progress);
            equipRotationOffset = Vector3.Lerp(weaponEquipLowerRotation, Vector3.zero, progress);
        }

        ApplyCurrentPose();
    }

    public void ResetEquipPose()
    {
        equipPositionOffset = Vector3.zero;
        equipRotationOffset = Vector3.zero;
        ApplyCurrentPose();
    }

    public void SetDamagePerShot(int newDamagePerShot)
    {
        damagePerShot = Mathf.Max(1, newDamagePerShot);
        NotifyStateChanged();
    }

    public bool RefillAmmoToConfiguredReserve()
    {
        if (!playerOwnedWeapon)
        {
            return false;
        }

        CacheConfiguredAmmo();

        int targetMagazine = Mathf.Max(1, magazineSize);
        int targetReserve = Mathf.Max(configuredReserveAmmo, targetMagazine);
        bool changed = currentAmmoInMagazine != targetMagazine || reserveAmmo != targetReserve || isReloading;

        currentAmmoInMagazine = targetMagazine;
        reserveAmmo = targetReserve;
        SetReloadState(false, false);

        if (changed)
        {
            NotifyStateChanged();
        }

        return changed;
    }

    public bool TryAddAmmoByMagazines(int magazineCount)
    {
        if (!playerOwnedWeapon || infiniteReserve || magazineCount <= 0)
        {
            return false;
        }

        CacheConfiguredAmmo();

        int ammoPerMagazine = Mathf.Max(1, magazineSize);
        int maxReserve = GetFiniteReserveCapacity();
        int maxTotalAmmo = ammoPerMagazine + maxReserve;
        int currentTotalAmmo = currentAmmoInMagazine + reserveAmmo;
        int missingAmmo = Mathf.Max(0, maxTotalAmmo - currentTotalAmmo);

        if (missingAmmo <= 0)
        {
            return false;
        }

        int ammoToAdd = Mathf.Min(ammoPerMagazine * magazineCount, missingAmmo);
        int magazineRoom = Mathf.Max(0, ammoPerMagazine - currentAmmoInMagazine);
        int addedToMagazine = Mathf.Min(magazineRoom, ammoToAdd);
        int addedToReserve = ammoToAdd - addedToMagazine;

        currentAmmoInMagazine += addedToMagazine;
        reserveAmmo = Mathf.Min(maxReserve, reserveAmmo + addedToReserve);
        SetReloadState(false, false);
        NotifyStateChanged();
        return true;
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
        PlayDryFireAudio();
        NotifyStateChanged();
        return false;
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

        PlayFireAudio();

        Vector3 normalizedDirection = direction.normalized;
        Vector3 rayOrigin = origin + normalizedDirection * 0.05f;

        if (Physics.Raycast(
            rayOrigin,
            normalizedDirection,
            out RaycastHit hit,
            maxRange,
            Physics.DefaultRaycastLayers,
            QueryTriggerInteraction.Ignore))
        {
            if (muzzleFlashPrefab != null)
            {
                Instantiate(muzzleFlashPrefab, hit.point, Quaternion.identity);
            }

            IDamageable damageable = hit.transform.GetComponentInParent<IDamageable>();

            if (damageable != null)
            {
                DamageResult damageResult = damageable.TakeDamage(damagePerShot);
                NotifyEnemyAggro(hit.transform, hit.point, damageResult);
                HandleDamageFeedback(damageResult);
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

        CacheConfiguredAmmo();
        magazineSize = Mathf.Max(1, magazineSize);
        currentAmmoInMagazine = magazineSize;
        reserveAmmo = infiniteReserve ? Mathf.Max(configuredReserveAmmo, magazineSize) : configuredReserveAmmo;
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

    private void CacheConfiguredAmmo()
    {
        if (configuredAmmoCached)
        {
            return;
        }

        magazineSize = Mathf.Max(1, magazineSize);
        reserveMagazineCapacity = Mathf.Max(0, reserveMagazineCapacity);
        configuredReserveAmmo = infiniteReserve
            ? Mathf.Max(reserveAmmo, magazineSize)
            : GetFiniteReserveCapacity();
        configuredAmmoCached = true;
    }

    private int GetFiniteReserveCapacity()
    {
        return Mathf.Max(0, reserveMagazineCapacity) * Mathf.Max(1, magazineSize);
    }

    private void SetReloadState(bool reloading, bool notifyStateChanged)
    {
        if (isReloading == reloading)
        {
            if (!reloading)
            {
                reloadTimer = 0f;
            }

            return;
        }

        isReloading = reloading;

        if (!reloading)
        {
            reloadTimer = 0f;
        }

        if (notifyStateChanged)
        {
            NotifyStateChanged();
        }
    }

    private void CacheBasePose()
    {
        if (basePoseCached)
        {
            return;
        }

        baseLocalPosition = transform.localPosition;
        baseLocalEulerAngles = transform.localEulerAngles;
        basePoseCached = true;
    }

    private void ResetVisualRecoil()
    {
        recoilPositionOffset = Vector3.zero;
        recoilRotationOffset = Vector3.zero;
        recoilPositionVelocity = Vector3.zero;
        recoilRotationVelocity = Vector3.zero;
        equipPositionOffset = Vector3.zero;
        equipRotationOffset = Vector3.zero;
        ApplyCurrentPose();
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

        ApplyCurrentPose();
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

    private void PlayFireAudio()
    {
        AudioClip clipToPlay = fireSound != null ? fireSound : AudioManager.Instance?.GetDefaultWeaponFireClip();

        if (clipToPlay == null)
        {
            return;
        }

        if (playerOwnedWeapon)
        {
            WarnIfMissingPlayerAudio();
            playerAudio?.PlayWeaponFire(clipToPlay, fireVolume, firePitchRandomness);
            return;
        }

        WarnIfMissingEnemyAudio();
        enemyAudio?.PlayWeaponFire(clipToPlay, fireVolume, firePitchRandomness);
    }

    private void PlayDryFireAudio()
    {
        AudioClip clipToPlay = dryFireSound != null ? dryFireSound : AudioManager.Instance?.GetDefaultWeaponDryFireClip();

        if (!playerOwnedWeapon || clipToPlay == null)
        {
            return;
        }

        WarnIfMissingPlayerAudio();
        playerAudio?.PlayDryFire(clipToPlay, dryFireVolume, dryFirePitchRandomness);
    }

    private void PlayReloadAudio()
    {
        AudioClip clipToPlay = reloadSound != null ? reloadSound : AudioManager.Instance?.GetDefaultWeaponReloadClip();

        if (!playerOwnedWeapon || clipToPlay == null)
        {
            return;
        }

        WarnIfMissingPlayerAudio();
        playerAudio?.PlayReload(clipToPlay, reloadVolume, reloadPitchRandomness);
    }

    private void HandleDamageFeedback(DamageResult damageResult)
    {
        if (!playerOwnedWeapon || !damageResult.WasDamaged)
        {
            return;
        }

        if (damageResult.WasKilled)
        {
            RunStatsStore.RegisterBotKill();
            CrosshairFeedbackController.Instance?.PlayDeathMarker();
            WarnIfMissingPlayerAudio();
            playerAudio?.PlayKillConfirm();
            return;
        }

        CrosshairFeedbackController.Instance?.PlayHitMarker();
    }

    private void NotifyEnemyAggro(Transform hitTransform, Vector3 hitPoint, DamageResult damageResult)
    {
        if (!playerOwnedWeapon || !damageResult.WasDamaged || hitTransform == null)
        {
            return;
        }

        EnemyController enemyController = hitTransform.GetComponentInParent<EnemyController>();

        if (enemyController == null)
        {
            return;
        }

        Transform aggressor = GetComponentInParent<PlayerController>()?.transform;
        enemyController.NotifyDamagedByPlayer(aggressor, hitPoint);
    }

    private void ApplyCurrentPose()
    {
        transform.localPosition = baseLocalPosition + equipPositionOffset + recoilPositionOffset;
        transform.localRotation = Quaternion.Euler(baseLocalEulerAngles + equipRotationOffset + recoilRotationOffset);
    }

    private void WarnIfMissingPlayerAudio()
    {
        if (playerAudio != null || hasWarnedMissingPlayerAudio)
        {
            return;
        }

        hasWarnedMissingPlayerAudio = true;
        GameDebug.Advertencia("Armas", $"El arma '{name}' necesita PlayerAudioController asignado en Inspector.", this);
    }

    private void WarnIfMissingEnemyAudio()
    {
        if (enemyAudio != null || hasWarnedMissingEnemyAudio)
        {
            return;
        }

        hasWarnedMissingEnemyAudio = true;
        GameDebug.Advertencia("Armas", $"El arma '{name}' necesita EnemyAudioController asignado en Inspector.", this);
    }
}
