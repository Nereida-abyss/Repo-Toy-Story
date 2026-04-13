using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public class WaveAnnouncementUI : MonoBehaviour
{
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private TMP_Text announcementText;
    [SerializeField] private AudioManager audioManager;
    [SerializeField] private ProjectAudioCatalog audioCatalog;
    [SerializeField] private AudioSource audioSource;

    private bool hasLoggedMissingReferences;
    private bool hasLoggedMissingAudio;

    void Awake()
    {
        panelRoot ??= gameObject;
    }

    void OnValidate()
    {
        panelRoot ??= gameObject;
    }

    // Muestra oleada.
    public void ShowWave(int waveNumber)
    {
        if (!HasRequiredUiReferences())
        {
            LogMissingReferences();
            return;
        }

        announcementText.text = $"WAVE {waveNumber}";
        PlayAnnouncementAudio();
        SetVisible(true);
    }

    // Oculta oleada.
    public void HideWave()
    {
        SetVisible(false);
    }

    // Actualiza visible.
    private void SetVisible(bool isVisible)
    {
        if (panelRoot == null)
        {
            return;
        }

        UIFxUtility.SetPanelActive(panelRoot, isVisible);
    }

    private void PlayAnnouncementAudio()
    {
        AudioManager resolvedAudioManager = ResolveAudioManager();
        ProjectAudioCatalog resolvedCatalog = ResolveAudioCatalog(resolvedAudioManager);
        AudioClip clip = ResolveAnnouncementClip(resolvedCatalog);
        float volume = ResolveAnnouncementVolume(resolvedCatalog);
        AudioSource source = ResolveAnnouncementAudioSource(resolvedAudioManager);

        if (clip == null || source == null)
        {
            LogMissingAudio();
            return;
        }

        source.PlayOneShot(clip, volume);
    }

    private bool HasRequiredUiReferences()
    {
        return panelRoot != null && announcementText != null;
    }

    private ProjectAudioCatalog ResolveAudioCatalog(AudioManager resolvedAudioManager)
    {
        return audioCatalog != null ? audioCatalog : (resolvedAudioManager != null ? resolvedAudioManager.Catalog : null);
    }

    private static AudioClip ResolveAnnouncementClip(ProjectAudioCatalog resolvedCatalog)
    {
        return resolvedCatalog != null ? resolvedCatalog.Waves.AnnouncementAudio.Clip : null;
    }

    private static float ResolveAnnouncementVolume(ProjectAudioCatalog resolvedCatalog)
    {
        return resolvedCatalog != null ? resolvedCatalog.Waves.AnnouncementAudio.Volume : 0f;
    }

    private AudioSource ResolveAnnouncementAudioSource(AudioManager resolvedAudioManager)
    {
        return audioSource != null ? audioSource : (resolvedAudioManager != null ? resolvedAudioManager.SharedSfxSource : null);
    }

    private AudioManager ResolveAudioManager()
    {
        if (audioManager != null)
        {
            return audioManager;
        }

        audioManager = AudioManager.Instance;
        return audioManager;
    }

    // Gestiona registro faltante referencias.
    private void LogMissingReferences()
    {
        if (hasLoggedMissingReferences)
        {
            return;
        }

        hasLoggedMissingReferences = true;
        GameDebug.Advertencia("HUDOleadas", "WaveAnnouncementUI no tiene panel o texto asignado.", this);
    }

    private void LogMissingAudio()
    {
        if (hasLoggedMissingAudio)
        {
            return;
        }

        hasLoggedMissingAudio = true;
        GameDebug.Advertencia("HUDOleadas", "WaveAnnouncementUI no pudo reproducir el audio de anuncio de ronda.", this);
    }
}
