using UnityEngine;

[CreateAssetMenu(fileName = "WeaponStatsProfile", menuName = "Player/Weapon Stats Profile")]
public class WeaponStatsProfile : ScriptableObject
{
    [Header("Weapon Stats")]
    [SerializeField] private float maxRange = 100f;
    [SerializeField] private float fireRate = 10f;
    [SerializeField] private int damagePerShot = 20;
    [SerializeField] private int magazineSize = 30;
    [SerializeField] private int reserveAmmo = 90;
    [SerializeField] private bool infiniteReserve = true;
    [SerializeField] private int reserveMagazineCapacity = 2;
    [SerializeField] private float reloadDuration = 1.2f;
    [SerializeField] private float dryFireCooldown = 0.2f;

    [Header("Weapon Effects")]
    [SerializeField] private GameObject muzzleFlashPrefab;
    [SerializeField] private AudioClip fireSound;
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

    public float MaxRange => maxRange;
    public float FireRate => fireRate;
    public int DamagePerShot => damagePerShot;
    public int MagazineSize => magazineSize;
    public int ReserveAmmo => reserveAmmo;
    public bool InfiniteReserve => infiniteReserve;
    public int ReserveMagazineCapacity => reserveMagazineCapacity;
    public float ReloadDuration => reloadDuration;
    public float DryFireCooldown => dryFireCooldown;
    public GameObject MuzzleFlashPrefab => muzzleFlashPrefab;
    public AudioClip FireSound => fireSound;
    public AudioClip DryFireSound => dryFireSound;
    public AudioClip ReloadSound => reloadSound;
    public float FireVolume => fireVolume;
    public float DryFireVolume => dryFireVolume;
    public float ReloadVolume => reloadVolume;
    public float FirePitchRandomness => firePitchRandomness;
    public float DryFirePitchRandomness => dryFirePitchRandomness;
    public float ReloadPitchRandomness => reloadPitchRandomness;
    public float CameraRecoilPitch => cameraRecoilPitch;
    public float CameraRecoilYaw => cameraRecoilYaw;
    public Vector3 WeaponRecoilPosition => weaponRecoilPosition;
    public Vector3 WeaponRecoilRotation => weaponRecoilRotation;
    public float WeaponRecoilReturnTime => weaponRecoilReturnTime;
    public Vector3 WeaponEquipLowerPosition => weaponEquipLowerPosition;
    public Vector3 WeaponEquipLowerRotation => weaponEquipLowerRotation;
}
