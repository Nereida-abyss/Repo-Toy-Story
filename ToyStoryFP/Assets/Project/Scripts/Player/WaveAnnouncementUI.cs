using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public class WaveAnnouncementUI : MonoBehaviour
{
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private TMP_Text announcementText;

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
        if (panelRoot == null || announcementText == null)
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
        AudioManager audioManager = AudioManager.Instance;

        if (audioManager == null)
        {
            LogMissingAudio();
            return;
        }

        AudioClip clip = audioManager.GetWaveAnnouncementClip();
        AudioSource source = audioManager.SharedSfxSource;

        if (clip == null || source == null)
        {
            LogMissingAudio();
            return;
        }

        source.PlayOneShot(clip);
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
