using UnityEngine;

[DisallowMultipleComponent]
public class EnemyAudioController : MonoBehaviour
{
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip alertClip;
    [SerializeField] [Range(0f, 1f)] private float alertVolume = 0.08f;
    [SerializeField] private float minDistance = 1.2f;
    [SerializeField] private float maxDistance = 16f;
    [SerializeField] private float firePitchRandomness = 0.02f;

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
        PlayOneShot(alertClip, alertVolume, 0.01f);
    }

    // Reproduce arma disparo.
    public void PlayWeaponFire(AudioClip clip, float volume, float pitchRandomness)
    {
        PlayOneShot(clip, volume, pitchRandomness > 0f ? pitchRandomness : firePitchRandomness);
    }

    // Resuelve audio origen.
    private void ResolveAudioSource()
    {
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }

        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
    }

    // Aplica 3 d ajustes.
    private void Apply3DSettings()
    {
        if (audioSource == null)
        {
            return;
        }

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
}
