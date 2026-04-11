using UnityEngine;

[CreateAssetMenu(fileName = "WeaponPresentationProfile", menuName = "Player/Weapon Presentation Profile")]
public class WeaponPresentationProfile : ScriptableObject
{
    [System.Serializable]
    public sealed class PresentationVariant
    {
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
    }

    [SerializeField] private PresentationVariant playerVariant = new PresentationVariant();
    [SerializeField] private PresentationVariant enemyVariant = new PresentationVariant();

    public PresentationVariant PlayerVariant => playerVariant;
    public PresentationVariant EnemyVariant => enemyVariant;

    public PresentationVariant ResolveVariant(bool playerOwnedWeapon)
    {
        return playerOwnedWeapon ? playerVariant : enemyVariant;
    }
}
