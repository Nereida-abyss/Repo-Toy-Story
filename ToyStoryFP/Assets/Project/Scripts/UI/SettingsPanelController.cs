using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class SettingsPanelController : MonoBehaviour
{
    private const string FullscreenKey = "settings.fullscreen";
    private const string MasterVolumeKey = "settings.masterVolume";
    private const string MasterMutedKey = "settings.masterMuted";
    private const string LookSensitivityKey = "settings.lookSensitivity";
    private const float DefaultVolume = 1f;
    private const float DefaultLookSensitivity = 2f;
    private const float MinLookSensitivity = 0.5f;
    private const float MaxLookSensitivity = 5f;

    [SerializeField] private Button closeButton;
    [SerializeField] private Button fullscreenButton;
    [SerializeField] private TMP_Text fullscreenStateText;
    [SerializeField] private Slider masterVolumeSlider;
    [SerializeField] private Button muteButton;
    [SerializeField] private TMP_Text muteButtonText;
    [SerializeField] private TMP_Text muteStateText;
    [SerializeField] private Slider lookSensitivitySlider;
    [SerializeField] private TMP_Text lookSensitivityValueText;
    [SerializeField] private GameObject previousPanelToHide;

    private float masterVolume = DefaultVolume;
    private float lookSensitivity = DefaultLookSensitivity;
    private bool masterMuted;
    private bool isFullscreen;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void ApplySavedSettingsOnStartup()
    {
        ApplySavedSettings();
    }

    private void Awake()
    {
        ValidateReferences();
    }

    private void OnEnable()
    {
        LoadSavedSettings();
        ApplyCurrentSettings();
        RefreshUI();
    }

    public void OpenPanel()
    {
        if (gameObject.activeSelf)
        {
            RefreshUI();
            return;
        }

        if (previousPanelToHide != null)
        {
            UIFxUtility.SetPanelActive(previousPanelToHide, false);
        }

        UIFxUtility.SetPanelActive(gameObject, true);
    }

    public void ClosePanel()
    {
        UIFxUtility.SetPanelActive(gameObject, false);

        if (previousPanelToHide != null)
        {
            UIFxUtility.SetPanelActive(previousPanelToHide, true);
        }
    }

    public void ToggleFullscreen()
    {
        isFullscreen = !isFullscreen;
        PlayerPrefs.SetInt(FullscreenKey, isFullscreen ? 1 : 0);
        PlayerPrefs.Save();

        Screen.fullScreen = isFullscreen;
        RefreshUI();
    }

    public void OnMasterVolumeChanged(float value)
    {
        masterVolume = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat(MasterVolumeKey, masterVolume);
        PlayerPrefs.Save();

        ApplyAudioSettings();
        RefreshUI();
    }

    public void ToggleMute()
    {
        masterMuted = !masterMuted;
        PlayerPrefs.SetInt(MasterMutedKey, masterMuted ? 1 : 0);
        PlayerPrefs.Save();

        ApplyAudioSettings();
        RefreshUI();
    }

    public void OnLookSensitivityChanged(float value)
    {
        lookSensitivity = Mathf.Clamp(value, MinLookSensitivity, MaxLookSensitivity);
        PlayerPrefs.SetFloat(LookSensitivityKey, lookSensitivity);
        PlayerPrefs.Save();

        if (MouseLookScript.instance != null)
        {
            MouseLookScript.instance.SetSensitivity(lookSensitivity);
        }

        RefreshUI();
    }

    public static void ApplySavedSettings()
    {
        bool fullscreen = PlayerPrefs.GetInt(FullscreenKey, Screen.fullScreen ? 1 : 0) == 1;
        float volume = Mathf.Clamp01(PlayerPrefs.GetFloat(MasterVolumeKey, DefaultVolume));
        bool muted = PlayerPrefs.GetInt(MasterMutedKey, 0) == 1;

        Screen.fullScreen = fullscreen;
        AudioListener.volume = muted ? 0f : volume;
    }

    private void LoadSavedSettings()
    {
        isFullscreen = PlayerPrefs.GetInt(FullscreenKey, Screen.fullScreen ? 1 : 0) == 1;
        masterVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(MasterVolumeKey, DefaultVolume));
        masterMuted = PlayerPrefs.GetInt(MasterMutedKey, 0) == 1;
        lookSensitivity = Mathf.Clamp(
            PlayerPrefs.GetFloat(LookSensitivityKey, DefaultLookSensitivity),
            MinLookSensitivity,
            MaxLookSensitivity);
    }

    private void ApplyCurrentSettings()
    {
        Screen.fullScreen = isFullscreen;
        ApplyAudioSettings();

        if (MouseLookScript.instance != null)
        {
            MouseLookScript.instance.SetSensitivity(lookSensitivity);
        }
    }

    private void ApplyAudioSettings()
    {
        AudioListener.volume = masterMuted ? 0f : masterVolume;
    }

    private void RefreshUI()
    {
        if (fullscreenStateText != null)
        {
            fullscreenStateText.text = isFullscreen ? "ON" : "OFF";
        }

        if (masterVolumeSlider != null)
        {
            masterVolumeSlider.SetValueWithoutNotify(masterVolume);
        }

        if (muteButtonText != null)
        {
            muteButtonText.text = masterMuted ? "UNMUTE" : "MUTE";
        }

        if (muteStateText != null)
        {
            muteStateText.text = masterMuted ? "Muted" : $"{Mathf.RoundToInt(masterVolume * 100f)}%";
        }

        if (lookSensitivitySlider != null)
        {
            lookSensitivitySlider.SetValueWithoutNotify(lookSensitivity);
        }

        if (lookSensitivityValueText != null)
        {
            lookSensitivityValueText.text = lookSensitivity.ToString("0.00");
        }
    }

    private void ValidateReferences()
    {
        if (closeButton == null || fullscreenButton == null || fullscreenStateText == null || masterVolumeSlider == null || muteButton == null || lookSensitivitySlider == null || lookSensitivityValueText == null)
        {
            GameDebug.Advertencia("Settings", "Faltan una o mas referencias UI en SettingsPanelController.", this);
        }
    }
}
