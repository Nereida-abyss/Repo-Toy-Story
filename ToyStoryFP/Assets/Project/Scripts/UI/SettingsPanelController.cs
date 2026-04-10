using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class SettingsPanelController : MonoBehaviour
{
    private enum FullscreenChangeOrigin
    {
        Startup,
        Button,
        HotkeyF11
    }

    private const string FullscreenKey = "settings.fullscreen";
    private const string WindowedWidthKey = "settings.windowedWidth";
    private const string WindowedHeightKey = "settings.windowedHeight";
    private const string MasterVolumeKey = "settings.masterVolume";
    private const string MasterMutedKey = "settings.masterMuted";
    private const string LookSensitivityKey = "settings.lookSensitivity";
    private const float DefaultVolume = 1f;
    private const float DefaultLookSensitivity = 2f;
    private const float MinLookSensitivity = 0.5f;
    private const float MaxLookSensitivity = 5f;
    private const int DefaultWindowedWidth = 1024;
    private const int DefaultWindowedHeight = 768;
    private const int MinimumWindowedDimension = 320;

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
        ToggleFullscreen(FullscreenChangeOrigin.Button);
    }

    public void ToggleFullscreenFromHotkey()
    {
        ToggleFullscreen(FullscreenChangeOrigin.HotkeyF11);
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
        bool fullscreen = ResolveFullscreenPreference();
        float volume = Mathf.Clamp01(PlayerPrefs.GetFloat(MasterVolumeKey, DefaultVolume));
        bool muted = PlayerPrefs.GetInt(MasterMutedKey, 0) == 1;

        ApplyFullscreenState(fullscreen, FullscreenChangeOrigin.Startup, savePreference: false, logChange: true);
        AudioListener.volume = muted ? 0f : volume;
    }

    private void LoadSavedSettings()
    {
        isFullscreen = ResolveFullscreenPreference();
        masterVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(MasterVolumeKey, DefaultVolume));
        masterMuted = PlayerPrefs.GetInt(MasterMutedKey, 0) == 1;
        lookSensitivity = Mathf.Clamp(
            PlayerPrefs.GetFloat(LookSensitivityKey, DefaultLookSensitivity),
            MinLookSensitivity,
            MaxLookSensitivity);
    }

    private void ApplyCurrentSettings()
    {
        ApplyFullscreenMode(isFullscreen);
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
        RefreshFullscreenUi();

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
        if (closeButton == null ||
            fullscreenButton == null ||
            fullscreenStateText == null ||
            masterVolumeSlider == null ||
            muteButton == null ||
            lookSensitivitySlider == null ||
            lookSensitivityValueText == null)
        {
            GameDebug.Advertencia("Settings", "Faltan una o mas referencias UI en SettingsPanelController.", this);
        }
    }

    private void ToggleFullscreen(FullscreenChangeOrigin origin)
    {
        bool targetFullscreen = !ResolveFullscreenPreference();
        ApplyFullscreenState(targetFullscreen, origin, savePreference: true, logChange: true);
        isFullscreen = targetFullscreen;
        RefreshFullscreenUi();
    }

    private static void ApplyFullscreenState(bool fullscreen, FullscreenChangeOrigin origin, bool savePreference, bool logChange)
    {
        ApplyFullscreenMode(fullscreen);

        if (savePreference)
        {
            PlayerPrefs.SetInt(FullscreenKey, fullscreen ? 1 : 0);
            PlayerPrefs.Save();
        }

        if (logChange)
        {
            GameDebug.Info("Settings", $"Fullscreen {GetFullscreenStateLabel(fullscreen)} aplicado desde {GetFullscreenOriginLabel(origin)}.");
        }
    }

    private static bool ResolveFullscreenPreference()
    {
        return PlayerPrefs.GetInt(FullscreenKey, ResolveCurrentFullscreenState() ? 1 : 0) == 1;
    }

    private static bool ResolveCurrentFullscreenState()
    {
        return Screen.fullScreenMode != FullScreenMode.Windowed;
    }

    private static void ApplyFullscreenMode(bool fullscreen)
    {
        if (fullscreen)
        {
            CaptureCurrentWindowedSizeIfNeeded();
            Resolution desktopResolution = Screen.currentResolution;
            Screen.fullScreenMode = FullScreenMode.FullScreenWindow;
            Screen.SetResolution(
                Mathf.Max(DefaultWindowedWidth, desktopResolution.width),
                Mathf.Max(DefaultWindowedHeight, desktopResolution.height),
                FullScreenMode.FullScreenWindow);
            return;
        }

        Vector2Int windowedSize = ResolveWindowedSize();
        Screen.fullScreenMode = FullScreenMode.Windowed;
        Screen.SetResolution(windowedSize.x, windowedSize.y, FullScreenMode.Windowed);
    }

    private static void CaptureCurrentWindowedSizeIfNeeded()
    {
        if (ResolveCurrentFullscreenState())
        {
            return;
        }

        int currentWidth = Mathf.Max(Screen.width, 0);
        int currentHeight = Mathf.Max(Screen.height, 0);

        if (currentWidth < MinimumWindowedDimension || currentHeight < MinimumWindowedDimension)
        {
            return;
        }

        PlayerPrefs.SetInt(WindowedWidthKey, currentWidth);
        PlayerPrefs.SetInt(WindowedHeightKey, currentHeight);
        PlayerPrefs.Save();
    }

    private static Vector2Int ResolveWindowedSize()
    {
        int width = PlayerPrefs.GetInt(WindowedWidthKey, DefaultWindowedWidth);
        int height = PlayerPrefs.GetInt(WindowedHeightKey, DefaultWindowedHeight);

        if (width < MinimumWindowedDimension)
        {
            width = DefaultWindowedWidth;
        }

        if (height < MinimumWindowedDimension)
        {
            height = DefaultWindowedHeight;
        }

        return new Vector2Int(width, height);
    }

    private void RefreshFullscreenUi()
    {
        if (fullscreenStateText != null)
        {
            fullscreenStateText.text = GetFullscreenStateLabel(isFullscreen);
        }
    }

    private static string GetFullscreenStateLabel(bool fullscreen)
    {
        return fullscreen ? "ON" : "OFF";
    }

    private static string GetFullscreenOriginLabel(FullscreenChangeOrigin origin)
    {
        switch (origin)
        {
            case FullscreenChangeOrigin.Button:
                return "Button";
            case FullscreenChangeOrigin.HotkeyF11:
                return "F11";
            default:
                return "Startup";
        }
    }
}
