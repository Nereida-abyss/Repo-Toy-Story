using UnityEngine;

[DisallowMultipleComponent]
public class PlayerAudioController : MonoBehaviour
{
    [Header("Audio Sources")]
    [SerializeField] private AudioSource generalSource;
    [SerializeField] private AudioSource weaponSource;
    [SerializeField] private AudioSource footstepSource;
    [SerializeField] private PlayerAudioProfile audioProfile;

    private float footstepTimer;
    private float lastHurtPlayTime = -100f;
    private bool hasLoggedMissingSources;
    private bool hasLoggedMissingHurtClip;
    private bool hasLoggedMissingAudioProfile;

    void Awake()
    {
        if (generalSource == null || weaponSource == null || footstepSource == null)
        {
            LogMissingSources();
        }
    }

    // Reproduce salto.
    public void PlayJump()
    {
        PlayerAudioProfile profile = ResolveAudioProfile();
        PlayOneShot(generalSource, profile != null ? profile.JumpClip : null, profile != null ? profile.JumpVolume : 0f);
    }

    // Reproduce arma cambio.
    public void PlayWeaponSwitch()
    {
        PlayerAudioProfile profile = ResolveAudioProfile();
        PlayOneShot(generalSource, profile != null ? profile.WeaponSwitchClip : null, profile != null ? profile.WeaponSwitchVolume : 0f);
    }

    // Reproduce moneda pickup.
    public void PlayCoinPickup()
    {
        PlayerAudioProfile profile = ResolveAudioProfile();
        PlayOneShot(
            generalSource,
            profile != null ? profile.CoinPickupClip : null,
            profile != null ? profile.CoinPickupVolume : 0f,
            profile != null ? profile.CoinPickupPitchRandomness : 0f);
    }

    // Reproduce kill confirm.
    public void PlayKillConfirm()
    {
        PlayerAudioProfile profile = ResolveAudioProfile();
        AudioClip clipToPlay = profile != null ? profile.KillConfirmClip : null;
        bool hasDedicatedKillConfirm = clipToPlay != null;

        if (!hasDedicatedKillConfirm)
        {
            clipToPlay = profile != null ? profile.WeaponSwitchClip : null;
        }

        float killConfirmVolume = profile != null ? profile.KillConfirmVolume : 0f;
        float weaponSwitchVolume = profile != null ? profile.WeaponSwitchVolume : 0f;
        float volumeToPlay = hasDedicatedKillConfirm ? killConfirmVolume : Mathf.Max(weaponSwitchVolume, killConfirmVolume);
        PlayOneShot(generalSource, clipToPlay, volumeToPlay);
    }

    public void PlayHitmarker()
    {
        PlayerAudioProfile profile = ResolveAudioProfile();
        PlayOneShot(generalSource, profile != null ? profile.HitmarkerClip : null, profile != null ? profile.HitmarkerVolume : 0f);
    }

    // Reproduce hurt.
    public void PlayHurt()
    {
        PlayerAudioProfile profile = ResolveAudioProfile();
        float hurtMinInterval = profile != null ? profile.HurtMinInterval : 0f;

        if (Time.time < lastHurtPlayTime + Mathf.Max(0f, hurtMinInterval))
        {
            return;
        }

        AudioClip resolvedHurtClip = profile != null ? profile.HurtClip : null;

        if (resolvedHurtClip == null)
        {
            if (!hasLoggedMissingHurtClip)
            {
                hasLoggedMissingHurtClip = true;
                GameDebug.Advertencia("AudioJugador", "No hay HurtClip configurado en PlayerAudioProfile.", this);
            }

            return;
        }

        lastHurtPlayTime = Time.time;
        PlayOneShot(
            generalSource,
            resolvedHurtClip,
            profile != null ? profile.HurtVolume : 0f,
            profile != null ? profile.HurtPitchRandomness : 0f);
    }

    public void PlayDeath()
    {
        PlayerAudioProfile profile = ResolveAudioProfile();
        PlayOneShot(generalSource, profile != null ? profile.DeathClip : null, profile != null ? profile.DeathVolume : 0f);
    }

    // Reproduce arma disparo.
    public void PlayWeaponFire(AudioClip clip, float volume, float pitchRandomness = 0.02f)
    {
        PlayOneShot(weaponSource, clip, volume, pitchRandomness);
    }

    // Reproduce recarga.
    public void PlayReload(AudioClip clip, float volume, float pitchRandomness = 0.02f)
    {
        PlayOneShot(weaponSource, clip, volume, pitchRandomness);
    }

    // Reproduce dry disparo.
    public void PlayDryFire(AudioClip clip, float volume, float pitchRandomness = 0.015f)
    {
        PlayOneShot(weaponSource, clip, volume, pitchRandomness);
    }

    // Actualiza footsteps.
    public void UpdateFootsteps(bool grounded, float moveInputAmount, float speedNormalized)
    {
        PlayerAudioProfile profile = ResolveAudioProfile();

        if (profile == null)
        {
            footstepTimer = 0f;
            return;
        }

        if (!grounded || moveInputAmount < profile.FootstepMinMoveAmount || speedNormalized < 0.05f)
        {
            footstepTimer = 0f;
            return;
        }

        AudioClip[] footstepClips = profile.FootstepClips ?? System.Array.Empty<AudioClip>();

        if (footstepClips.Length == 0 || footstepSource == null)
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
            GetRandomFootstepClip(footstepClips),
            profile.FootstepVolume,
            profile.FootstepPitchRandomness);

        footstepTimer = Mathf.Lerp(
            Mathf.Max(0.01f, profile.FootstepMaxInterval),
            Mathf.Max(0.01f, profile.FootstepMinInterval),
            Mathf.Clamp01(speedNormalized));
    }

    // Obtiene aleatorio footstep clip.
    private AudioClip GetRandomFootstepClip(AudioClip[] clips)
    {
        if (clips == null || clips.Length == 0)
        {
            return null;
        }

        int clipIndex = Random.Range(0, clips.Length);
        return clips[clipIndex];
    }

    // Reproduce one disparo.
    private void PlayOneShot(AudioSource source, AudioClip clip, float volume, float pitchRandomness = 0f)
    {
        if (source == null || clip == null || volume <= 0f)
        {
            return;
        }

        source.pitch = 1f + Random.Range(-pitchRandomness, pitchRandomness);
        source.PlayOneShot(clip, volume);
    }

    // Gestiona registro faltante sources.
    private void LogMissingSources()
    {
        if (hasLoggedMissingSources)
        {
            return;
        }

        hasLoggedMissingSources = true;
        GameDebug.Advertencia("AudioJugador", "Faltan una o mas referencias de AudioSource hijas en PlayerAudioController.", this);
    }

    private PlayerAudioProfile ResolveAudioProfile()
    {
        if (audioProfile != null)
        {
            hasLoggedMissingAudioProfile = false;
            return audioProfile;
        }

        WarnIfMissingAudioProfile();
        return null;
    }

    private void WarnIfMissingAudioProfile()
    {
        if (hasLoggedMissingAudioProfile)
        {
            return;
        }

        hasLoggedMissingAudioProfile = true;
        GameDebug.Advertencia("AudioJugador", "PlayerAudioController necesita PlayerAudioProfile asignado.", this);
    }
}
