using UnityEngine;

[DisallowMultipleComponent]
public class EnemyAudioController : MonoBehaviour
{
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private EnemyAudioProfile audioProfile;

    private bool hasLoggedMissingAudioSource;
    private bool hasLoggedMissingAudioProfile;

    void Awake()
    {
        ResolveAudioSource();
        Apply3DSettings();
    }

    void OnValidate()
    {
        ResolveAudioSource();
        Apply3DSettings();
    }

    // Reproduce alerta.
    public void PlayAlert()
    {
        EnemyAudioProfile profile = ResolveAudioProfile();
        PlayOneShot(
            profile != null ? profile.AlertClip : null,
            profile != null ? profile.AlertVolume : 0f,
            0.01f);
    }

    // Reproduce arma disparo.
    public void PlayWeaponFire(AudioClip clip, float volume, float pitchRandomness)
    {
        EnemyAudioProfile profile = ResolveAudioProfile();
        float resolvedPitchRandomness = profile != null ? profile.FirePitchRandomness : 0f;
        PlayOneShot(clip, volume, pitchRandomness > 0f ? pitchRandomness : resolvedPitchRandomness);
    }

    // Resuelve audio origen.
    private void ResolveAudioSource()
    {
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }

        if (audioSource == null && !hasLoggedMissingAudioSource)
        {
            hasLoggedMissingAudioSource = true;
            GameDebug.Advertencia("Audio", "EnemyAudioController necesita un AudioSource asignado en el prefab enemigo.", this);
        }
    }

    // Aplica 3 d ajustes.
    private void Apply3DSettings()
    {
        if (audioSource == null)
        {
            return;
        }

        EnemyAudioProfile profile = ResolveAudioProfile();
        float minDistance = profile != null ? profile.MinDistance : 1.2f;
        float maxDistance = profile != null ? profile.MaxDistance : 16f;

        audioSource.playOnAwake = false;
        audioSource.loop = false;
        audioSource.spatialBlend = 1f;
        audioSource.minDistance = minDistance;
        audioSource.maxDistance = Mathf.Max(minDistance + 0.1f, maxDistance);
        audioSource.rolloffMode = AudioRolloffMode.Logarithmic;
    }

    // Reproduce one disparo.
    private void PlayOneShot(AudioClip clip, float volume, float pitchRandomness)
    {
        if (audioSource == null || clip == null || volume <= 0f)
        {
            return;
        }

        audioSource.pitch = 1f + Random.Range(-pitchRandomness, pitchRandomness);
        audioSource.PlayOneShot(clip, volume);
    }

    private EnemyAudioProfile ResolveAudioProfile()
    {
        if (audioProfile != null)
        {
            hasLoggedMissingAudioProfile = false;
            return audioProfile;
        }

        if (!hasLoggedMissingAudioProfile)
        {
            hasLoggedMissingAudioProfile = true;
            GameDebug.Advertencia("Audio", "EnemyAudioController necesita EnemyAudioProfile asignado en el prefab enemigo.", this);
        }

        return null;
    }
}
