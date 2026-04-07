using UnityEngine;

[DisallowMultipleComponent]
public class PlayerAudioController : MonoBehaviour
{
    private const string GeneralSourceName = "PlayerAudioGeneral";
    private const string WeaponSourceName = "PlayerAudioWeapon";
    private const string FootstepSourceName = "PlayerAudioFootsteps";

    [Header("Audio Sources")]
    [SerializeField] private AudioSource generalSource;
    [SerializeField] private AudioSource weaponSource;
    [SerializeField] private AudioSource footstepSource;

    [Header("Jump")]
    [SerializeField] private AudioClip jumpClip;
    [SerializeField] [Range(0f, 1f)] private float jumpVolume = 0.5f;

    [Header("Footsteps")]
    [SerializeField] private AudioClip[] footstepClips = System.Array.Empty<AudioClip>();
    [SerializeField] [Range(0f, 1f)] private float footstepVolume = 0.12f;
    [SerializeField] private float footstepMinInterval = 0.3f;
    [SerializeField] private float footstepMaxInterval = 0.48f;
    [SerializeField] private float footstepMinMoveAmount = 0.2f;
    [SerializeField] private float footstepPitchRandomness = 0.03f;

    [Header("Weapon Switch")]
    [SerializeField] private AudioClip weaponSwitchClip;
    [SerializeField] [Range(0f, 1f)] private float weaponSwitchVolume = 0.28f;

    [Header("Coin Pickup")]
    [SerializeField] private AudioClip coinPickupClip;
    [SerializeField] [Range(0f, 1f)] private float coinPickupVolume = 0.24f;
    [SerializeField] private float coinPickupPitchRandomness = 0.04f;

    [Header("Kill Confirm")]
    [SerializeField] private AudioClip killConfirmClip;
    [SerializeField] [Range(0f, 1f)] private float killConfirmVolume = 0.22f;

    [Header("Damage")]
    [SerializeField] private AudioClip hurtClip;
    [SerializeField] [Range(0f, 1f)] private float hurtVolume = 0.2f;
    [SerializeField] private float hurtPitchRandomness = 0.035f;
    [SerializeField] private float hurtMinInterval = 0.08f;

    private float footstepTimer;
    private float lastHurtPlayTime = -100f;
    private bool hasLoggedMissingSources;
    private bool hasLoggedMissingHurtClip;

    void Awake()
    {
        ResolveSources();
    }

    void OnValidate()
    {
        ResolveSources();
    }

    public void PlayJump()
    {
        PlayOneShot(generalSource, jumpClip, jumpVolume);
    }

    public void PlayWeaponSwitch()
    {
        PlayOneShot(generalSource, weaponSwitchClip, weaponSwitchVolume);
    }

    public void PlayCoinPickup()
    {
        PlayOneShot(generalSource, coinPickupClip, coinPickupVolume, coinPickupPitchRandomness);
    }

    public void PlayKillConfirm()
    {
        AudioClip clipToPlay = killConfirmClip != null ? killConfirmClip : weaponSwitchClip;
        float volumeToPlay = killConfirmClip != null ? killConfirmVolume : Mathf.Max(weaponSwitchVolume, killConfirmVolume);
        PlayOneShot(generalSource, clipToPlay, volumeToPlay);
    }

    public void PlayHurt()
    {
        if (Time.time < lastHurtPlayTime + Mathf.Max(0f, hurtMinInterval))
        {
            return;
        }

        if (hurtClip == null)
        {
            if (!hasLoggedMissingHurtClip)
            {
                hasLoggedMissingHurtClip = true;
                GameDebug.Advertencia("AudioJugador", "No hay hurtClip asignado en PlayerAudioController.", this);
            }

            return;
        }

        lastHurtPlayTime = Time.time;
        PlayOneShot(generalSource, hurtClip, hurtVolume, hurtPitchRandomness);
    }

    public void PlayWeaponFire(AudioClip clip, float volume, float pitchRandomness = 0.02f)
    {
        PlayOneShot(weaponSource, clip, volume, pitchRandomness);
    }

    public void PlayReload(AudioClip clip, float volume, float pitchRandomness = 0.02f)
    {
        PlayOneShot(weaponSource, clip, volume, pitchRandomness);
    }

    public void PlayDryFire(AudioClip clip, float volume, float pitchRandomness = 0.015f)
    {
        PlayOneShot(weaponSource, clip, volume, pitchRandomness);
    }

    public void UpdateFootsteps(bool grounded, float moveInputAmount, float speedNormalized)
    {
        float movementIntensity = speedNormalized;

        if (!grounded || moveInputAmount < footstepMinMoveAmount || speedNormalized < 0.05f)
        {
            footstepTimer = 0f;
            return;
        }

        if (footstepClips == null || footstepClips.Length == 0 || footstepSource == null)
        {
            return;
        }

        footstepTimer -= Time.deltaTime;

        if (footstepTimer > 0f)
        {
            return;
        }

        PlayOneShot(
            footstepSource,
            GetRandomFootstepClip(),
            footstepVolume,
            footstepPitchRandomness);

        footstepTimer = Mathf.Lerp(
            Mathf.Max(0.01f, footstepMaxInterval),
            Mathf.Max(0.01f, footstepMinInterval),
            Mathf.Clamp01(movementIntensity));
    }

    private void ResolveSources()
    {
        if (generalSource == null)
        {
            generalSource = ResolveChildSource(GeneralSourceName);
        }

        if (weaponSource == null)
        {
            weaponSource = ResolveChildSource(WeaponSourceName);
        }

        if (footstepSource == null)
        {
            footstepSource = ResolveChildSource(FootstepSourceName);
        }

        if (generalSource == null || weaponSource == null || footstepSource == null)
        {
            LogMissingSources();
        }
    }

    private AudioSource ResolveChildSource(string childName)
    {
        AudioSource[] sources = GetComponentsInChildren<AudioSource>(true);

        for (int i = 0; i < sources.Length; i++)
        {
            if (sources[i] != null && sources[i].gameObject.name == childName)
            {
                return sources[i];
            }
        }

        return null;
    }

    private AudioClip GetRandomFootstepClip()
    {
        if (footstepClips == null || footstepClips.Length == 0)
        {
            return null;
        }

        int clipIndex = Random.Range(0, footstepClips.Length);
        return footstepClips[clipIndex];
    }

    private void PlayOneShot(AudioSource source, AudioClip clip, float volume, float pitchRandomness = 0f)
    {
        if (source == null || clip == null || volume <= 0f)
        {
            return;
        }

        source.pitch = 1f + Random.Range(-pitchRandomness, pitchRandomness);
        source.PlayOneShot(clip, volume);
    }

    private void LogMissingSources()
    {
        if (hasLoggedMissingSources)
        {
            return;
        }

        hasLoggedMissingSources = true;
        GameDebug.Advertencia("AudioJugador", "Faltan una o mas referencias de AudioSource hijas en PlayerAudioController.", this);
    }
}
